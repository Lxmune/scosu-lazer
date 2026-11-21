// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: a card in one of the main menu's corners. The menu anchors it to its corner (and may move it to another);
    /// the card keeps its margin on <see cref="Margined"/> (not on itself), so that a hidden card leaves no gap,
    /// and keeps that margin on the side facing the screen's edge.
    /// </summary>
    public abstract partial class CornerCard : VisibilityContainer
    {
        public const float CARD_WIDTH = 300;

        private const float edge_margin = 10;

        /// <summary>
        /// The card's visible content, which gets the margin.
        /// </summary>
        protected Drawable Margined { get; set; } = null!;

        private Anchor appliedAnchor = (Anchor)(-1);

        /// <summary>
        /// Called once the card's anchor is known, and again whenever the menu changes it.
        /// </summary>
        protected virtual void OnAnchorApplied(Anchor anchor)
        {
        }

        protected override void Update()
        {
            base.Update();

            // The anchor is set (and, for the event card, changed) by the menu, so follow it.
            if (Anchor == appliedAnchor || Margined == null)
                return;

            appliedAnchor = Anchor;
            OnAnchorApplied(Anchor);

            bool bottom = (Anchor & Anchor.y2) != 0;

            Margined.Margin = new MarginPadding
            {
                Horizontal = edge_margin,
                Top = bottom ? 0 : edge_margin,
                Bottom = bottom ? edge_margin : 0,
            };
        }
    }
}
