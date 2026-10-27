// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Scoring
{
    /// <summary>
    /// The best possible osu!stable score on a beatmap, split into parts, as computed by servers that rank plays by osu!stable score.
    /// </summary>
    public class StableScoreAttributes
    {
        /// <summary>
        /// Base hit values without combo bonus (300 per object, 30/10 for slider parts, ...).
        /// </summary>
        [JsonProperty("accuracy_score")]
        public long AccuracyScore { get; set; }

        /// <summary>
        /// Combo bonus of a perfect play without mods.
        /// </summary>
        [JsonProperty("combo_score")]
        public long ComboScore { get; set; }

        /// <summary>
        /// Spinner (and other) bonus of a perfect play.
        /// </summary>
        [JsonProperty("bonus_score")]
        public long BonusScore { get; set; }

        [JsonProperty("max_combo")]
        public long MaxCombo { get; set; }

        /// <summary>
        /// Objects that get a 300/100/50 judgement.
        /// </summary>
        [JsonProperty("object_count")]
        public long ObjectCount { get; set; }

        [JsonProperty("slider_count")]
        public long SliderCount { get; set; }

        /// <summary>
        /// The part of <see cref="AccuracyScore"/> that comes from slider heads, ticks, repeats and tails.
        /// </summary>
        [JsonProperty("nested_score")]
        public long NestedScore { get; set; }

        [JsonProperty("nested_count")]
        public long NestedCount { get; set; }

        /// <summary>
        /// For osu! beatmaps converted to osu!mania: the key count the converter picks (0 otherwise).
        /// </summary>
        [JsonProperty("converted_columns")]
        public long ConvertedColumns { get; set; }
    }
}
