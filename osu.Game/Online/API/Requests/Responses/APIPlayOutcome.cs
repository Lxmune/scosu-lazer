// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// What a submitted play did, as scosu's server tells it in the answer to the submission (<c>scosu_outcome</c>):
    /// whether it counted and why not, the ranked score it added, and the places it reached on the beatmap and in the ranking.
    /// </summary>
    public class APIPlayOutcome
    {
        /// <summary>
        /// Whether the play counts for leaderboards (and, on beatmaps that give it, ranked score).
        /// </summary>
        [JsonProperty("counted")]
        public bool Counted { get; set; }

        /// <summary>
        /// Why the play doesn't count, in the order a player would fix them. Empty when it counts.
        /// </summary>
        [JsonProperty("not_counted")]
        public NotCountedReason[] NotCounted { get; set; } = Array.Empty<NotCountedReason>();

        /// <summary>
        /// The leaderboard the play went to: its ruleset's online ID, or one of scosu's Relax and Autopilot modes (4 and above).
        /// </summary>
        [JsonProperty("mode")]
        public int Mode { get; set; }

        /// <summary>
        /// Whether the beatmap's best scores add up to ranked score. Qualified beatmaps have a leaderboard but don't, until they are ranked.
        /// </summary>
        [JsonProperty("gives_ranked_score")]
        public bool GivesRankedScore { get; set; }

        [JsonProperty("ranked_score_gain")]
        public long RankedScoreGain { get; set; }

        /// <summary>
        /// Whether the play is now the player's best on the beatmap.
        /// </summary>
        [JsonProperty("new_best")]
        public bool NewBest { get; set; }

        [JsonProperty("map_rank_before")]
        public long? MapRankBefore { get; set; }

        [JsonProperty("map_rank")]
        public long? MapRank { get; set; }

        /// <summary>
        /// The player who held first place on the beatmap until this play.
        /// </summary>
        [JsonProperty("took_first_from")]
        public Player? TookFirstFrom { get; set; }

        /// <summary>
        /// The player right above on the beatmap, and the score a new best needs to pass them.
        /// </summary>
        [JsonProperty("next_on_map")]
        public Target? NextOnMap { get; set; }

        [JsonProperty("global_rank_before")]
        public long? GlobalRankBefore { get; set; }

        [JsonProperty("global_rank")]
        public long? GlobalRank { get; set; }

        /// <summary>
        /// The player right above in the ranking by ranked score, and the ranked score needed to pass them.
        /// </summary>
        [JsonProperty("next_in_ranking")]
        public Target? NextInRanking { get; set; }

        public class NotCountedReason
        {
            /// <summary>
            /// <c>failed</c>, <c>no_leaderboard</c>, <c>unranked_mod</c> or <c>mod_settings</c>.
            /// </summary>
            [JsonProperty("code")]
            public string Code { get; set; } = string.Empty;

            /// <summary>
            /// For <c>no_leaderboard</c>: the beatmap's status ("pending", "graveyard", ...).
            /// </summary>
            [JsonProperty("status")]
            public string? Status { get; set; }

            /// <summary>
            /// For <c>unranked_mod</c> and <c>mod_settings</c>: the mod.
            /// </summary>
            [JsonProperty("acronym")]
            public string? Acronym { get; set; }
        }

        public class Player
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; } = string.Empty;
        }

        public class Target
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; } = string.Empty;

            /// <summary>
            /// Their place, from 1.
            /// </summary>
            [JsonProperty("rank")]
            public long Rank { get; set; }

            /// <summary>
            /// On a beatmap, the osu!stable score a new best needs above the current one; in the ranking, the ranked score needed.
            /// </summary>
            [JsonProperty("score_needed")]
            public long ScoreNeeded { get; set; }
        }
    }
}
