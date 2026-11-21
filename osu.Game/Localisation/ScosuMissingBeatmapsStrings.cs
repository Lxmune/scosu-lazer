// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's "download missing beatmaps" settings.
    /// </summary>
    public static class ScosuMissingBeatmapsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuMissingBeatmaps";

        /// <summary>
        /// "Missing beatmaps"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Missing beatmaps");

        /// <summary>
        /// "Ranked maps"
        /// </summary>
        public static LocalisableString Kind => new TranslatableString(getKey(@"kind"), @"Ranked maps");

        /// <summary>
        /// "In the selected mode, the maps you haven't done this on yet, the most score first."
        /// </summary>
        public static LocalisableString KindHint => new TranslatableString(getKey(@"kind_hint"), @"In the selected mode, the maps you haven't done this on yet, the most score first.");

        /// <summary>
        /// "you haven't played"
        /// </summary>
        public static LocalisableString NotPlayed => new TranslatableString(getKey(@"not_played"), @"you haven't played");

        /// <summary>
        /// "you haven't passed"
        /// </summary>
        public static LocalisableString NotPassed => new TranslatableString(getKey(@"not_passed"), @"you haven't passed");

        /// <summary>
        /// "without your full combo"
        /// </summary>
        public static LocalisableString NoFullCombo => new TranslatableString(getKey(@"no_full_combo"), @"without your full combo");

        /// <summary>
        /// "without your S rank"
        /// </summary>
        public static LocalisableString NoSRank => new TranslatableString(getKey(@"no_s_rank"), @"without your S rank");

        /// <summary>
        /// "Minimum star rating"
        /// </summary>
        public static LocalisableString MinimumStars => new TranslatableString(getKey(@"minimum_stars"), @"Minimum star rating");

        /// <summary>
        /// "Maps to download at most"
        /// </summary>
        public static LocalisableString Limit => new TranslatableString(getKey(@"limit"), @"Maps to download at most");

        /// <summary>
        /// "Looking for missing maps..."
        /// </summary>
        public static LocalisableString Searching => new TranslatableString(getKey(@"searching"), @"Looking for missing maps...");

        /// <summary>
        /// "Download {0} maps you don't have"
        /// </summary>
        public static LocalisableString Download(int count)
            => new TranslatableString(getKey(@"download"), count == 1 ? @"Download {0} map you don't have" : @"Download {0} maps you don't have", count);

        /// <summary>
        /// "You have every map that matches"
        /// </summary>
        public static LocalisableString NothingMissing => new TranslatableString(getKey(@"nothing_missing"), @"You have every map that matches");

        /// <summary>
        /// "Couldn't get the list from the server"
        /// </summary>
        public static LocalisableString Failed => new TranslatableString(getKey(@"failed"), @"Couldn't get the list from the server");

        /// <summary>
        /// "Log in to find the maps you're missing"
        /// </summary>
        public static LocalisableString LogIn => new TranslatableString(getKey(@"log_in"), @"Log in to find the maps you're missing");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
