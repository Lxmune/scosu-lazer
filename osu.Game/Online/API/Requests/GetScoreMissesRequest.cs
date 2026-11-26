// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets where an osu! play missed and why, read from its replay (a scosu server endpoint).
    /// </summary>
    public class GetScoreMissesRequest : ScosuWebAPIRequest<APIMissAnalysis>
    {
        private readonly long scoreId;

        public GetScoreMissesRequest(long scoreId)
        {
            this.scoreId = scoreId;
        }

        protected override string Target => $@"scores/{scoreId}/misses";
    }
}
