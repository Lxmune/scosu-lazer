// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets how hard a score's beatmap is over its length with the score's mods (a scosu server endpoint).
    /// </summary>
    public class GetScoreStrainsRequest : APIRequest<APIScoreStrains>
    {
        private readonly long scoreId;

        public GetScoreStrainsRequest(long scoreId)
        {
            this.scoreId = scoreId;
        }

        protected override string Target => $@"scores/{scoreId}/strains";
    }
}
