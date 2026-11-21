// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// scosu: one news post, with its Markdown, for the news overlay to show in the game (osu! opens posts in the browser).
    /// </summary>
    public class GetNewsPostRequest : APIRequest<GetNewsPostResponse>
    {
        private readonly string slug;

        public GetNewsPostRequest(string slug)
        {
            this.slug = slug;
        }

        protected override string Target => $@"news/{slug}";
    }
}
