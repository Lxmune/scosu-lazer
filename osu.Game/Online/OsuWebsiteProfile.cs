// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Online.Chat;
using osu.Game.Users;

namespace osu.Game.Online
{
    /// <summary>
    /// scosu: beatmaps' mappers (and nominators) are osu! players, who mostly have no account here, and whose osu! id can
    /// be another player's id here. Their profile is the one on osu.ppy.sh, opened in the browser.
    /// </summary>
    public static class OsuWebsiteProfile
    {
        public static LinkDetails LinkFor(IUser user) => new LinkDetails(LinkAction.External, UrlFor(user));

        public static string UrlFor(IUser user) => user.OnlineID > 0
            ? $@"https://osu.ppy.sh/users/{user.OnlineID}"
            : $@"https://osu.ppy.sh/users/{Uri.EscapeDataString(user.Username)}";
    }
}
