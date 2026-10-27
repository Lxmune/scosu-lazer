// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// The ranked score the local user gained today and this week, as the scosu server counts it.
    /// </summary>
    public class APIScoreSummary
    {
        [JsonProperty("today")]
        public Period Today { get; set; } = new Period();

        [JsonProperty("week")]
        public Period Week { get; set; } = new Period();

        /// <summary>
        /// The user's current global rank, if they have one.
        /// </summary>
        [JsonProperty("rank")]
        public int? Rank { get; set; }

        /// <summary>
        /// The player one place above the user, if any.
        /// </summary>
        [JsonProperty("next")]
        public NextRank? Next { get; set; }

        public class Period
        {
            /// <summary>
            /// How much the user's ranked score grew over the period.
            /// </summary>
            [JsonProperty("ranked_score")]
            public long RankedScore { get; set; }

            [JsonProperty("play_count")]
            public int PlayCount { get; set; }

            /// <summary>
            /// The user's global rank when the period started, if they had one.
            /// </summary>
            [JsonProperty("rank_start")]
            public int? RankStart { get; set; }

            /// <summary>
            /// The ranked score gained so far after each play that raised it, in order.
            /// </summary>
            [JsonProperty("points")]
            public List<Point> Points { get; set; } = new List<Point>();
        }

        public class Point
        {
            [JsonProperty("at")]
            public DateTimeOffset At { get; set; }

            [JsonProperty("ranked_score")]
            public long RankedScore { get; set; }
        }

        public class NextRank
        {
            [JsonProperty("rank")]
            public int Rank { get; set; }

            [JsonProperty("user_id")]
            public int UserId { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; } = string.Empty;

            /// <summary>
            /// The ranked score the user needs to pass this player.
            /// </summary>
            [JsonProperty("ranked_score_needed")]
            public long RankedScoreNeeded { get; set; }
        }
    }
}
