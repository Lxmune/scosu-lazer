// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the main menu's look, shared by the band, the corner cards and the hub's tiles.
    /// </summary>
    public static class ScosuMenuStyle
    {
        /// <summary>
        /// The background of cards and tiles: dark, slightly violet, see-through.
        /// </summary>
        public static readonly Color4 GLASS = Color4Extensions.FromHex(@"17131f").Opacity(0.72f);

        /// <summary>
        /// A hub tile's base: a neutral dark grey (the violet is left to the accents), nearly opaque, since the tiles sit
        /// straight on the menu's background.
        /// </summary>
        public static readonly Color4 TILE = Color4Extensions.FromHex(@"0f0f11").Opacity(0.95f);

        /// <summary>
        /// The band's and the hub bar's background, a little denser than <see cref="GLASS"/>.
        /// </summary>
        public static readonly Color4 BAND = Color4Extensions.FromHex(@"120f19").Opacity(0.82f);

        /// <summary>
        /// The thin outline of cards and tiles.
        /// </summary>
        public static readonly Color4 OUTLINE = Color4.White.Opacity(0.11f);

        /// <summary>
        /// A light fill inside a card (counters, chips, hovered buttons).
        /// </summary>
        public static readonly Color4 FILL = Color4.White.Opacity(0.07f);

        public static readonly Color4 VIOLET = Color4Extensions.FromHex(@"a68bff");
        public static readonly Color4 VIOLET_DEEP = Color4Extensions.FromHex(@"7556ec");
        public static readonly Color4 VIOLET_INK = Color4Extensions.FromHex(@"d9ceff");

        /// <summary>
        /// The accent of scosu events (their cards and tiles have a bar of it on the left).
        /// </summary>
        public static readonly Color4 ICE = Color4Extensions.FromHex(@"7fb6e8");

        /// <summary>
        /// Secondary text.
        /// </summary>
        public static readonly Color4 TEXT_DIM = Color4.White.Opacity(0.69f);

        /// <summary>
        /// The primary button's fill (play), top to bottom.
        /// </summary>
        public static readonly ColourInfo PRIMARY = ColourInfo.GradientVertical(Color4Extensions.FromHex(@"8f70ff"), Color4Extensions.FromHex(@"6d4fe6"));

        public const float CORNER_RADIUS = 12;

        public const float CARD_PADDING = 16;

        /// <summary>
        /// Small capitals above a card's title ("TODAY", "SCORE GAINED").
        /// </summary>
        public static FontUsage Eyebrow => OsuFont.GetFont(size: 11, weight: FontWeight.SemiBold);

        /// <summary>
        /// Big numbers and titles, in the display font.
        /// </summary>
        public static FontUsage Display(float size) => OsuFont.GetFont(Typeface.Venera, size: size, weight: FontWeight.SemiBold);

        public static FontUsage Body(float size = 14, FontWeight weight = FontWeight.Regular) => OsuFont.GetFont(size: size, weight: weight);

        /// <summary>
        /// A card's background: <see cref="GLASS"/> with rounded corners and a thin outline. Put it first in a card's children.
        /// </summary>
        public static Drawable CreateGlassBackground() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = CORNER_RADIUS,
            CornerExponent = 2.5f,
            BorderThickness = 1.5f,
            BorderColour = OUTLINE,
            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = GLASS,
            },
        };
    }
}
