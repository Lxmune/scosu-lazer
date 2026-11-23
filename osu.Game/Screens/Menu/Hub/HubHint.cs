// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// scosu: "On scosu now" with a chevron under it, at the bottom of the menu's first screen just above the bar.
    /// Says there is more below (the chevron drifts down and back), lights up under the cursor, and opens the hub when clicked
    /// or scrolled on. The menu shows it while the band is out and the hub closed, and scrolls it away with the band.
    /// </summary>
    public partial class HubHint : OsuClickableContainer
    {
        public const float HEIGHT = 36;

        /// <summary>
        /// Called when the hint is scrolled on; returns whether the scroll was handled (returning false lets volume scrolling run).
        /// </summary>
        public Func<ScrollEvent, bool>? Scrolled;

        private OsuSpriteText label = null!;
        private SpriteIcon chevron = null!;

        public HubHint()
        {
            // Before load: OsuClickableContainer sets its content up for auto-sizing there.
            AutoSizeAxes = Axes.X;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                // A wider hit area than the text.
                Padding = new MarginPadding { Horizontal = 32 },
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    label = new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = MenuHubStrings.OnScosuNow,
                        Font = OsuFont.GetFont(size: 17, weight: FontWeight.SemiBold),
                        Shadow = true,
                    },
                    chevron = new SpriteIcon
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.ChevronDown,
                        Size = new Vector2(12),
                        Margin = new MarginPadding { Top = 6 },
                        Colour = ScosuMenuStyle.TEXT_DIM,
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            chevron.MoveToY(3, 900, Easing.InOutSine).Then().MoveToY(0, 900, Easing.InOutSine).Loop();
        }

        protected override bool OnHover(HoverEvent e)
        {
            label.FadeColour(ScosuMenuStyle.VIOLET_INK, HubTile.HOVER_DURATION, Easing.OutQuint);
            chevron.FadeColour(ScosuMenuStyle.VIOLET_INK, HubTile.HOVER_DURATION, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            label.FadeColour(Color4.White, HubTile.HOVER_DURATION, Easing.OutQuint);
            chevron.FadeColour(ScosuMenuStyle.TEXT_DIM, HubTile.HOVER_DURATION, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnScroll(ScrollEvent e) => Scrolled?.Invoke(e) ?? false;
    }
}
