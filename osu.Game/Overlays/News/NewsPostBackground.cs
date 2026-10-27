// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Online.API;

namespace osu.Game.Overlays.News
{
    [LongRunningLoad]
    public partial class NewsPostBackground : Sprite
    {
        private readonly string sourceUrl;

        public NewsPostBackground(string sourceUrl)
        {
            this.sourceUrl = sourceUrl;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore store, IAPIProvider api)
        {
            Texture = store.Get(createUrl(sourceUrl, api.Endpoints.WebsiteUrl));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(500, Easing.OutQuint);
        }

        private static string createUrl(string source, string websiteUrl)
        {
            if (string.IsNullOrEmpty(source))
                return "Headers/news";

            // Relative to the website of the server the post came from.
            if (source.StartsWith('/'))
                return websiteUrl + source;

            return source;
        }
    }
}
