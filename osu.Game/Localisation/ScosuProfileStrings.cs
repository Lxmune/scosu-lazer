// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's additions to the profile.
    /// </summary>
    public static class ScosuProfileStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuProfile";

        /// <summary>
        /// "Restricted"
        /// </summary>
        public static LocalisableString Restricted => new TranslatableString(getKey(@"restricted"), @"Restricted");

        /// <summary>
        /// "{0} broke the rules: this account is left out of rankings and leaderboards."
        /// </summary>
        public static LocalisableString RestrictedHint(string username)
            => new TranslatableString(getKey(@"restricted_hint"), @"{0} broke the rules: this account is left out of rankings and leaderboards.", username);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
