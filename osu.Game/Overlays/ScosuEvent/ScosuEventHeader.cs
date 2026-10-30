// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Game.Localisation;

namespace osu.Game.Overlays.ScosuEvent
{
    public partial class ScosuEventHeader : OverlayHeader
    {
        protected override OverlayTitle CreateTitle() => new ScosuEventTitle();

        private partial class ScosuEventTitle : OverlayTitle
        {
            public ScosuEventTitle()
            {
                Title = ScosuEventStrings.HeaderTitle;
                Description = ScosuEventStrings.HeaderDescription;
                Icon = FontAwesome.Solid.Trophy;
            }
        }
    }
}
