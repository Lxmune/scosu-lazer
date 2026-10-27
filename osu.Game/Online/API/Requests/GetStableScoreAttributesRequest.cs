// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.IO.Network;
using osu.Game.Scoring;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// The <see cref="StableScoreAttributes"/> a score token carries, for plays that have none (replays),
    /// on servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public class GetStableScoreAttributesRequest : APIRequest<StableScoreAttributes>
    {
        private readonly int beatmapId;
        private readonly int rulesetId;

        public GetStableScoreAttributesRequest(int beatmapId, int rulesetId)
        {
            this.beatmapId = beatmapId;
            this.rulesetId = rulesetId;
        }

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.AddParameter(@"ruleset_id", rulesetId.ToString(CultureInfo.InvariantCulture));
            return request;
        }

        protected override string Target => $@"beatmaps/{beatmapId}/stable-score-attributes";
    }
}
