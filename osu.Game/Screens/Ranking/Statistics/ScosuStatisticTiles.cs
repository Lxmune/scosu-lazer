// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: a statistic card's headline figure: a small caption over a large value, and under it what the value is measured against.
    /// Tiles have one height, so a row of them lines up whatever they say.
    /// </summary>
    public partial class ScosuStatTile : CompositeDrawable, IHasTooltip
    {
        public const float HEIGHT = 64;

        public LocalisableString TooltipText { get; init; }

        /// <summary>
        /// Small and grey after the value: "/ 400x", "of 3".
        /// </summary>
        public LocalisableString ValueSuffix { get; init; }

        /// <summary>
        /// The places a rank went up, after the value.
        /// </summary>
        public long? PlacesGained { get; init; }

        /// <summary>
        /// The value's colour, white otherwise.
        /// </summary>
        public Color4? ValueColour { get; init; }

        /// <summary>
        /// An icon before the value (the crown of a first place).
        /// </summary>
        public IconUsage? Icon { get; init; }

        /// <summary>
        /// Whether the value is nothing worth noting (a zero): it is shown dimmed.
        /// </summary>
        public bool Muted { get; init; }

        private readonly LocalisableString caption;
        private readonly LocalisableString value;
        private readonly LocalisableString detail;

        public ScosuStatTile(LocalisableString caption, LocalisableString value, LocalisableString detail = default)
        {
            this.caption = caption;
            this.value = value;
            this.detail = detail;

            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            var valueFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5, 0),
                Alpha = Muted ? 0.4f : 1,
            };

            if (Icon is IconUsage icon)
            {
                valueFlow.Add(new SpriteIcon
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Size = new Vector2(15),
                    Margin = new MarginPadding { Bottom = 5 },
                    Icon = icon,
                    Colour = ValueColour ?? Color4.White,
                });
            }

            valueFlow.Add(new OsuSpriteText
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                Text = value,
                Font = OsuFont.Torus.With(size: 24, weight: FontWeight.SemiBold),
                Colour = ValueColour ?? Color4.White,
            });

            if (!LocalisableString.IsNullOrEmpty(ValueSuffix))
            {
                valueFlow.Add(new OsuSpriteText
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Margin = new MarginPadding { Bottom = 4 },
                    Text = ValueSuffix,
                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.SemiBold),
                    Colour = colours.Gray9,
                });
            }

            if (PlacesGained is long gained)
            {
                valueFlow.Add(new FillFlowContainer
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Margin = new MarginPadding { Bottom = 4 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(1, 0),
                    Colour = colours.Green1,
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Size = new Vector2(9),
                            Icon = FontAwesome.Solid.CaretUp,
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = gained.ToLocalisableString(@"N0"),
                            Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                        },
                    }
                });
            }

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0.05f,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Horizontal = 12, Vertical = 9 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = caption.ToUpper(),
                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Bold),
                                Spacing = new Vector2(0.6f, 0),
                                Colour = colours.Gray9,
                            },
                            valueFlow,
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = detail,
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                Colour = colours.GrayA,
                            },
                        }
                    },
                }
            };
        }
    }

    /// <summary>
    /// scosu: <see cref="ScosuStatTile"/>s in equal columns, wrapping onto new rows. A last row with fewer tiles keeps the columns' width.
    /// </summary>
    public partial class ScosuTileGrid : FillFlowContainer
    {
        private const float gap = 8;

        private readonly int columns;

        public ScosuTileGrid(int columns)
        {
            this.columns = columns;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Full;
            Spacing = new Vector2(0, gap);
        }

        /// <summary>
        /// Adds <paramref name="tile"/> in the next column.
        /// </summary>
        public void AddTile(Drawable tile)
        {
            int column = Count % columns;

            // Each column gives up the same share of the gaps, so all are as wide.
            Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Width = 1f / columns,
                Padding = new MarginPadding
                {
                    Left = gap * column / columns,
                    Right = gap * (columns - 1 - column) / columns,
                },
                Child = tile,
            });
        }
    }

    /// <summary>
    /// scosu: a rounded bar split into parts, each in its colour with a sliver of gap between them, filling in one after another.
    /// </summary>
    public partial class ScosuSegmentedBar : CompositeDrawable
    {
        private const float height = 8;
        private const float gap = 2;

        private readonly FillFlowContainer segments;

        public ScosuSegmentedBar()
        {
            RelativeSizeAxes = Axes.X;
            Height = height;

            InternalChildren = new Drawable[]
            {
                new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0.06f,
                },
                segments = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                },
            };
        }

        /// <summary>
        /// Shows <paramref name="parts"/>: each a fraction of the whole bar, from the left.
        /// </summary>
        public void SetParts(IEnumerable<(double Fraction, Color4 Colour)> parts)
        {
            segments.Clear();

            var shown = parts.Where(p => p.Fraction > 0).ToList();
            double delay = 0;

            for (int i = 0; i < shown.Count; i++)
            {
                var segment = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0,
                    Padding = new MarginPadding { Right = i < shown.Count - 1 ? gap : 0 },
                    Child = new Circle
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = shown[i].Colour,
                    },
                };

                segments.Add(segment);

                using (BeginDelayedSequence(delay))
                    segment.ResizeWidthTo((float)Math.Clamp(shown[i].Fraction, 0, 1), 500, Easing.OutQuint);

                delay += 120;
            }
        }
    }
}
