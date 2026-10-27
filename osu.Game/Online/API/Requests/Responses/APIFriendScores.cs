// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// The scores of the local user's friends, shown on the dashboard's friend cards (see <see cref="GetFriendScoresRequest"/>).
    /// </summary>
    public class APIFriendScores
    {
        [JsonProperty("friends")]
        public List<Entry> Friends { get; set; } = new List<Entry>();

        public class Entry
        {
            [JsonProperty("user_id")]
            public int UserId { get; set; }

            /// <summary>
            /// <c>null</c> if unranked.
            /// </summary>
            [JsonProperty("global_rank")]
            public int? GlobalRank { get; set; }

            [JsonProperty("ranked_score")]
            public long RankedScore { get; set; }

            /// <summary>
            /// The ranked score gained since the local user's midnight.
            /// </summary>
            [JsonProperty("today_ranked_score")]
            public long TodayRankedScore { get; set; }
        }
    }
}
