// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// scosu: how hard a score's beatmap is over its length with the score's mods (see <see cref="GetScoreStrainsRequest"/>).
    /// </summary>
    public class APIScoreStrains
    {
        /// <summary>
        /// The beatmap's star rating with the score's mods.
        /// </summary>
        [JsonProperty("stars")]
        public double Stars { get; set; }

        /// <summary>
        /// The star rating of the precision skills (aim, patterns, movement), or <c>null</c> where the ruleset has no such split (osu!catch, osu!mania).
        /// </summary>
        [JsonProperty("precision_stars")]
        public double? PrecisionStars { get; set; }

        /// <summary>
        /// The star rating of the speed skills (tapping, streams, stamina), or <c>null</c> where the ruleset has no such split.
        /// </summary>
        [JsonProperty("speed_stars")]
        public double? SpeedStars { get; set; }

        /// <summary>
        /// The length of each section of <see cref="Total"/>, in seconds of the play's time (beatmap time divided by the play's rate),
        /// counted from the beatmap's start.
        /// </summary>
        [JsonProperty("section_seconds")]
        public double SectionSeconds { get; set; }

        /// <summary>
        /// The difficulty of each section.
        /// </summary>
        [JsonProperty("total")]
        public List<double> Total { get; set; } = new List<double>();

        /// <summary>
        /// The difficulty of the precision skills in each section.
        /// </summary>
        [JsonProperty("precision")]
        public List<double> Precision { get; set; } = new List<double>();

        /// <summary>
        /// The difficulty of the speed skills in each section.
        /// </summary>
        [JsonProperty("speed")]
        public List<double> Speed { get; set; } = new List<double>();

        /// <summary>
        /// Whether the difficulty is split into precision and speed (otherwise only <see cref="Total"/> means something).
        /// </summary>
        [JsonIgnore]
        public bool HasSkillSplit => PrecisionStars != null && SpeedStars != null && Precision.Count == Total.Count && Speed.Count == Total.Count;
    }
}
