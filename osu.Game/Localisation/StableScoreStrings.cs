// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings shown on servers that rank by osu!stable score, in place of the ones about performance points.
    /// </summary>
    public static class StableScoreStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.StableScore";

        /// <summary>
        /// "Best Scores"
        /// </summary>
        public static LocalisableString BestScoresTitle => new TranslatableString(getKey(@"best_scores_title"), @"Best Scores");

        /// <summary>
        /// "Max {0}"
        /// </summary>
        public static LocalisableString MaximumScore(LocalisableString maximum) => new TranslatableString(getKey(@"maximum_score"), @"Max {0}", maximum);

        /// <summary>
        /// "Best {0} / Max {1}"
        /// </summary>
        public static LocalisableString BestAndMaximumScore(LocalisableString best, LocalisableString maximum)
            => new TranslatableString(getKey(@"best_and_maximum_score"), @"Best {0} / Max {1}", best, maximum);

        /// <summary>
        /// "Best {0} / Max {1} (−{2})"
        /// </summary>
        public static LocalisableString BestAndMaximumScoreWithGap(LocalisableString best, LocalisableString maximum, LocalisableString gap)
            => new TranslatableString(getKey(@"best_and_maximum_score_with_gap"), @"Best {0} / Max {1} (−{2})", best, maximum, gap);

        /// <summary>
        /// "The osu!stable score of a perfect play with the selected mods, next to your best score on this beatmap: the ranked score you can still gain on it."
        /// </summary>
        public static LocalisableString MaximumScoreTooltip => new TranslatableString(getKey(@"maximum_score_tooltip"),
            @"The osu!stable score of a perfect play with the selected mods, next to your best score on this beatmap: the ranked score you can still gain on it.");

        /// <summary>
        /// "ranked score"
        /// </summary>
        public static LocalisableString RankedScoreGainLabel => new TranslatableString(getKey(@"ranked_score_gain_label"), @"ranked score");

        /// <summary>
        /// "This play won't give ranked score"
        /// </summary>
        public static LocalisableString NoRankedScoreTitle => new TranslatableString(getKey(@"no_ranked_score_title"), @"This play won't give ranked score");

        /// <summary>
        /// "Only ranked, approved and loved beatmaps give ranked score."
        /// </summary>
        public static LocalisableString NoRankedScoreForBeatmap => new TranslatableString(getKey(@"no_ranked_score_for_beatmap"), @"Only ranked, approved and loved beatmaps give ranked score.");

        /// <summary>
        /// "These mods are unranked: {0}."
        /// </summary>
        public static LocalisableString NoRankedScoreForMods(string mods) => new TranslatableString(getKey(@"no_ranked_score_for_mods"), @"These mods are unranked: {0}.", mods);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
