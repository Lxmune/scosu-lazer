// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    /// <summary>
    /// Computes the osu!stable score of a play the way servers that rank plays by it do (scosu's <c>stable_score::play_score</c>).
    /// </summary>
    /// <remarks>
    /// Stable's combo bonus depends on the order of judgements. When it isn't known, the combo part of a play
    /// that isn't a perfect full combo is the server's estimate.
    /// </remarks>
    public static class StableScoreCalculator
    {
        /// <summary>
        /// The stable score of a play so far.
        /// </summary>
        /// <param name="attributes">The beatmap's attributes, from the server.</param>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="statistics">The judgements of the play.</param>
        /// <param name="maximumStatistics">The judgements of a perfect play on the beatmap.</param>
        /// <param name="maxCombo">The longest combo of the play.</param>
        /// <param name="mods">The mods of the play, with their settings.</param>
        /// <param name="judgementOrder">The order of the play's judgements (see <see cref="EncodeJudgement"/>), if known.</param>
        public static long Calculate(StableScoreAttributes attributes, int rulesetId, IReadOnlyDictionary<HitResult, int> statistics,
                                     IReadOnlyDictionary<HitResult, int> maximumStatistics, int maxCombo, IReadOnlyList<APIMod> mods,
                                     string? judgementOrder = null)
        {
            var play = new Play(statistics, maximumStatistics, maxCombo, parseOrder(judgementOrder, statistics, maxCombo));
            double multiplier = ScoreMultiplier(rulesetId, mods, attributes.ConvertedColumns);

            switch (rulesetId)
            {
                case 0:
                    return osuScore(attributes, play, multiplier);

                case 1:
                    return taikoScore(attributes, play, multiplier);

                case 2:
                    return catchScore(attributes, play, multiplier);

                case 3:
                    return maniaScore(play, multiplier);

                default:
                    return 0;
            }
        }

        private static long osuScore(StableScoreAttributes attributes, Play play, double multiplier)
        {
            long great = play.Count(HitResult.Great);
            long ok = play.Count(HitResult.Ok);
            long meh = play.Count(HitResult.Meh);
            long hits = great + ok + meh;

            // Slider parts (heads, ticks, repeats, tails), in lazer's two layouts:
            // - normal: heads are judged like circles, ticks and repeats are large ticks, tails slider tails;
            // - Classic mod: heads, ticks and repeats are large ticks, tails small ticks (which don't add combo).
            // The share of parts hit tells how much of the beatmap's slider score the player got.
            long largeTicks = play.Count(HitResult.LargeTickHit);
            bool classicLayout = play.Has(HitResult.SmallTickHit) || play.Has(HitResult.SmallTickMiss);
            double nestedHits;
            long comboHits;

            if (classicLayout)
            {
                nestedHits = largeTicks + play.Count(HitResult.SmallTickHit);
                comboHits = hits + largeTicks;
            }
            else
            {
                long tails = play.Count(HitResult.SliderTailHit);
                // Which heads were hit is unknown: assume the same share as all circle-like judgements.
                double heads = attributes.SliderCount * ratio(hits, attributes.ObjectCount);
                nestedHits = largeTicks + tails + heads;
                comboHits = hits + largeTicks + tails;
            }

            double nestedShare = attributes.NestedCount > 0 ? Math.Clamp(nestedHits / attributes.NestedCount, 0, 1) : 1;
            long accuracyPart = 300 * great + 100 * ok + 50 * meh + round(attributes.NestedScore * nestedShare);

            // Combo bonus: the value of each hit (300 → 12, 100 → 4, 50 → 2) times its combo.
            double valueShare = hits == 0 ? 0 : (double)(12 * great + 4 * ok + 2 * meh) / (12 * hits);
            long breaks = play.Count(HitResult.Miss) + play.Count(HitResult.LargeTickMiss);
            double progress = comboProgress(ComboCurve.Quadratic, attributes, play, comboHits, breaks);
            long comboPart = round(attributes.ComboScore * multiplier * progress * valueShare);

            // Spinner bonus: 100 per spin, 1100 per bonus spin.
            long bonus = Math.Min(100 * play.Count(HitResult.SmallBonus) + 1100 * play.Count(HitResult.LargeBonus), attributes.BonusScore);

            return accuracyPart + comboPart + bonus;
        }

        private static long taikoScore(StableScoreAttributes attributes, Play play, double multiplier)
        {
            long great = play.Count(HitResult.Great);
            long ok = play.Count(HitResult.Ok);
            long hits = great + ok;
            // A taiko "good" (lazer's ok) is worth half a "great", including its combo bonus.
            double valueShare = hits == 0 ? 0 : (great + 0.5 * ok) / hits;
            double accuracyPart = attributes.AccuracyScore * ratio(hits, attributes.ObjectCount) * valueShare;

            double progress = comboProgress(ComboCurve.Capped, attributes, play, hits, play.Count(HitResult.Miss));
            double comboPart = attributes.ComboScore * multiplier * progress * valueShare;

            // stable and lazer count drum roll ticks and swell hits differently: use the share hit.
            long bonusHits = play.Count(HitResult.SmallBonus) + play.Count(HitResult.LargeBonus);
            long bonusMaximum = play.Maximum(HitResult.SmallBonus) + play.Maximum(HitResult.LargeBonus);
            double bonus = bonusMaximum > 0 ? attributes.BonusScore * ratio(bonusHits, bonusMaximum) : 0;

            return round(accuracyPart + comboPart + bonus);
        }

        private static long catchScore(StableScoreAttributes attributes, Play play, double multiplier)
        {
            long fruits = play.Count(HitResult.Great);
            long droplets = play.Count(HitResult.LargeTickHit);
            // Fruits 300, droplets 100, tiny droplets 10.
            long accuracyPart = 300 * fruits + 100 * droplets + 10 * play.Count(HitResult.SmallTickHit);

            long breaks = play.Count(HitResult.Miss) + play.Count(HitResult.LargeTickMiss);
            double progress = comboProgress(ComboCurve.Quadratic, attributes, play, fruits + droplets, breaks);
            long comboPart = round(attributes.ComboScore * multiplier * progress);

            // Bananas: 1100 each.
            long bonus = Math.Min(1100 * play.Count(HitResult.LargeBonus), attributes.BonusScore);
            return accuracyPart + comboPart + bonus;
        }

        /// <summary>
        /// Half of an osu!mania score comes from hit values, half from a bonus that rewards keeping up good hits.
        /// </summary>
        private const double mania_half = 500_000;

        /// <summary>
        /// For each osu!mania judgement: hit value, bonus value, and how it changes the bonus counter.
        /// </summary>
        private static readonly (HitResult Result, double HitValue, double BonusValue, int BonusChange)[] mania_judgements =
        {
            (HitResult.Perfect, 320, 32, 2),
            (HitResult.Great, 300, 32, 1),
            (HitResult.Good, 200, 16, -8),
            (HitResult.Ok, 100, 8, -24),
            (HitResult.Meh, 50, 4, -44),
            (HitResult.Miss, 0, 0, -100),
        };

        /// <remarks>
        /// The server counts the notes a finished play has no judgement for as misses (they come last).
        /// Here, they are notes still to come, so they don't count yet: once every note is judged, both agree.
        /// </remarks>
        private static long maniaScore(Play play, double multiplier)
        {
            long[] counts = mania_judgements.Select(j => play.Count(j.Result)).ToArray();
            long notes = Math.Max(counts.Sum(), mania_judgements.Sum(j => play.Maximum(j.Result)));
            if (notes == 0)
                return 0;

            // Base half: exact, it only depends on the counts.
            double hitValues = 0;
            for (int i = 0; i < counts.Length; i++)
                hitValues += mania_judgements[i].HitValue * counts[i];
            double baseHalf = mania_half * hitValues / (320.0 * notes);

            // Bonus half: replays stable's bonus counter over the judgements in order.
            // When the order isn't known, the judgements are spread evenly over the beatmap instead.
            var order = play.Order?.Select(result => Array.FindIndex(mania_judgements, j => j.Result == result)).Where(index => index >= 0)
                        ?? evenlySpread(counts);

            long bonusCounter = 100;
            double bonusTotal = 0;

            foreach (int judgement in order)
            {
                bonusCounter = Math.Clamp(bonusCounter + mania_judgements[judgement].BonusChange, 0, 100);
                bonusTotal += mania_judgements[judgement].BonusValue * Math.Sqrt(bonusCounter);
            }

            double bonusHalf = mania_half * bonusTotal / (320.0 * notes);

            return round((baseHalf + bonusHalf) * multiplier);
        }

        /// <summary>
        /// Judgement indexes in an order where each kind is spread as evenly as possible.
        /// </summary>
        private static IEnumerable<int> evenlySpread(long[] counts)
        {
            long total = counts.Sum();
            long[] placed = new long[counts.Length];

            for (long step = 1; step <= total; step++)
            {
                // Pick the kind that is furthest behind its fair share at this point.
                int best = 0;
                double bestBehind = double.MinValue;

                for (int i = 0; i < counts.Length; i++)
                {
                    double behind = (double)(counts[i] * step) / total - placed[i];

                    if (behind > bestBehind)
                    {
                        best = i;
                        bestBehind = behind;
                    }
                }

                placed[best]++;
                yield return best;
            }
        }

        /// <summary>
        /// osu!stable's score multiplier of <paramref name="mods"/> (lazer's <c>GetLegacyScoreMultiplier</c>), with scosu's house rules:
        /// Relax and Autopilot count ×1 (their plays have their own leaderboards), and so does Difficulty Adjust when it only changes the approach rate.
        /// Speed mods count their speed (Double Time at 1.15× is ×1.15), and osu!'s Classic ×0.99, or ×0.97 without its note lock.
        /// </summary>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="mods">The mods, with their settings.</param>
        /// <param name="convertedColumns">The key count of an osu! beatmap converted to osu!mania (0 otherwise), for the key mod penalty.</param>
        public static double ScoreMultiplier(int rulesetId, IReadOnlyList<APIMod> mods, long convertedColumns)
        {
            bool scoreV2 = mods.Any(m => m.Acronym == "SV2");
            double multiplier = 1;

            foreach (var mod in mods)
                multiplier *= modMultiplier(rulesetId, mod, scoreV2);

            // Playing a converted beatmap with another key count than the converter chose.
            if (rulesetId == 3 && convertedColumns > 0)
            {
                long? chosen = mods.Select(m => keyCount(m.Acronym)).LastOrDefault(k => k != null);

                if (chosen > convertedColumns)
                    multiplier *= 0.9;
                else if (chosen < convertedColumns)
                    multiplier *= 0.9 - 0.04 * (convertedColumns - chosen.Value);
            }

            return multiplier;
        }

        private static double modMultiplier(int rulesetId, APIMod mod, bool scoreV2)
        {
            // Settings that differ from the defaults. `extended_limits` changes nothing by itself: it only allows values outside 0-10.
            if (mod.Acronym == "DA" && mod.Settings.Keys.Any(setting => setting != "approach_rate" && setting != "extended_limits"))
                return 0.5;

            if (speed(mod) is double speedChange)
                return speedChange;

            switch (mod.Acronym)
            {
                case "NF":
                    return scoreV2 ? 1 : 0.5;

                case "EZ":
                    return 0.5;
            }

            if (rulesetId == 3)
                return 1;

            switch (rulesetId, mod.Acronym)
            {
                case (0, "CL"):
                    return mod.Settings.TryGetValue("classic_note_lock", out object? noteLock) && !Convert.ToBoolean(noteLock, CultureInfo.InvariantCulture) ? 0.97 : 0.99;

                case (0, "HD"):
                    return 1.06;

                case (0, "HR"):
                    return scoreV2 ? 1.10 : 1.06;

                case (0, "FL"):
                    return 1.12;

                case (0, "SO"):
                    return 0.9;

                case (1, "HD" or "HR"):
                    return 1.06;

                case (1, "FL"):
                    return 1.12;

                case (2, "HD"):
                    return scoreV2 ? 1 : 1.06;

                case (2, "HR" or "FL"):
                    return 1.12;

                default:
                    return 1;
            }
        }

        /// <summary>
        /// The default speed of a speed mod (Double Time, Nightcore, Half Time, Daycore) and the speeds lazer allows it,
        /// or <c>null</c> for other mods (scosu's <c>scoring::speed_range</c>).
        /// </summary>
        private static (double Default, double Min, double Max)? speedRange(string acronym) => acronym switch
        {
            "DT" or "NC" => (1.5, 1.01, 2),
            "HT" or "DC" => (0.75, 0.5, 0.99),
            _ => null,
        };

        /// <summary>
        /// The speed <paramref name="mod"/> plays at if it is a speed mod, kept to the speeds lazer allows like the server does.
        /// </summary>
        private static double? speed(APIMod mod)
        {
            if (speedRange(mod.Acronym) is not { } range)
                return null;

            double speedChange = mod.Settings.TryGetValue("speed_change", out object? value) ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : range.Default;
            return Math.Clamp(speedChange, range.Min, range.Max);
        }

        /// <summary>
        /// Whether the best scores on beatmaps with <paramref name="status"/> add up to players' ranked score (scosu's <c>scoring::RANKED_SCORE_STATUSES</c>).
        /// </summary>
        public static bool GivesRankedScore(BeatmapOnlineStatus status) => status is BeatmapOnlineStatus.Ranked or BeatmapOnlineStatus.Approved or BeatmapOnlineStatus.Loved;

        /// <summary>
        /// The score multiplier of <paramref name="mods"/> on <paramref name="beatmap"/>, and whether plays with them are ranked,
        /// on servers that rank plays by osu!stable score.
        /// </summary>
        public static (double Multiplier, bool Ranked) GetModEffects(Ruleset ruleset, IBeatmapInfo beatmap, IReadOnlyList<Mod> mods)
        {
            int rulesetId = ruleset.RulesetInfo.OnlineID;
            var apiMods = mods.Select(m => new APIMod(m)).ToArray();

            // The key count lazer's converter gives an osu! beatmap played in osu!mania: the same as the server's.
            long convertedColumns = rulesetId == 3 && beatmap.Ruleset.OnlineID != 3 ? ruleset.GetVariantForBeatmap(beatmap) : 0;

            return (ScoreMultiplier(rulesetId, apiMods, convertedColumns), apiMods.All(m => IsRankedMod(rulesetId, m)));
        }

        /// <summary>
        /// The stable score of a perfect play with <paramref name="mods"/>: the most ranked score a beatmap can give with them.
        /// </summary>
        /// <param name="attributes">The beatmap's attributes, from the server.</param>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="mods">The mods, with their settings.</param>
        public static long MaximumScore(StableScoreAttributes attributes, int rulesetId, IReadOnlyList<APIMod> mods)
        {
            double multiplier = ScoreMultiplier(rulesetId, mods, attributes.ConvertedColumns);

            switch (rulesetId)
            {
                case 0:
                case 2:
                    return attributes.AccuracyScore + round(attributes.ComboScore * multiplier) + attributes.BonusScore;

                case 1:
                    return round(attributes.AccuracyScore + attributes.ComboScore * multiplier + attributes.BonusScore);

                case 3:
                    return round(2 * mania_half * multiplier);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Mods ranked whatever their settings, per ruleset (scosu's <c>scoring::is_ranked_mod</c>).
        /// </summary>
        private static readonly string[][] always_ranked =
        {
            new[] { "AC", "AL", "AP", "BL", "CL", "MR", "MU", "NS", "PF", "RX", "SD", "SG", "TC" },
            new[] { "AC", "CL", "MU", "PF", "RX", "SD", "SG", "SW" },
            new[] { "CL", "MU", "NS", "PF", "RX", "SD" },
            new[] { "CL", "CO", "MU", "PF", "SD" },
        };

        /// <summary>
        /// Mods ranked only with their default settings, per ruleset.
        /// </summary>
        private static readonly string[][] ranked_with_defaults =
        {
            new[] { "EZ", "FL", "HD", "HR", "NF", "SO", "TD" },
            new[] { "EZ", "FL", "HD", "HR", "NF" },
            new[] { "EZ", "FL", "HD", "HR", "NF" },
            new[] { "4K", "5K", "6K", "7K", "8K", "9K", "EZ", "FI", "FL", "HD", "MR", "NF" },
        };

        /// <summary>
        /// Whether servers that rank plays by osu!stable score rank plays with <paramref name="mod"/> (scosu's <c>scoring::is_ranked_mod</c>).
        /// Unlike lazer's <see cref="Mod.Ranked"/>: Difficulty Adjust (unless it sets an approach rate below 0), Classic,
        /// osu!'s Mirror and osu!taiko's Accuracy Challenge are ranked, so are speed mods at any speed lazer allows,
        /// and so are Relax and Autopilot, on leaderboards of their own.
        /// </summary>
        /// <param name="rulesetId">The online ID of the ruleset played.</param>
        /// <param name="mod">The mod, with the settings that differ from its defaults.</param>
        public static bool IsRankedMod(int rulesetId, APIMod mod)
        {
            if (speedRange(mod.Acronym) is { } speeds)
            {
                if (!mod.Settings.TryGetValue("speed_change", out object? value))
                    return true;

                double speedChange = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return speedChange >= speeds.Min && speedChange <= speeds.Max;
            }

            switch (mod.Acronym)
            {
                case "DA":
                    return !(mod.Settings.TryGetValue("approach_rate", out object? approachRate) && Convert.ToDouble(approachRate, CultureInfo.InvariantCulture) < 0);
            }

            if (rulesetId < 0 || rulesetId >= always_ranked.Length)
                return false;

            if (always_ranked[rulesetId].Contains(mod.Acronym))
                return true;

            return ranked_with_defaults[rulesetId].Contains(mod.Acronym) && mod.Settings.Count == 0;
        }

        private static long? keyCount(string acronym)
        {
            if (acronym.EndsWith('K') && long.TryParse(acronym.AsSpan(0, acronym.Length - 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long keys))
                return keys;

            return null;
        }

        /// <summary>
        /// The share of a perfect play's combo bonus that the play earned: exact when the order of its judgements is known.
        /// </summary>
        private static double comboProgress(ComboCurve curve, StableScoreAttributes attributes, Play play, long comboHits, long breaks)
            => play.Order == null
                ? ComboProgress(curve, attributes.MaxCombo, play.MaxCombo, comboHits, breaks)
                : ExactComboProgress(curve, attributes.MaxCombo, comboRuns(play.Order));

        /// <summary>
        /// The code of a judgement that changes the combo in the order sent to the server, or <c>null</c> for other judgements.
        /// </summary>
        public static char? EncodeJudgement(HitResult result)
        {
            foreach (var (code, judgement) in judgement_codes)
            {
                if (judgement == result)
                    return code;
            }

            return null;
        }

        /// <summary>
        /// Every judgement that changes the combo (<see cref="HitResultExtensions.AffectsCombo"/>), by code. Shared with the server.
        /// </summary>
        private static readonly (char Code, HitResult Result)[] judgement_codes =
        {
            ('P', HitResult.Perfect),
            ('G', HitResult.Great),
            ('g', HitResult.Good),
            ('o', HitResult.Ok),
            ('m', HitResult.Meh),
            ('x', HitResult.Miss),
            ('T', HitResult.LargeTickHit),
            ('t', HitResult.LargeTickMiss),
            ('S', HitResult.SliderTailHit),
#pragma warning disable CS0618
            ('L', HitResult.LegacyComboIncrease),
#pragma warning restore CS0618
            ('b', HitResult.ComboBreak),
        };

        /// <summary>
        /// The judgements of <paramref name="order"/>, or <c>null</c> unless it holds exactly the play's judgements
        /// that change the combo, with the play's max combo as its longest combo (as the server checks).
        /// </summary>
        private static HitResult[]? parseOrder(string? order, IReadOnlyDictionary<HitResult, int> statistics, int maxCombo)
        {
            if (order == null)
                return null;

            var results = new HitResult[order.Length];

            for (int i = 0; i < order.Length; i++)
            {
                int index = Array.FindIndex(judgement_codes, j => j.Code == order[i]);
                if (index < 0)
                    return null;

                results[i] = judgement_codes[index].Result;
            }

            foreach (var (_, result) in judgement_codes)
            {
                if (results.Count(r => r == result) != Math.Max(statistics.GetValueOrDefault(result), 0))
                    return null;
            }

            return comboRuns(results).Max() == maxCombo ? results : null;
        }

        /// <summary>
        /// The length of each run of combo in <paramref name="order"/>, the last one included.
        /// </summary>
        private static List<long> comboRuns(IEnumerable<HitResult> order)
        {
            var runs = new List<long>();
            long combo = 0;

            foreach (var result in order)
            {
                if (result.IncreasesCombo())
                    combo++;
                else
                {
                    runs.Add(combo);
                    combo = 0;
                }
            }

            runs.Add(combo);
            return runs;
        }

        public enum ComboCurve
        {
            /// <summary>
            /// osu! and osu!catch: each hit's bonus grows with the combo, so a run of n is worth ~n².
            /// </summary>
            Quadratic,

            /// <summary>
            /// osu!taiko: the bonus grows one step every 10 combo and stops growing at 100 combo.
            /// </summary>
            Capped,
        }

        /// <summary>
        /// The share (0 to 1) of a perfect play's combo bonus that a play earned.
        /// </summary>
        /// <remarks>
        /// The longest run is known. The other hits form <paramref name="breaks"/> more runs of unknown lengths:
        /// at worst all equal (the lowest total), at best as long as the longest run allows. The result leans 70/30 towards
        /// the optimistic end and is capped at 1.2 times the midpoint. A full combo gives exactly 1.
        /// </remarks>
        /// <param name="curve">How the combo bonus grows with the combo.</param>
        /// <param name="mapMaxCombo">The beatmap's maximum combo.</param>
        /// <param name="maxCombo">The play's longest combo.</param>
        /// <param name="comboHits">Hits that added to the combo.</param>
        /// <param name="breaks">Misses that reset the combo.</param>
        public static double ComboProgress(ComboCurve curve, long mapMaxCombo, long maxCombo, long comboHits, long breaks)
        {
            if (mapMaxCombo <= 0)
                return 1;

            double longest = Math.Clamp(maxCombo, 0, mapMaxCombo);
            double rest = Math.Max(comboHits - longest, 0);
            double runs = Math.Max(breaks, rest > 0 ? 1 : 0);

            double lower, upper;

            if (rest == 0 || longest == 0)
                lower = upper = runValue(curve, longest);
            else
            {
                lower = runValue(curve, longest) + runs * runValue(curve, rest / runs);
                double fullRuns = Math.Min(Math.Floor(rest / longest), runs);
                double leftover = Math.Max(rest - fullRuns * longest, 0);
                upper = (1 + fullRuns) * runValue(curve, longest) + runValue(curve, leftover);
            }

            double estimate = Math.Min(0.3 * lower + 0.7 * upper, 1.2 * (lower + upper) / 2);
            return Math.Clamp(estimate / runValue(curve, mapMaxCombo), 0, 1);
        }

        /// <summary>
        /// The share (0 to 1) of a perfect play's combo bonus earned by a play whose runs of combo are known.
        /// </summary>
        public static double ExactComboProgress(ComboCurve curve, long mapMaxCombo, IEnumerable<long> runs)
        {
            if (mapMaxCombo <= 0)
                return 1;

            double earned = runs.Sum(run => runValue(curve, run));
            return Math.Clamp(earned / runValue(curve, mapMaxCombo), 0, 1);
        }

        /// <summary>
        /// The combo bonus of a run of <paramref name="length"/> hits.
        /// </summary>
        private static double runValue(ComboCurve curve, double length)
        {
            switch (curve)
            {
                case ComboCurve.Quadratic:
                    // Sum of (combo − 1) over the run.
                    return length * Math.Max(length - 1, 0) / 2;

                default:
                    // Between whole lengths, interpolate.
                    double whole = Math.Floor(length);
                    double below = cappedRunValue((long)whole);
                    return below + (cappedRunValue((long)whole + 1) - below) * (length - whole);
            }
        }

        /// <summary>
        /// Sum of <c>min(combo, 100) / 10</c> (integer division) for combo = 0 .. length − 1.
        /// </summary>
        private static double cappedRunValue(long length)
        {
            long growing = Math.Clamp(length, 0, 100);
            long tens = growing / 10, rest = growing % 10;
            return 10 * tens * Math.Max(tens - 1, 0) / 2 + rest * tens + 10 * Math.Max(length - 100, 0);
        }

        /// <summary>
        /// <paramref name="part"/> / <paramref name="whole"/>, limited to 0 to 1 (and 1 when there is nothing to hit).
        /// </summary>
        private static double ratio(long part, long whole) => whole <= 0 ? 1 : Math.Clamp((double)part / whole, 0, 1);

        /// <summary>
        /// Rounds like the server: halves away from zero.
        /// </summary>
        private static long round(double value) => (long)Math.Round(value, MidpointRounding.AwayFromZero);

        private readonly record struct Play(IReadOnlyDictionary<HitResult, int> Statistics, IReadOnlyDictionary<HitResult, int> MaximumStatistics, long MaxCombo,
                                            HitResult[]? Order)
        {
            public long Count(HitResult result) => Math.Max(Statistics.GetValueOrDefault(result), 0);

            public long Maximum(HitResult result) => Math.Max(MaximumStatistics.GetValueOrDefault(result), 0);

            /// <summary>
            /// Whether the play has <paramref name="result"/> judgements (submitted scores leave out the results they have none of).
            /// </summary>
            public bool Has(HitResult result) => Statistics.GetValueOrDefault(result) != 0;
        }
    }
}
