// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Wiki.Markdown;
using osuTK;

namespace osu.Game.Overlays.News.Displays
{
    /// <summary>
    /// scosu: a news post, in the game: its picture, title, author and date, then the post, rendered like a wiki page.
    /// </summary>
    public partial class ArticleDisplay : CompositeDrawable
    {
        private readonly GetNewsPostResponse post;

        public ArticleDisplay(GetNewsPostResponse post)
        {
            this.post = post;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, IAPIProvider api)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Padding = new MarginPadding
            {
                Vertical = 20,
                Left = 30,
                Right = WaveOverlayContainer.HORIZONTAL_PADDING,
            };

            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 12),
            };

            if (!string.IsNullOrEmpty(post.FirstImage))
            {
                flow.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 240,
                    Masking = true,
                    CornerRadius = 8,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background4,
                        },
                        new NewsPostBackground(post.FirstImage)
                        {
                            RelativeSizeAxes = Axes.Both,
                            FillMode = FillMode.Fill,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    },
                });
            }

            flow.Add(new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 32, weight: FontWeight.SemiBold))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Margin = new MarginPadding { Top = 6 },
                Text = post.Title,
            });

            flow.Add(new OsuTextFlowContainer(t =>
            {
                t.Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold);
                t.Colour = colourProvider.Content2;
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = LocalisableString.Interpolate($@"{post.Author} · {post.PublishedAt.ToLocalisableString(@"d MMMM yyyy")}"),
            });

            flow.Add(new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 1,
                Colour = colourProvider.Background3,
                Margin = new MarginPadding { Vertical = 4 },
            });

            flow.Add(new WikiMarkdownContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                // The post's pictures sit next to it, under /news on the server's website.
                CurrentPath = $@"{api.Endpoints.WebsiteUrl}/news/",
                Text = post.Markdown,
                DocumentMargin = new MarginPadding(0),
                DocumentPadding = new MarginPadding(0),
            });

            InternalChild = flow;
        }
    }
}
