// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Game.Scoring;

namespace osu.Game.Online.Rooms
{
    public class APIScoreToken
    {
        [JsonProperty("id")]
        public long ID { get; set; }

        /// <summary>
        /// Sent by servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>),
        /// to show the play's stable score while it happens.
        /// </summary>
        [JsonProperty("stable_score_attributes")]
        public StableScoreAttributes? StableScoreAttributes { get; set; }
    }
}
