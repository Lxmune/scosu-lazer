// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's additions to the integration settings.
    /// </summary>
    public static class ScosuIntegrationStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuIntegration";

        /// <summary>
        /// "Link your Discord account"
        /// </summary>
        public static LocalisableString LinkDiscord => new TranslatableString(getKey(@"link_discord"), @"Link your Discord account");

        /// <summary>
        /// "Discord: linked as {0}"
        /// </summary>
        public static LocalisableString DiscordLinked(string name) => new TranslatableString(getKey(@"discord_linked"), @"Discord: linked as {0}", name);

        /// <summary>
        /// "Opens your account settings on the website. ScosuBot then knows who you are, and DMs you when someone takes your #1."
        /// </summary>
        public static LocalisableString DiscordLinkTooltip => new TranslatableString(getKey(@"discord_link_tooltip"), @"Opens your account settings on the website. ScosuBot then knows who you are, and DMs you when someone takes your #1.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
