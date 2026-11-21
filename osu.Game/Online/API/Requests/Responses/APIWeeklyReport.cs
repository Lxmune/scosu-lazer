// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// An entry of the user's weekly reports list: the week and its highlights.
    /// </summary>
    public class APIWeeklyReport
    {
        /// <summary>
        /// The Monday (<c>yyyy-MM-dd</c>) the week started on.
        /// </summary>
        [JsonProperty("week_start")]
        public string WeekStart { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("highlights")]
        public List<Highlight> Highlights { get; set; } = new List<Highlight>();

        public class Highlight
        {
            /// <summary>
            /// <c>ranked_score</c>, <c>accuracy</c>, <c>pass_rate</c>, <c>stars</c>, <c>ur</c>, <c>ur_spread</c>, <c>offset</c>, <c>stream_rush</c> or <c>spinner_rpm</c>.
            /// </summary>
            [JsonProperty("measure")]
            public string Measure { get; set; } = string.Empty;

            [JsonProperty("mode")]
            public string? Mode { get; set; }

            /// <summary>
            /// <c>improved</c> or <c>worse</c>.
            /// </summary>
            [JsonProperty("change")]
            public string Change { get; set; } = string.Empty;

            /// <summary>
            /// The week's value.
            /// </summary>
            [JsonProperty("value")]
            public double Value { get; set; }

            /// <summary>
            /// The average of the weeks before.
            /// </summary>
            [JsonProperty("before")]
            public double Before { get; set; }
        }
    }
}
