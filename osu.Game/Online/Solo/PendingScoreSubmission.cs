// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.Solo
{
    /// <summary>
    /// A solo score submission saved to disk by <see cref="ScoreSubmissionQueue"/> until the server has it.
    /// </summary>
    public class PendingScoreSubmission
    {
        /// <summary>
        /// The score token the play was started with.
        /// </summary>
        [JsonProperty("token")]
        public long Token { get; set; }

        /// <summary>
        /// The player who made the play: only their login submits it.
        /// </summary>
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        /// <summary>
        /// The beatmap's name, for notifications.
        /// </summary>
        [JsonProperty("beatmap_title")]
        public string BeatmapTitle { get; set; } = string.Empty;

        /// <summary>
        /// The submission request's body, exactly as first sent.
        /// </summary>
        [JsonProperty("score")]
        public string ScoreJson { get; set; } = string.Empty;

        /// <summary>
        /// The replay's frames as scosu's replay upload takes them, if the play has any.
        /// </summary>
        [JsonProperty("replay")]
        public string? ReplayJson { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("next_attempt_at")]
        public DateTimeOffset NextAttemptAt { get; set; }

        /// <summary>
        /// Retries so far (the first submission, made by the player screen, isn't counted).
        /// </summary>
        [JsonProperty("attempts")]
        public int Attempts { get; set; }
    }
}
