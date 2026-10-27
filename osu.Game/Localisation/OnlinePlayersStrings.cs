// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the main menu's list of the other players online.
    /// </summary>
    public static class OnlinePlayersStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.OnlinePlayers";

        /// <summary>
        /// "{0} online"
        /// </summary>
        public static LocalisableString OnlineCount(LocalisableString count) => new TranslatableString(getKey(@"online_count"), @"{0} online", count);

        /// <summary>
        /// "and {0} more"
        /// </summary>
        public static LocalisableString AndMore(LocalisableString count) => new TranslatableString(getKey(@"and_more"), @"and {0} more", count);

        /// <summary>
        /// "See everyone online"
        /// </summary>
        public static LocalisableString ShowAll => new TranslatableString(getKey(@"show_all"), @"See everyone online");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
