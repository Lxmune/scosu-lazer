// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// scosu: the local user's daily tasks today, as the scosu server keeps them (one board per UTC day, for the mode they play most).
    /// </summary>
    public class APIDailyTasks
    {
        /// <summary>
        /// The UTC day of the board (<c>yyyy-MM-dd</c>).
        /// </summary>
        [JsonProperty("day")]
        public string Day { get; set; } = string.Empty;

        /// <summary>
        /// The leaderboard whose plays count (<c>osu</c>, <c>osurx</c>, ...; see <see cref="osu.Game.Rulesets.RelaxLeaderboards"/>).
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// When the next board starts.
        /// </summary>
        [JsonProperty("resets_at")]
        public DateTimeOffset ResetsAt { get; set; }

        [JsonProperty("tasks")]
        public List<DailyTask> Tasks { get; set; } = new List<DailyTask>();

        [JsonProperty("completed_count")]
        public int CompletedCount { get; set; }

        /// <summary>
        /// How many tasks make the day count toward the streaks.
        /// </summary>
        [JsonProperty("day_goal")]
        public int DayGoal { get; set; }

        [JsonProperty("day_complete")]
        public bool DayComplete { get; set; }

        [JsonProperty("reroll_available")]
        public bool RerollAvailable { get; set; }

        [JsonProperty("streaks")]
        public TaskStreaks Streaks { get; set; } = new TaskStreaks();

        public class DailyTask
        {
            [JsonProperty("slot")]
            public int Slot { get; set; }

            /// <summary>
            /// <c>gain_ranked_score</c>, <c>improve_best</c>, <c>fail_then_pass</c>, <c>return_to_map</c>, <c>one_miss</c>,
            /// <c>daily_challenge_top</c>, <c>take_first</c>, <c>rising_stars</c> or <c>full_combo_above</c>.
            /// </summary>
            [JsonProperty("kind")]
            public string Kind { get; set; } = string.Empty;

            /// <summary>
            /// A percentage (<c>improve_best</c>, <c>daily_challenge_top</c>), days (<c>return_to_map</c>) or stars in hundredths
            /// (<c>full_combo_above</c>); 0 for the others.
            /// </summary>
            [JsonProperty("param")]
            public long Param { get; set; }

            /// <summary>
            /// Ranked score for <c>gain_ranked_score</c>, maps in a row for <c>rising_stars</c>, 1 for the others.
            /// </summary>
            [JsonProperty("target")]
            public long Target { get; set; }

            [JsonProperty("progress")]
            public long Progress { get; set; }

            [JsonProperty("completed")]
            public bool Completed { get; set; }
        }

        public class TaskStreaks
        {
            [JsonProperty("daily")]
            public int Daily { get; set; }

            [JsonProperty("daily_best")]
            public int DailyBest { get; set; }

            [JsonProperty("weekly")]
            public int Weekly { get; set; }

            [JsonProperty("weekly_best")]
            public int WeeklyBest { get; set; }

            [JsonProperty("week_days")]
            public int WeekDays { get; set; }

            [JsonProperty("week_goal")]
            public int WeekGoal { get; set; }
        }
    }
}
