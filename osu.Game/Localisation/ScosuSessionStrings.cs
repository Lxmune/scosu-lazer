// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's session summary notification.
    /// </summary>
    public static class ScosuSessionStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuSession";

        /// <summary>
        /// "This session: {0}"
        /// </summary>
        public static LocalisableString Summary(string parts) => new TranslatableString(getKey(@"summary"), @"This session: {0}", parts);

        /// <summary>
        /// "This session: {0}. Best: {1}, {2}"
        /// </summary>
        public static LocalisableString SummaryWithBest(string parts, string map, string accuracy)
            => new TranslatableString(getKey(@"summary_with_best"), @"This session: {0}. Best: {1}, {2}", parts, map, accuracy);

        /// <summary>
        /// "{0} plays"
        /// </summary>
        public static LocalisableString Plays(int count) => new TranslatableString(getKey(@"plays"), count == 1 ? @"{0} play" : @"{0} plays", count);

        /// <summary>
        /// "{0} passes"
        /// </summary>
        public static LocalisableString Passes(int count) => new TranslatableString(getKey(@"passes"), count == 1 ? @"{0} pass" : @"{0} passes", count);

        /// <summary>
        /// "{0} ranked score ({1})"
        /// </summary>
        public static LocalisableString RankedScore(string score, string ruleset) => new TranslatableString(getKey(@"ranked_score"), @"{0} ranked score ({1})", score, ruleset);

        /// <summary>
        /// "{0} full combos"
        /// </summary>
        public static LocalisableString FullCombos(int count) => new TranslatableString(getKey(@"full_combos"), count == 1 ? @"{0} full combo" : @"{0} full combos", count);

        /// <summary>
        /// "{0} S ranks"
        /// </summary>
        public static LocalisableString SRanks(int count) => new TranslatableString(getKey(@"s_ranks"), count == 1 ? @"{0} S rank" : @"{0} S ranks", count);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
