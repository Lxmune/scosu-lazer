// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
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
    public static partial class ScosuMenuStyle
    {
        /// <summary>
        /// Default accent hue (scosu violet, matching the website's --h = 256).
        /// </summary>
        public const int DEFAULT_HUE = 256;

        /// <summary>
        /// Live accent hue in degrees (0..360), bound to <see cref="Configuration.OsuSetting.AccentHue"/>.
        /// </summary>
        public static readonly BindableInt AccentHue = new BindableInt(DEFAULT_HUE)
        {
            MinValue = 0,
            MaxValue = 360,
            Default = DEFAULT_HUE,
        };

        private static readonly Color4 default_glass = Color4Extensions.FromHex(@"17131f").Opacity(0.72f);
        private static readonly Color4 default_band = Color4Extensions.FromHex(@"120f19").Opacity(0.82f);
        private static readonly Color4 default_violet = Color4Extensions.FromHex(@"a68bff");
        private static readonly Color4 default_violet_deep = Color4Extensions.FromHex(@"7556ec");
        private static readonly Color4 default_violet_ink = Color4Extensions.FromHex(@"d9ceff");
        private static readonly ColourInfo default_primary = ColourInfo.GradientVertical(Color4Extensions.FromHex(@"8f70ff"), Color4Extensions.FromHex(@"6d4fe6"));

        public static Color4 HueColour(int hue, float saturation, float lightness)
        {
            int wrapped = ((hue % 360) + 360) % 360;
            return Colour4.FromHSL(wrapped / 360f, saturation, lightness);
        }

        private static bool isDefaultHue => ((AccentHue.Value % 360) + 360) % 360 == DEFAULT_HUE;

        /// <summary>
        /// The background of cards and tiles: dark, subtly tinted by the accent hue, see-through.
        /// </summary>
        public static Color4 GLASS => isDefaultHue ? default_glass : HueColour(AccentHue.Value, 0.24f, 0.10f).Opacity(0.72f);

        /// <summary>
        /// A hub tile's base: a neutral dark grey (the accent is left to highlights), nearly opaque, since the tiles sit
        /// straight on the menu's background.
        /// </summary>
        public static readonly Color4 TILE = Color4Extensions.FromHex(@"0f0f11").Opacity(0.95f);

        /// <summary>
        /// The band's and the hub bar's background, a little denser than <see cref="GLASS"/>.
        /// </summary>
        public static Color4 BAND => isDefaultHue ? default_band : HueColour(AccentHue.Value, 0.25f, 0.08f).Opacity(0.82f);

        /// <summary>
        /// The thin outline of cards and tiles.
        /// </summary>
        public static readonly Color4 OUTLINE = Color4.White.Opacity(0.11f);

        /// <summary>
        /// A light fill inside a card (counters, chips, hovered buttons).
        /// </summary>
        public static readonly Color4 FILL = Color4.White.Opacity(0.07f);

        public static Color4 VIOLET => isDefaultHue ? default_violet : HueColour(AccentHue.Value, 1.0f, 0.77f);
        public static Color4 VIOLET_DEEP => isDefaultHue ? default_violet_deep : HueColour(AccentHue.Value, 0.80f, 0.63f);
        public static Color4 VIOLET_INK => isDefaultHue ? default_violet_ink : HueColour(AccentHue.Value, 1.0f, 0.90f);

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
        public static ColourInfo PRIMARY => isDefaultHue
            ? default_primary
            : ColourInfo.GradientVertical(HueColour(AccentHue.Value, 0.95f, 0.70f), HueColour(AccentHue.Value, 0.75f, 0.58f));

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
        public static Drawable CreateGlassBackground() => new GlassBackground();

        private partial class GlassBackground : Container
        {
            private readonly IBindable<int> accentHue = AccentHue.GetBoundCopy();
            private readonly Box box;

            public GlassBackground()
            {
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = CORNER_RADIUS;
                CornerExponent = 2.5f;
                BorderThickness = 1.5f;
                BorderColour = OUTLINE;
                Child = box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = GLASS,
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                accentHue.BindValueChanged(_ => box.Colour = GLASS, true);
            }
        }
    }
}
