// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The latest news post. Opens it in the news overlay.
    /// </summary>
    public partial class HubNewsTile : HubTile
    {
        /// <summary>
        /// The news doesn't change often, so opening the hub again soon doesn't ask again.
        /// </summary>
        private static readonly TimeSpan refresh_interval = TimeSpan.FromMinutes(5);

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private NewsOverlay? news { get; set; }

        private GetNewsRequest? request;
        private DateTimeOffset? lastFetch;
        private bool hasPost;

        public HubNewsTile()
        {
            Eyebrow.Text = MenuHubStrings.News.ToUpper();
            ShowPlaceholder(MenuHubStrings.NoNews);
        }

        /// <summary>
        /// Fetches the latest post, unless that was done recently.
        /// </summary>
        public void RefreshIfStale()
        {
            if (request != null || api.State.Value != APIState.Online)
                return;

            if (lastFetch != null && DateTimeOffset.Now - lastFetch < refresh_interval && hasPost)
                return;

            var req = request = new GetNewsRequest();

            req.Success += response => Schedule(() =>
            {
                if (req != request)
                    return;

                request = null;
                lastFetch = DateTimeOffset.Now;
                display(response.NewsPosts?.OrderByDescending(p => p.PublishedAt).FirstOrDefault());
            });
            req.Failure += _ => Schedule(() =>
            {
                if (req == request)
                    request = null;
            });

            api.Queue(req);
        }

        private void display(APINewsPost? post)
        {
            hasPost = post != null;

            if (post == null)
            {
                ShowPlaceholder(MenuHubStrings.NoNews);
                return;
            }

            Body.Clear();
            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = post.Title,
                Font = ScosuMenuStyle.Display(16),
            });
            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = post.PublishedAt.ToLocalTime().ToString(@"d MMM yyyy"),
                Font = ScosuMenuStyle.Body(12),
                Colour = ScosuMenuStyle.TEXT_DIM,
            });

            SetFoot(FontAwesome.Solid.Newspaper, MenuHubStrings.Read);
            SetAction(() => news?.ShowArticle(post.Slug));
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
