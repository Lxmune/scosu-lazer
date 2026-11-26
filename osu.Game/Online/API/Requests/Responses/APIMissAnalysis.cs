// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// scosu: where an osu! play missed and the likely cause of each miss, as the server reads it from the play's replay
    /// (see <see cref="GetScoreMissesRequest"/>).
    /// </summary>
    public class APIMissAnalysis
    {
        /// <summary>
        /// <see cref="Reason"/> when the server has no replay of the play (yet: a play just set has its replay saved when it is processed).
        /// </summary>
        public const string NO_REPLAY = @"no_replay";

        [JsonProperty("score_id")]
        public long ScoreId { get; set; }

        /// <summary>
        /// False when the play can't be analysed: <see cref="Reason"/> and <see cref="Message"/> then say why.
        /// </summary>
        [JsonProperty("available")]
        public bool Available { get; set; }

        /// <summary>
        /// <c>ruleset</c>, <see cref="NO_REPLAY"/>, <c>no_beatmap</c>, <c>unsupported_mods</c> or <c>unreadable_replay</c>.
        /// </summary>
        [JsonProperty("reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// The reason in words, to show as it is.
        /// </summary>
        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("misses")]
        public List<Miss> Misses { get; set; } = new List<Miss>();

        [JsonProperty("summary")]
        public MissSummary Summary { get; set; } = new MissSummary();

        public class Miss
        {
            /// <summary>
            /// The beatmap time of the miss, in milliseconds.
            /// </summary>
            [JsonProperty("time_ms")]
            public double Time { get; set; }

            /// <summary>
            /// <c>miss</c>, <c>slider_break</c> or <c>slider_end</c>.
            /// </summary>
            [JsonProperty("kind")]
            public string Kind { get; set; } = string.Empty;

            /// <summary>
            /// One of the <see cref="MissCauses"/>.
            /// </summary>
            [JsonProperty("cause")]
            public string Cause { get; set; } = string.Empty;

            /// <summary>
            /// The timing of the press nearest the object, in real milliseconds (negative: early), when there was one.
            /// </summary>
            [JsonProperty("offset_ms")]
            public double? Offset { get; set; }

            /// <summary>
            /// How far the cursor was from the object at that press, in circle radii.
            /// </summary>
            [JsonProperty("distance")]
            public double? Distance { get; set; }
        }

        public class MissSummary
        {
            [JsonProperty("misses")]
            public int Misses { get; set; }

            [JsonProperty("slider_breaks")]
            public int SliderBreaks { get; set; }

            [JsonProperty("slider_ends")]
            public int SliderEnds { get; set; }

            /// <summary>
            /// How many misses have each cause, by <see cref="Miss.Cause"/>.
            /// </summary>
            [JsonProperty("causes")]
            public Dictionary<string, int> Causes { get; set; } = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// scosu: the causes the server tells a miss by (<see cref="APIMissAnalysis.Miss.Cause"/>).
    /// </summary>
    public static class MissCauses
    {
        public const string EARLY = @"early";
        public const string LATE = @"late";
        public const string AIMED_SHORT = @"aimed_short";
        public const string AIMED_LONG = @"aimed_long";
        public const string AIMED_OFF = @"aimed_off";
        public const string BLOCKED = @"blocked";
        public const string NO_PRESS = @"no_press";
        public const string FOLLOW_LOST = @"follow_lost";
        public const string KEY_RELEASED = @"key_released";
    }
}
