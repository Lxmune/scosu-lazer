// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// scosu: a news post with its Markdown and the news sidebar of the post's year.
    /// </summary>
    public class GetNewsPostResponse : APINewsPost
    {
        [JsonProperty("markdown")]
        public string Markdown { get; set; } = string.Empty;

        [JsonProperty("news_sidebar")]
        public APINewsSidebar? SidebarMetadata { get; set; }
    }
}
