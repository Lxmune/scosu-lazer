// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// scosu: a scroll container in the hub that only takes the scroll it can use. At its top (or bottom), or when everything fits,
    /// the wheel goes on to what is behind it: the hub page, then the menu, which scrolls between the band and the hub.
    /// </summary>
    public partial class HubScrollContainer : OsuScrollContainer
    {
        protected override bool OnScroll(ScrollEvent e)
        {
            if (ScrollContent.DrawHeight <= DisplayableContent + 0.5f)
                return false;

            bool up = e.ScrollDelta.Y > 0;

            if (up ? Current <= 0 : IsScrolledToEnd())
                return false;

            return base.OnScroll(e);
        }
    }
}
