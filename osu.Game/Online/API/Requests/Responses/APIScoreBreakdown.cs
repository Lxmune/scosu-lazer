// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// How much of the beatmap's best osu!stable score a play earned, and what it lost (the website's score breakdown).
    /// </summary>
    public class APIScoreBreakdown
    {
        [JsonProperty("score_id")]
        public long ScoreId { get; set; }

        /// <summary>
        /// The play's osu!stable score.
        /// </summary>
        [JsonProperty("score")]
        public long Score { get; set; }

        /// <summary>
        /// The best osu!stable score possible on the beatmap with the play's mods.
        /// </summary>
        [JsonProperty("max_score")]
        public long MaxScore { get; set; }

        /// <summary>
        /// <see cref="MaxScore"/> minus <see cref="Score"/>, never below 0.
        /// </summary>
        [JsonProperty("missing")]
        public long Missing { get; set; }

        /// <summary>
        /// Null where the gap can't be told apart by cause (osu!mania, plays imported from osu!stable).
        /// </summary>
        [JsonProperty("split")]
        public ScoreSplit? Split { get; set; }

        /// <summary>
        /// The share of <see cref="MaxScore"/> the play earned, from 0 to 1.
        /// </summary>
        [JsonIgnore]
        public double Fraction => MaxScore <= 0 ? 0 : System.Math.Clamp((double)Score / MaxScore, 0, 1);

        public class ScoreSplit
        {
            /// <summary>
            /// What the combo breaks cost.
            /// </summary>
            [JsonProperty("combo_loss")]
            public long ComboLoss { get; set; }

            /// <summary>
            /// What the 100s, 50s and misses cost.
            /// </summary>
            [JsonProperty("judgement_loss")]
            public long JudgementLoss { get; set; }

            /// <summary>
            /// False when the play has no judgement order, so <see cref="ComboLoss"/> is an estimate.
            /// </summary>
            [JsonProperty("exact")]
            public bool Exact { get; set; }
        }
    }
}
