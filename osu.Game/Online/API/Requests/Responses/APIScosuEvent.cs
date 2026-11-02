// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A scosu! event: a limited time ranking by ranked score gained, with objectives and profile titles to earn
    /// (a scosu server feature, see <see cref="GetScosuEventRequest"/>).
    /// </summary>
    public class APIScosuEvent
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("tagline")]
        public string? Tagline { get; set; }

        /// <summary>
        /// Markdown; empty without one.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScosuEventStatus Status { get; set; }

        [JsonProperty("starts_at")]
        public DateTimeOffset StartsAt { get; set; }

        [JsonProperty("ends_at")]
        public DateTimeOffset EndsAt { get; set; }

        [JsonProperty("look")]
        public Look Appearance { get; set; } = new Look();

        /// <summary>
        /// Which plays count, one short sentence each ("osu! only", "4★ and more").
        /// </summary>
        [JsonProperty("rules")]
        public List<string> Rules { get; set; } = new List<string>();

        /// <summary>
        /// Players in the whole ranking (<see cref="Ranking"/> is only its top).
        /// </summary>
        [JsonProperty("player_count")]
        public int PlayerCount { get; set; }

        [JsonProperty("ranking")]
        public List<Rank> Ranking { get; set; } = new List<Rank>();

        [JsonProperty("objectives")]
        public List<Objective> Objectives { get; set; } = new List<Objective>();

        [JsonProperty("rewards")]
        public List<Reward> Rewards { get; set; } = new List<Reward>();

        /// <summary>
        /// The local user's standing.
        /// </summary>
        [JsonProperty("me")]
        public Standing? Me { get; set; }

        public class Look
        {
            /// <summary>
            /// "#rrggbb".
            /// </summary>
            [JsonProperty("accent")]
            public string Accent { get; set; } = @"#7856ce";

            [JsonProperty("banner_url")]
            public string? BannerUrl { get; set; }

            /// <summary>
            /// How much darker the banner is drawn, 0 to 1.
            /// </summary>
            [JsonProperty("banner_dim")]
            public float BannerDim { get; set; }

            /// <summary>
            /// Whether the main menu shows the event's card.
            /// </summary>
            [JsonProperty("menu_card")]
            public bool MenuCard { get; set; }

            [JsonProperty("menu_position")]
            [JsonConverter(typeof(StringEnumConverter))]
            public ScosuEventMenuPosition MenuPosition { get; set; }

            [JsonProperty("menu_image_url")]
            public string? MenuImageUrl { get; set; }

            [JsonProperty("menu_dim")]
            public float MenuDim { get; set; }
        }

        public class Rank
        {
            [JsonProperty("rank")]
            public int Position { get; set; }

            [JsonProperty("user")]
            public APIUser User { get; set; } = new APIUser();

            [JsonProperty("ranked_score")]
            public long RankedScore { get; set; }

            [JsonProperty("play_count")]
            public int PlayCount { get; set; }
        }

        public class Objective
        {
            /// <summary>
            /// "ranked_score_gained", "plays", "playtime", "full_combos", ...
            /// </summary>
            [JsonProperty("kind")]
            public string Kind { get; set; } = string.Empty;

            [JsonProperty("label")]
            public string Label { get; set; } = string.Empty;

            /// <summary>
            /// Score, a count, or seconds for "playtime".
            /// </summary>
            [JsonProperty("target")]
            public long Target { get; set; }

            /// <summary>
            /// <see cref="Target"/> as shown: "5m", "10", "6h".
            /// </summary>
            [JsonProperty("target_text")]
            public string TargetText { get; set; } = string.Empty;
        }

        public class Reward
        {
            [JsonProperty("title")]
            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// "#rrggbb"; null: the event's accent.
            /// </summary>
            [JsonProperty("colour")]
            public string? Colour { get; set; }

            /// <summary>
            /// "Complete every objective", "Finish in the top 3".
            /// </summary>
            [JsonProperty("condition")]
            public string Condition { get; set; } = string.Empty;
        }

        public class Standing
        {
            /// <summary>
            /// Null until the user is ranked.
            /// </summary>
            [JsonProperty("rank")]
            public int? Rank { get; set; }

            [JsonProperty("ranked_score")]
            public long RankedScore { get; set; }

            [JsonProperty("play_count")]
            public int PlayCount { get; set; }

            /// <summary>
            /// The player to pass next; null at #1 or unranked.
            /// </summary>
            [JsonProperty("next")]
            public APIScoreSummary.NextRank? Next { get; set; }

            /// <summary>
            /// In the order of <see cref="Objectives"/>.
            /// </summary>
            [JsonProperty("objectives")]
            public List<Progress> Objectives { get; set; } = new List<Progress>();

            [JsonProperty("completed")]
            public int Completed { get; set; }

            /// <summary>
            /// In the order of <see cref="Rewards"/>.
            /// </summary>
            [JsonProperty("rewards")]
            public List<RewardStatus> Rewards { get; set; } = new List<RewardStatus>();
        }

        public class Progress
        {
            [JsonProperty("progress")]
            public long Value { get; set; }

            /// <summary>
            /// <see cref="Value"/> as shown: "4h 12m".
            /// </summary>
            [JsonProperty("progress_text")]
            public string Text { get; set; } = string.Empty;

            [JsonProperty("done")]
            public bool Done { get; set; }
        }

        public class RewardStatus
        {
            /// <summary>
            /// The user meets the condition now (while the event runs) or did at the end.
            /// </summary>
            [JsonProperty("met")]
            public bool Met { get; set; }

            /// <summary>
            /// The title was given (the event is over).
            /// </summary>
            [JsonProperty("given")]
            public bool Given { get; set; }

            /// <summary>
            /// "2 / 4", "you: #4", "not ranked yet".
            /// </summary>
            [JsonProperty("status_text")]
            public string StatusText { get; set; } = string.Empty;
        }
    }

    public enum ScosuEventStatus
    {
        [EnumMember(Value = @"upcoming")]
        Upcoming,

        [EnumMember(Value = @"live")]
        Live,

        [EnumMember(Value = @"ended")]
        Ended,
    }

    public enum ScosuEventMenuPosition
    {
        [EnumMember(Value = @"top-left")]
        TopLeft,

        [EnumMember(Value = @"top-right")]
        TopRight,
    }
}
