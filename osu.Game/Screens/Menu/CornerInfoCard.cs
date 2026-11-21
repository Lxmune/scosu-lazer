// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: a clickable card in one of the main menu's corners (today's score, the weekly report, the event).
    /// It slides in from its corner's edge when shown, lifts a little with a violet outline when hovered, and flashes when pressed.
    /// </summary>
    public abstract partial class CornerInfoCard : CornerCard
    {
        private const float slide_distance = 40;
        private const float slide_duration = 500;

        private Container? hoverOverlay;
        private Box? pressFlash;

        /// <summary>
        /// The card's rounded surface; backdrops (a graph, a picture) go in it so they are clipped to its corners.
        /// </summary>
        protected Container Surface { get; private set; } = null!;

        /// <summary>
        /// The card's background: glass, then the given backdrops, then the hover outline and press flash.
        /// Call this again when the card's content is rebuilt.
        /// </summary>
        protected Drawable CreateSurface(params Drawable[] backdrops)
        {
            Surface = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = ScosuMenuStyle.CORNER_RADIUS,
                CornerExponent = 2.5f,
                BorderThickness = 1.5f,
                BorderColour = ScosuMenuStyle.OUTLINE,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ScosuMenuStyle.GLASS,
                },
            };

            Surface.AddRange(backdrops);

            Surface.AddRange(new Drawable[]
            {
                hoverOverlay = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = ScosuMenuStyle.CORNER_RADIUS,
                    CornerExponent = 2.5f,
                    BorderThickness = 1.5f,
                    BorderColour = ScosuMenuStyle.VIOLET.Opacity(0.4f),
                    Alpha = 0,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ScosuMenuStyle.FILL,
                    },
                },
                pressFlash = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0,
                },
            });

            return Surface;
        }

        private float slideOffset => (Anchor & Anchor.x0) != 0 ? -slide_distance : slide_distance;

        protected override bool OnHover(HoverEvent e)
        {
            hoverOverlay?.FadeIn(200, Easing.OutQuint);
            Margined.MoveToY(-2, 200, Easing.OutQuint);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverOverlay?.FadeOut(300, Easing.OutQuint);
            Margined.MoveToY(0, 300, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            pressFlash?.FadeTo(0.1f, 60);
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            pressFlash?.FadeOut(250, Easing.OutQuint);
            base.OnMouseUp(e);
        }

        protected override void PopIn()
        {
            Margined.MoveToX(slideOffset).MoveToX(0, slide_duration, Easing.OutQuint);
            this.FadeIn(slide_duration, Easing.OutQuint);
        }

        protected override void PopOut() => this.FadeOut(slide_duration, Easing.OutQuint);
    }
}
