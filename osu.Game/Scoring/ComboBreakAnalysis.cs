// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    /// <summary>
    /// scosu: the combo of a play over time and every place it broke, from the play's judgements in order,
    /// with what each break cost in osu!stable score (see <see cref="ComputeCosts"/>).
    /// </summary>
    public class ComboBreakAnalysis
    {
        /// <summary>
        /// One judgement of the play: when its object was, how it was judged, and the best it could have been judged.
        /// </summary>
        public readonly record struct JudgedObject(double Time, HitResult Result, HitResult MaxResult);

        /// <summary>
        /// The combo right after a judgement.
        /// </summary>
        public readonly record struct ComboPoint(double Time, int Combo);

        /// <summary>
        /// A judgement that reset the combo: a miss, a missed slider tick or repeat, or a slider break.
        /// </summary>
        public class ComboBreak
        {
            public double Time { get; init; }

            public HitResult Result { get; init; }

            /// <summary>
            /// The combo the break reset. 0 for a miss right after another.
            /// </summary>
            public int ComboLost { get; init; }

            /// <summary>
            /// How much more osu!stable score the play would have with only this judgement at its best (a miss hit perfectly,
            /// a slider break held), or <c>null</c> when it isn't known (see <see cref="ComputeCosts"/>).
            /// </summary>
            public long? Cost { get; internal set; }

            /// <summary>
            /// Its place in the judgement order.
            /// </summary>
            internal int OrderIndex { get; init; }

            internal HitResult MaxResult { get; init; }

            /// <summary>
            /// The combo the run after it reached, before the next break or the end.
            /// </summary>
            internal int ComboAfter { get; set; }
        }

        /// <summary>
        /// The combo after each judgement that changes it, in order.
        /// </summary>
        public IReadOnlyList<ComboPoint> Points => points;

        /// <summary>
        /// Every break, in order.
        /// </summary>
        public IReadOnlyList<ComboBreak> Breaks => breaks;

        /// <summary>
        /// The breaks gathered into at most about <paramref name="maxGroups"/> groups, each spanning at most 1/<paramref name="maxGroups"/>
        /// of the play's length, for drawing: a long play with hundreds of breaks would otherwise draw hundreds of marks.
        /// Each group is shown as its break that lost the most combo (the first of those).
        /// </summary>
        public IReadOnlyList<BreakGroup> GroupBreaks(int maxGroups)
        {
            if (breaks.Count <= maxGroups)
                return breaks.Select(b => new BreakGroup(b, 1)).ToArray();

            var groups = new List<BreakGroup>();
            double span = Math.Max(1, EndTime - StartTime) / Math.Max(1, maxGroups);

            int first = 0;

            while (first < breaks.Count)
            {
                var shown = breaks[first];
                int end = first + 1;

                while (end < breaks.Count && breaks[end].Time - breaks[first].Time < span)
                {
                    if (breaks[end].ComboLost > shown.ComboLost)
                        shown = breaks[end];
                    end++;
                }

                groups.Add(new BreakGroup(shown, end - first));
                first = end;
            }

            return groups;
        }

        /// <param name="Shown">The break shown for the group.</param>
        /// <param name="Count">How many breaks the group has, <paramref name="Shown"/> included.</param>
        public record BreakGroup(ComboBreak Shown, int Count);

        /// <summary>
        /// The time of the first and the last judgement.
        /// </summary>
        public double StartTime { get; }

        public double EndTime { get; }

        /// <summary>
        /// The play's longest combo.
        /// </summary>
        public int MaxCombo { get; }

        /// <summary>
        /// Whether <see cref="ComputeCosts"/> found the cost of the breaks.
        /// </summary>
        public bool HasCosts { get; private set; }

        /// <summary>
        /// The play's osu!stable score over time against a full combo's, once <see cref="ComputeScoreRace"/> has found it.
        /// </summary>
        public ScoreRace? Race { get; private set; }

        /// <summary>
        /// The osu!stable score of a play and of a full combo on the same beatmap, at evenly spaced times from <see cref="StartTime"/> to <see cref="EndTime"/>.
        /// </summary>
        /// <param name="Times">The time of each sample.</param>
        /// <param name="Play">The play's score at each time.</param>
        /// <param name="FullCombo">A full combo's score at each time: every judgement so far at its best, in one unbroken combo.
        /// It ends at the beatmap's maximum score (<see cref="StableScoreCalculator.MaximumScore"/>).</param>
        public record ScoreRace(double[] Times, long[] Play, long[] FullCombo)
        {
            /// <summary>
            /// The play's score and a full combo's at <paramref name="time"/>, between the samples around it.
            /// </summary>
            public (double Play, double FullCombo) At(double time)
            {
                int after = Array.FindIndex(Times, t => t >= time);

                if (after < 0)
                    return (Play[^1], FullCombo[^1]);
                if (after == 0)
                    return (Play[0], FullCombo[0]);

                double share = Times[after] > Times[after - 1] ? (time - Times[after - 1]) / (Times[after] - Times[after - 1]) : 1;
                return (lerp(Play[after - 1], Play[after], share), lerp(FullCombo[after - 1], FullCombo[after], share));
            }

            private static double lerp(long from, long to, double share) => from + (to - from) * share;
        }

        private readonly List<ComboPoint> points = new List<ComboPoint>();
        private readonly List<ComboBreak> breaks = new List<ComboBreak>();

        /// <summary>
        /// Every judgement of the play, in order.
        /// </summary>
        private readonly List<JudgedObject> judgements = new List<JudgedObject>();

        /// <summary>
        /// The play's judgements that change the combo, as the server receives them (<see cref="StableScoreCalculator.EncodeJudgement"/>).
        /// </summary>
        private readonly string order;

        public static ComboBreakAnalysis FromHitEvents(IEnumerable<HitEvent> hitEvents)
            // An object is judged once it ends (a slider, a spinner), so that's where its judgement goes on the timeline.
            => new ComboBreakAnalysis(hitEvents.Select(e => new JudgedObject(e.HitObject.GetEndTime(), e.Result, e.HitObject.Judgement.MaxResult)));

        /// <param name="judgements">The play's judgements, in the order they happened.</param>
        public ComboBreakAnalysis(IEnumerable<JudgedObject> judgements)
        {
            var encoded = new StringBuilder();
            int combo = 0;
            double start = double.MaxValue, end = double.MinValue;

            foreach (var judgement in judgements)
            {
                this.judgements.Add(judgement);
                start = Math.Min(start, judgement.Time);
                end = Math.Max(end, judgement.Time);

                if (!judgement.Result.AffectsCombo() || StableScoreCalculator.EncodeJudgement(judgement.Result) is not char code)
                    continue;

                int index = encoded.Length;
                encoded.Append(code);

                if (judgement.Result.IncreasesCombo())
                {
                    combo++;
                    MaxCombo = Math.Max(MaxCombo, combo);
                }
                else
                {
                    if (breaks.Count > 0)
                        breaks[^1].ComboAfter = combo;

                    breaks.Add(new ComboBreak
                    {
                        Time = judgement.Time,
                        Result = judgement.Result,
                        ComboLost = combo,
                        OrderIndex = index,
                        MaxResult = judgement.MaxResult,
                    });
                    combo = 0;
                }

                points.Add(new ComboPoint(judgement.Time, combo));
            }

            if (breaks.Count > 0)
                breaks[^1].ComboAfter = combo;

            order = encoded.ToString();
            StartTime = start == double.MaxValue ? 0 : start;
            EndTime = end == double.MinValue ? 0 : end;
        }

        /// <summary>
        /// The combo over time in <paramref name="count"/> equal slices from <see cref="StartTime"/> to <see cref="EndTime"/>: the highest combo
        /// each slice reached, or the combo it was carried at when nothing in it changed the combo (a spinner, a break in the map).
        /// </summary>
        public int[] ComboColumns(int count)
        {
            int[] columns = new int[count];

            if (count == 0 || points.Count == 0)
                return columns;

            double length = EndTime - StartTime;
            int combo = 0;
            int next = 0;

            for (int i = 0; i < count; i++)
            {
                // The last slice takes everything up to and including the end.
                double sliceEnd = i == count - 1 ? double.PositiveInfinity : StartTime + length * (i + 1) / count;
                int highest = combo;

                while (next < points.Count && points[next].Time < sliceEnd)
                {
                    combo = points[next++].Combo;
                    highest = Math.Max(highest, combo);
                }

                columns[i] = highest;
            }

            return columns;
        }

        /// <summary>
        /// Finds what each break cost: the play's osu!stable score with that one judgement at its best, minus its own.
        /// Costs don't add up to the score lost to combo breaks, since two breaks close together cost more than each one alone.
        /// </summary>
        /// <param name="attributes">The beatmap's attributes, from the server.</param>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="statistics">The play's judgements.</param>
        /// <param name="maximumStatistics">The judgements of a perfect play on the beatmap.</param>
        /// <param name="multiplier">The <see cref="StableScoreCalculator.ScoreMultiplier"/> of the play's mods.</param>
        /// <returns>Whether the costs are known: not when the judgements don't match <paramref name="statistics"/> (the play was rewound, for example).</returns>
        public bool ComputeCosts(StableScoreAttributes attributes, int rulesetId, IReadOnlyDictionary<HitResult, int> statistics,
                                 IReadOnlyDictionary<HitResult, int> maximumStatistics, double multiplier)
        {
            if (!StableScoreCalculator.IsValidOrder(order, statistics, MaxCombo))
                return false;

            long actual = StableScoreCalculator.Calculate(attributes, rulesetId, statistics, maximumStatistics, MaxCombo, multiplier, order);

            foreach (var comboBreak in breaks)
            {
                if (!comboBreak.MaxResult.IncreasesCombo() || StableScoreCalculator.EncodeJudgement(comboBreak.MaxResult) is not char bestCode)
                    continue;

                char[] fixedOrder = order.ToCharArray();
                fixedOrder[comboBreak.OrderIndex] = bestCode;

                var fixedStatistics = new Dictionary<HitResult, int>(statistics);
                fixedStatistics[comboBreak.Result] = fixedStatistics.GetValueOrDefault(comboBreak.Result) - 1;
                fixedStatistics[comboBreak.MaxResult] = fixedStatistics.GetValueOrDefault(comboBreak.MaxResult) + 1;

                // The run before the break and the run after it become one.
                int fixedMaxCombo = Math.Max(MaxCombo, comboBreak.ComboLost + 1 + comboBreak.ComboAfter);

                long withoutBreak = StableScoreCalculator.Calculate(attributes, rulesetId, fixedStatistics, maximumStatistics, fixedMaxCombo, multiplier, new string(fixedOrder));
                comboBreak.Cost = Math.Max(withoutBreak - actual, 0);
            }

            HasCosts = true;
            return true;
        }

        /// <summary>
        /// Finds the play's osu!stable score over time and a full combo's next to it (see <see cref="Race"/>): the score of each prefix of the play,
        /// and of the same prefix with every judgement at its best. The gap between the two is the score lost so far.
        /// </summary>
        /// <param name="attributes">The beatmap's attributes, from the server.</param>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="statistics">The play's judgements.</param>
        /// <param name="maximumStatistics">The judgements of a perfect play on the beatmap.</param>
        /// <param name="multiplier">The <see cref="StableScoreCalculator.ScoreMultiplier"/> of the play's mods.</param>
        /// <param name="sampleCount">How many times to sample, from <see cref="StartTime"/> to <see cref="EndTime"/>.</param>
        /// <returns>Whether the race is known: not when the judgements don't match <paramref name="statistics"/> (the play was rewound, for example).</returns>
        public bool ComputeScoreRace(StableScoreAttributes attributes, int rulesetId, IReadOnlyDictionary<HitResult, int> statistics,
                                     IReadOnlyDictionary<HitResult, int> maximumStatistics, double multiplier, int sampleCount)
        {
            if (sampleCount < 2 || judgements.Count == 0 || !StableScoreCalculator.IsValidOrder(order, statistics, MaxCombo))
                return false;

            var times = new double[sampleCount];
            long[] play = new long[sampleCount];
            long[] fullCombo = new long[sampleCount];

            var playPrefix = new Prefix();
            var fullComboPrefix = new Prefix();

            // A full combo's spinner bonus isn't in its judgements (lazer and stable count spins differently): it comes with the spinners,
            // in step with their bonus judgements, or evenly over the play when it has none.
            int bonusJudgements = judgements.Count(j => j.MaxResult.IsBonus());
            int bonusSoFar = 0;
            long bonusScore = rulesetId == 3 ? 0 : attributes.BonusScore;

            double length = EndTime - StartTime;
            int next = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                bool last = i == sampleCount - 1;
                double time = last ? EndTime : StartTime + length * i / (sampleCount - 1);

                for (; next < judgements.Count && (last || judgements[next].Time <= time); next++)
                {
                    var judgement = judgements[next];
                    playPrefix.Add(judgement.Result);

                    if (judgement.MaxResult.IsBonus())
                        bonusSoFar++;
                    else
                        fullComboPrefix.Add(judgement.MaxResult);
                }

                times[i] = time;

                // The whole play is scored from its own statistics, exactly as the server scores it.
                play[i] = last
                    ? StableScoreCalculator.Calculate(attributes, rulesetId, statistics, maximumStatistics, MaxCombo, multiplier, order)
                    : playPrefix.Score(attributes, rulesetId, maximumStatistics, multiplier);

                double bonusShare = bonusJudgements > 0 ? (double)bonusSoFar / bonusJudgements : length > 0 ? (time - StartTime) / length : 1;
                fullCombo[i] = fullComboPrefix.Score(attributes, rulesetId, maximumStatistics, multiplier)
                               + (long)Math.Round(bonusScore * bonusShare, MidpointRounding.AwayFromZero);
            }

            Race = new ScoreRace(times, play, fullCombo);
            return true;
        }

        /// <summary>
        /// The judgements of a play up to some time, to score with <see cref="StableScoreCalculator.Calculate"/>.
        /// </summary>
        private class Prefix
        {
            private readonly Dictionary<HitResult, int> statistics = new Dictionary<HitResult, int>();
            private readonly StringBuilder order = new StringBuilder();
            private int combo;
            private int maxCombo;

            public void Add(HitResult result)
            {
                statistics[result] = statistics.GetValueOrDefault(result) + 1;

                if (!result.AffectsCombo() || StableScoreCalculator.EncodeJudgement(result) is not char code)
                    return;

                order.Append(code);
                combo = result.IncreasesCombo() ? combo + 1 : 0;
                maxCombo = Math.Max(maxCombo, combo);
            }

            public long Score(StableScoreAttributes attributes, int rulesetId, IReadOnlyDictionary<HitResult, int> maximumStatistics, double multiplier)
                => StableScoreCalculator.Calculate(attributes, rulesetId, statistics, maximumStatistics, maxCombo, multiplier, order.ToString());
        }
    }
}
