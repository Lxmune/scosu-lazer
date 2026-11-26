// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: what the results screen's "Combo Timeline" and "Costliest Breaks" share: the words and colour of a break,
    /// finding what the breaks cost (and the score race), and watching the replay from one.
    /// </summary>
    public static class ComboBreakDisplay
    {
        /// <summary>
        /// How long before a break watching from it starts, to see what led to it.
        /// </summary>
        public const double WATCH_LEAD_IN = 1500;

        /// <summary>
        /// "1:23", for a time in the beatmap.
        /// </summary>
        public static string FormatTime(double time)
        {
            var span = TimeSpan.FromMilliseconds(Math.Max(0, time));
            return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"m\:ss");
        }

        /// <summary>
        /// Whether the break is a slider letting go (a missed tick or repeat in osu!) rather than a missed object.
        /// </summary>
        public static bool IsSliderBreak(ComboBreakAnalysis.ComboBreak comboBreak, int rulesetId) => rulesetId == 0 && comboBreak.Result != HitResult.Miss;

        public static LocalisableString KindName(ComboBreakAnalysis.ComboBreak comboBreak, int rulesetId)
            => IsSliderBreak(comboBreak, rulesetId) ? ScosuOutcomeStrings.SliderBreak : ScosuOutcomeStrings.Miss;

        public static Color4 KindColour(ComboBreakAnalysis.ComboBreak comboBreak, int rulesetId, OsuColour colours)
            => IsSliderBreak(comboBreak, rulesetId) ? colours.Orange1 : colours.Red1;

        /// <summary>
        /// "combo 312 → 0", or for a break that came right after another (and so reset no combo), says so.
        /// Hitting it would still have joined the runs on both sides, which is what it cost.
        /// </summary>
        public static LocalisableString ComboText(ComboBreakAnalysis.ComboBreak comboBreak)
            => comboBreak.ComboLost > 0 ? ScosuOutcomeStrings.ComboReset(comboBreak.ComboLost.ToLocalisableString(@"N0")) : ScosuOutcomeStrings.RightAfterAnotherBreak;

        /// <summary>
        /// "1:23 · miss", "combo 312 → 0", "cost −1,234,567" (once <paramref name="costsKnown"/>), "Click to watch from 1:18",
        /// and "+3 more breaks around it" when <paramref name="nearby"/> other breaks are drawn together with it (see <see cref="ComboBreakAnalysis.GroupBreaks"/>).
        /// </summary>
        public static LocalisableString Tooltip(ComboBreakAnalysis.ComboBreak comboBreak, int rulesetId, bool costsKnown, bool canWatch, int nearby = 0)
        {
            var text = LocalisableString.Interpolate(
                $"{FormatTime(comboBreak.Time)} · {KindName(comboBreak, rulesetId)}\n{ComboText(comboBreak)}");

            if (costsKnown && comboBreak.Cost is long cost)
                text = LocalisableString.Interpolate($"{text}\n{ScosuOutcomeStrings.BreakCost(cost.ToLocalisableString(@"N0"))}");

            if (nearby > 0)
                text = LocalisableString.Interpolate($"{text}\n{ScosuOutcomeStrings.MoreBreaksNearby(nearby)}");

            if (canWatch)
                text = LocalisableString.Interpolate($"{text}\n{ScosuOutcomeStrings.ClickToWatchFrom(FormatTime(WatchTime(comboBreak)))}");

            return text;
        }

        /// <summary>
        /// Where watching from a break starts.
        /// </summary>
        public static double WatchTime(ComboBreakAnalysis.ComboBreak comboBreak) => comboBreak.Time - WATCH_LEAD_IN;

        /// <summary>
        /// Watches the score's replay from a little before the break.
        /// </summary>
        public static void Watch(OsuGame? game, ScoreInfo score, ComboBreakAnalysis.ComboBreak comboBreak)
            => game?.PresentScore(score, ScorePresentType.Gameplay, WatchTime(comboBreak));

        /// <summary>
        /// How many times the score race of the "Combo Timeline" samples the play (see <see cref="ComboBreakAnalysis.ComputeScoreRace"/>).
        /// </summary>
        public const int RACE_SAMPLES = 160;

        /// <summary>
        /// On servers that rank by osu!stable score, finds what each break of <paramref name="analysis"/> cost (see <see cref="ComboBreakAnalysis.ComputeCosts"/>)
        /// and the play's score over time against a full combo's (see <see cref="ComboBreakAnalysis.ComputeScoreRace"/>), from one fetch of the beatmap's attributes.
        /// The breaks' <see cref="ComboBreakAnalysis.ComboBreak.Cost"/> and the <see cref="ComboBreakAnalysis.Race"/> are only to be read once the task is done.
        /// </summary>
        public static Task AnalyseStableScoreAsync(ScoreInfo score, ComboBreakAnalysis analysis, IAPIProvider api, StableScoreAttributesCache? attributesCache, CancellationToken token)
        {
            int beatmapId = score.BeatmapInfo?.OnlineID ?? 0;

            if (attributesCache == null || !api.Endpoints.RanksByStableScore || beatmapId <= 0)
                return Task.CompletedTask;

            // Read on this thread: the score may be changed after (its hit events are cleared when the results screen is left).
            var ruleset = score.Ruleset.CreateInstance();
            int rulesetId = score.Ruleset.OnlineID;
            var statistics = new Dictionary<HitResult, int>(score.Statistics);
            var maximumStatistics = new Dictionary<HitResult, int>(score.MaximumStatistics);
            var difficulty = score.BeatmapInfo!.Difficulty;
            var mods = score.Mods;

            return Task.Run(async () =>
            {
                var attributes = await attributesCache.GetAttributesAsync(beatmapId, rulesetId, token).ConfigureAwait(false);

                if (attributes == null)
                    return;

                double multiplier = StableScoreCalculator.ScoreMultiplier(ruleset, difficulty, mods);

                analysis.ComputeCosts(attributes, rulesetId, statistics, maximumStatistics, multiplier);
                analysis.ComputeScoreRace(attributes, rulesetId, statistics, maximumStatistics, multiplier, RACE_SAMPLES);
            }, token);
        }
    }
}
