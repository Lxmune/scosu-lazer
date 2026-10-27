// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Profile.Header.Components
{
    /// <summary>
    /// A profile graph in its own panel, under a title and a summary of the change it shows.
    /// </summary>
    public partial class ProfileGraphPanel : CompositeDrawable
    {
        private readonly LocalisableString title;
        private readonly Drawable graph;

        private SpriteIcon summaryIcon = null!;
        private OsuSpriteText summaryText = null!;
        private FillFlowContainer summary = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public ProfileGraphPanel(LocalisableString title, Drawable graph)
        {
            this.title = title;
            this.graph = graph;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Masking = true;
            CornerRadius = 10;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 12, Vertical = 10 },
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6, 0),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = title,
                                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                    Colour = colourProvider.Content2,
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Text = @"90 days",
                                    Font = OsuFont.GetFont(size: 11),
                                    Colour = colourProvider.Foreground1,
                                },
                            }
                        },
                        summary = new FillFlowContainer
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(3, 0),
                            Children = new Drawable[]
                            {
                                summaryIcon = new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(10),
                                },
                                summaryText = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                },
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 24 },
                            Children = new[]
                            {
                                new Box
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 1,
                                    Colour = colourProvider.Background3,
                                },
                                graph,
                            }
                        },
                    }
                },
            };
        }

        /// <summary>
        /// Shows <paramref name="text"/> in the top right corner, after <paramref name="icon"/> if there is one.
        /// </summary>
        public void SetSummary(LocalisableString text, Color4 colour, IconUsage? icon = null)
        {
            summary.Colour = colour;
            summaryText.Text = text;
            summaryIcon.Icon = icon ?? default;
            summaryIcon.Alpha = icon == null ? 0 : 1;
        }
    }
}
