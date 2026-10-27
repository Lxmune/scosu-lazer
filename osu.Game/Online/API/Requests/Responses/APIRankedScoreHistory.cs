// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// The ranked score of a player at the end of each of the last 90 days (UTC), the oldest first and their ranked score now last.
    /// Sent with profiles by servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public class APIRankedScoreHistory
    {
        [JsonProperty(@"mode")]
        public string Mode = string.Empty;

        [JsonProperty(@"data")]
        public long[] Data = [];
    }
}
