// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A user's attempts at a beatmap in one leaderboard mode (the website's beatmap attempts).
    /// </summary>
    public class APIBeatmapAttempts
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        /// <summary>
        /// The mode (<c>osu</c>, <c>osurx</c>, ...) the numbers are about.
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// Submitted plays, passed or failed.
        /// </summary>
        [JsonProperty("plays")]
        public long Plays { get; set; }

        [JsonProperty("passes")]
        public long Passes { get; set; }

        /// <summary>
        /// <c>passes / plays</c>, from 0 to 1, or <c>null</c> without plays.
        /// </summary>
        [JsonProperty("pass_rate")]
        public double? PassRate { get; set; }

        /// <summary>
        /// Plays that were started but never finished (quit, retried).
        /// </summary>
        [JsonProperty("unsubmitted")]
        public long Unsubmitted { get; set; }

        /// <summary>
        /// The passed play that came closest to a full combo.
        /// </summary>
        [JsonProperty("closest")]
        public ClosestAttempt? Closest { get; set; }

        public class ClosestAttempt
        {
            [JsonProperty("score_id")]
            public long ScoreId { get; set; }

            [JsonProperty("score")]
            public long Score { get; set; }

            [JsonProperty("accuracy")]
            public double Accuracy { get; set; }

            [JsonProperty("rank")]
            public string Rank { get; set; } = string.Empty;

            [JsonProperty("max_combo")]
            public int MaxCombo { get; set; }

            /// <summary>
            /// The beatmap's own maximum combo (0 if unknown).
            /// </summary>
            [JsonProperty("beatmap_max_combo")]
            public int BeatmapMaxCombo { get; set; }

            /// <summary>
            /// Circles and slider heads missed.
            /// </summary>
            [JsonProperty("misses")]
            public long Misses { get; set; }

            /// <summary>
            /// Slider ticks and repeats missed (counted with <see cref="Misses"/> by servers before 2026.11.26).
            /// </summary>
            [JsonProperty("slider_breaks")]
            public long SliderBreaks { get; set; }

            [JsonProperty("full_combo")]
            public bool FullCombo { get; set; }
        }
    }

    /// <summary>
    /// What one beatmap could add to a user's ranked score (the website's per-beatmap progress).
    /// </summary>
    public class APIBeatmapGain
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        /// <summary>
        /// The user's best score on the beatmap in the mode, or <c>null</c> if they never played it.
        /// </summary>
        [JsonProperty("score")]
        public long? Score { get; set; }

        /// <summary>
        /// The estimated ranked score beating the best would add, or <c>null</c> when there is nothing to estimate.
        /// </summary>
        [JsonProperty("gain")]
        public long? Gain { get; set; }

        /// <summary>
        /// How the estimate was made: <c>peers</c>, <c>typical</c> or <c>combo</c>.
        /// </summary>
        [JsonProperty("basis")]
        public string? Basis { get; set; }
    }
}
