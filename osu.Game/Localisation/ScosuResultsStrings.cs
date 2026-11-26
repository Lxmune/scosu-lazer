// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the scosu results screen and song select statistics: the score lost on a play, and the local user's history on a beatmap.
    /// </summary>
    public static class ScosuResultsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuResults";

        /// <summary>
        /// "Score Lost"
        /// </summary>
        public static LocalisableString ScoreLostHeader => new TranslatableString(getKey(@"score_lost_header"), @"Score Lost");

        /// <summary>
        /// "Where each circle was hit, turned so the arrow points the way the cursor came from. Brighter: more hits there. ×: hits outside the circle."
        /// </summary>
        public static LocalisableString HeatmapLegend => new TranslatableString(getKey(@"heatmap_legend"),
            @"Where each circle was hit, turned so the arrow points the way the cursor came from. Brighter: more hits there. ×: hits outside the circle.");

        /// <summary>
        /// "This play's judgements don't say where its circles were hit, so there's nothing to place on the heatmap."
        /// </summary>
        public static LocalisableString HeatmapUnavailable => new TranslatableString(getKey(@"heatmap_unavailable"),
            @"This play's judgements don't say where its circles were hit, so there's nothing to place on the heatmap.");

        /// <summary>
        /// "Early / Late"
        /// </summary>
        public static LocalisableString EarlyLateTitle => new TranslatableString(getKey(@"early_late_title"), @"Early / Late");

        /// <summary>
        /// "{0} early · {1} late"
        /// </summary>
        public static LocalisableString EarlyLateShares(LocalisableString early, LocalisableString late) => new TranslatableString(getKey(@"early_late_shares"),
            @"{0} early · {1} late", early, late);

        /// <summary>
        /// "Of the maximum"
        /// </summary>
        public static LocalisableString ShareOfMaximum => new TranslatableString(getKey(@"share_of_maximum"), @"Of the maximum");

        /// <summary>
        /// "{0} of max"
        /// </summary>
        public static LocalisableString PercentOfMaximum(LocalisableString percent) => new TranslatableString(getKey(@"percent_of_maximum"), @"{0} of max", percent);

        /// <summary>
        /// "Score missing"
        /// </summary>
        public static LocalisableString ScoreMissing => new TranslatableString(getKey(@"score_missing"), @"Score missing");

        /// <summary>
        /// "Lost to combo breaks"
        /// </summary>
        public static LocalisableString LostToComboBreaks => new TranslatableString(getKey(@"lost_to_combo_breaks"), @"Lost to combo breaks");

        /// <summary>
        /// "Lost to judgements"
        /// </summary>
        public static LocalisableString LostToJudgements => new TranslatableString(getKey(@"lost_to_judgements"), @"Lost to judgements");

        /// <summary>
        /// "~{0}"
        /// </summary>
        public static LocalisableString Estimate(LocalisableString value) => new TranslatableString(getKey(@"estimate"), @"~{0}", value);

        /// <summary>
        /// "The play's osu!stable score against the best score possible on this beatmap with the same mods. A value marked with ~ is an estimate, because the order of the play's judgements is unknown."
        /// </summary>
        public static LocalisableString ScoreLostTooltip => new TranslatableString(getKey(@"score_lost_tooltip"),
            @"The play's osu!stable score against the best score possible on this beatmap with the same mods. A value marked with ~ is an estimate, because the order of the play's judgements is unknown.");

        /// <summary>
        /// "{0} plays, {1} passed"
        /// </summary>
        public static LocalisableString PlaysAndPassRate(LocalisableString plays, LocalisableString passRate) => new TranslatableString(getKey(@"plays_and_pass_rate"), @"{0} plays, {1} passed", plays, passRate);

        /// <summary>
        /// "{0} unfinished"
        /// </summary>
        public static LocalisableString Unfinished(LocalisableString count) => new TranslatableString(getKey(@"unfinished"), @"{0} unfinished", count);

        /// <summary>
        /// "Closest FC: {0}x / {1}x, {2} misses"
        /// </summary>
        public static LocalisableString ClosestAttempt(LocalisableString combo, LocalisableString mapCombo, LocalisableString misses)
            => new TranslatableString(getKey(@"closest_attempt"), @"Closest FC: {0}x / {1}x, {2} misses", combo, mapCombo, misses);

        /// <summary>
        /// "Closest FC: full combo"
        /// </summary>
        public static LocalisableString ClosestAttemptFullCombo => new TranslatableString(getKey(@"closest_attempt_full_combo"), @"Closest FC: full combo");

        /// <summary>
        /// "Beat your best: about +{0}"
        /// </summary>
        public static LocalisableString EstimatedGain(LocalisableString gain) => new TranslatableString(getKey(@"estimated_gain"), @"Beat your best: about +{0}", gain);

        /// <summary>
        /// "Your history on this beatmap in the selected mode: plays, pass rate, unfinished starts, the closest attempt to a full combo, and the estimated ranked score beating your best would add."
        /// </summary>
        public static LocalisableString HistoryTooltip => new TranslatableString(getKey(@"history_tooltip"),
            @"Your history on this beatmap in the selected mode: plays, pass rate, unfinished starts, the closest attempt to a full combo, and the estimated ranked score beating your best would add.");

        /// <summary>
        /// "Copy the score's link"
        /// </summary>
        public static LocalisableString ShareScore => new TranslatableString(getKey(@"share_score"), @"Copy the score's link");

        /// <summary>
        /// "Score link copied: paste it in Discord or anywhere to show your score card."
        /// </summary>
        public static LocalisableString ShareScoreCopied => new TranslatableString(getKey(@"share_score_copied"), @"Score link copied: paste it in Discord or anywhere to show your score card.");

        /// <summary>
        /// "This score isn't on the server yet, so it has no link. Try again in a moment."
        /// </summary>
        public static LocalisableString ShareScoreNotSubmitted => new TranslatableString(getKey(@"share_score_not_submitted"), @"This score isn't on the server yet, so it has no link. Try again in a moment.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
