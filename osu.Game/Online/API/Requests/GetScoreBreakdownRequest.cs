// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets how much of the beatmap's best osu!stable score a score earned and what it lost to combo breaks and judgements (a scosu server endpoint).
    /// The response is <see langword="null"/> (JSON <c>null</c>) when the server can't work the beatmap's maximum out, so handlers must check for it.
    /// </summary>
    public class GetScoreBreakdownRequest : ScosuWebAPIRequest<APIScoreBreakdown>
    {
        private readonly long scoreId;

        public GetScoreBreakdownRequest(long scoreId)
        {
            this.scoreId = scoreId;
        }

        protected override string Target => $@"scores/{scoreId}/breakdown";
    }
}
