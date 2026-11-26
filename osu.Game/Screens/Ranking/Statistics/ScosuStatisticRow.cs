// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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
    /// scosu: a row of a statistic card, like <see cref="SimpleStatisticItem"/>: its name on the left, its value on the right,
    /// with the places a rank went up and the value's own colour when it has one.
    /// </summary>
    public partial class ScosuStatisticRow : CompositeDrawable, IHasTooltip
    {
        public LocalisableString TooltipText { get; init; }

        /// <summary>
        /// The places a rank went up, shown before the value.
        /// </summary>
        public long? PlacesGained { get; init; }

        /// <summary>
        /// The value's colour, white otherwise.
        /// </summary>
        public Color4? ValueColour { get; init; }

        /// <summary>
        /// A dot before the name, in the colour of what the row stands for elsewhere in the card (a bar's part).
        /// </summary>
        public Color4? DotColour { get; init; }

        /// <summary>
        /// Small and grey after the value: a share, an aside.
        /// </summary>
        public LocalisableString Detail { get; init; }

        /// <summary>
        /// Fades a card's rows in one after the other, top to bottom, as they arrive.
        /// </summary>
        public static void Cascade(IEnumerable<Drawable> rows)
        {
            double delay = 0;

            foreach (var row in rows)
            {
                row.FadeOut().Delay(delay).FadeIn(300, Easing.OutQuint);
                delay += 60;
            }
        }

        private readonly LocalisableString name;
        private readonly LocalisableString value;

        public ScosuStatisticRow(LocalisableString name, LocalisableString value)
        {
            this.name = name;
            this.value = value;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            var valueFlow = new FillFlowContainer
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6, 0),
            };

            if (PlacesGained is long gained)
            {
                valueFlow.Add(new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
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
                            Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 1, weight: FontWeight.Bold),
                        },
                    }
                });
            }

            valueFlow.Add(new TruncatingSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Text = value,
                Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.Bold),
                Colour = ValueColour ?? Color4.White,
                MaxWidth = 260,
            });

            if (!LocalisableString.IsNullOrEmpty(Detail))
            {
                valueFlow.Add(new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = Detail,
                    Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 1, weight: FontWeight.SemiBold),
                    Colour = colours.Gray9,
                    // A column of its own, so the values line up.
                    Width = 34,
                });
            }

            var nameFlow = new FillFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(8, 0),
            };

            if (DotColour is Color4 dot)
            {
                nameFlow.Add(new Circle
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(8),
                    Colour = dot,
                });
            }

            nameFlow.Add(new OsuSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Text = name,
                Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE),
            });

            InternalChildren = new Drawable[]
            {
                nameFlow,
                valueFlow,
            };
        }
    }
}
