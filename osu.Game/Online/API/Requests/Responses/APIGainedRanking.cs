// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A player's place in <see cref="GetGainedRankingsRequest"/>: the ranked score they gained in the period.
    /// </summary>
    public class APIGainedRanking
    {
        [JsonProperty("user")]
        public APIUser User = null!;

        [JsonProperty("ranked_score")]
        public long RankedScore;

        [JsonProperty("play_count")]
        public int PlayCount;
    }
}
