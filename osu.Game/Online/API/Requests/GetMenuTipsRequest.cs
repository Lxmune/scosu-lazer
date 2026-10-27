// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetMenuTipsRequest : OsuJsonWebRequest<APIMenuTips>
    {
        /// <param name="apiUrl">The server's <see cref="EndpointConfiguration.APIUrl"/>, whose tips are shown.</param>
        public GetMenuTipsRequest(string apiUrl)
            : base($@"{apiUrl}/api/v2/menu-tips")
        {
        }
    }
}
