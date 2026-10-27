// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetMenuContentRequest : OsuJsonWebRequest<APIMenuContent>
    {
        /// <param name="apiUrl">The server's <see cref="EndpointConfiguration.APIUrl"/>, whose banners are shown.</param>
        public GetMenuContentRequest(string apiUrl)
            : base($@"{apiUrl}/api/v2/menu-content")
        {
        }
    }
}
