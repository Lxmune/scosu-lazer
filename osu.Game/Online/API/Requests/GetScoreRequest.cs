// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets one score. On scosu servers, it comes with the order of its judgements when the play was set with this client
    /// (<see cref="SoloScoreInfo.JudgementOrder"/>), which score lists leave out.
    /// </summary>
    public class GetScoreRequest : APIRequest<SoloScoreInfo>
    {
        private readonly long scoreId;

        public GetScoreRequest(long scoreId)
        {
            this.scoreId = scoreId;
        }

        protected override string Target => $@"scores/{scoreId}";
    }
}
