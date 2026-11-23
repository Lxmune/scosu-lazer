// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// scosu: what changed since the local user's previous game session, and their account standing (a scosu server endpoint).
    /// </summary>
    public class APIBriefing
    {
        [JsonProperty("mode")]
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// When the user was last seen in this mode; null the first time (nothing to compare with).
        /// </summary>
        [JsonProperty("since")]
        public DateTimeOffset? Since { get; set; }

        [JsonProperty("global_rank")]
        public RankChange? GlobalRank { get; set; }

        [JsonProperty("country_rank")]
        public RankChange? CountryRank { get; set; }

        [JsonProperty("ranked_score")]
        public ScoreChange? RankedScore { get; set; }

        /// <summary>
        /// The first places lost since then, newest first (a few of them, see <see cref="SnipeCount"/>).
        /// </summary>
        [JsonProperty("snipes")]
        public List<Snipe> Snipes { get; set; } = new List<Snipe>();

        [JsonProperty("snipe_count")]
        public int SnipeCount { get; set; }

        [JsonProperty("unread_messages")]
        public int UnreadMessages { get; set; }

        [JsonProperty("daily_tasks")]
        public TasksSummary? DailyTasks { get; set; }

        /// <summary>
        /// Set while the user is restricted or silenced.
        /// </summary>
        [JsonProperty("standing")]
        public StandingNotice? Standing { get; set; }

        public class RankChange
        {
            [JsonProperty("before")]
            public long? Before { get; set; }

            [JsonProperty("now")]
            public long? Now { get; set; }
        }

        public class ScoreChange
        {
            [JsonProperty("before")]
            public long Before { get; set; }

            [JsonProperty("now")]
            public long Now { get; set; }
        }

        public class Snipe
        {
            [JsonProperty("beatmap_id")]
            public long BeatmapId { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; } = string.Empty;

            [JsonProperty("by")]
            public PlayerRef? By { get; set; }

            [JsonProperty("at")]
            public DateTimeOffset At { get; set; }
        }

        public class PlayerRef
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; } = string.Empty;
        }

        public class TasksSummary
        {
            [JsonProperty("completed_count")]
            public int CompletedCount { get; set; }

            [JsonProperty("total")]
            public int Total { get; set; }

            [JsonProperty("day_goal")]
            public int DayGoal { get; set; }

            [JsonProperty("day_complete")]
            public bool DayComplete { get; set; }

            [JsonProperty("daily_streak")]
            public int DailyStreak { get; set; }
        }

        public class StandingNotice
        {
            [JsonProperty("restricted")]
            public bool Restricted { get; set; }

            /// <summary>
            /// Empty when no reason was given; null when not restricted.
            /// </summary>
            [JsonProperty("restriction_reason")]
            public string? RestrictionReason { get; set; }

            [JsonProperty("restricted_at")]
            public DateTimeOffset? RestrictedAt { get; set; }

            [JsonProperty("silenced_until")]
            public DateTimeOffset? SilencedUntil { get; set; }

            [JsonProperty("silence_reason")]
            public string? SilenceReason { get; set; }

            [JsonProperty("appeal_url")]
            public string AppealUrl { get; set; } = string.Empty;
        }
    }
}
