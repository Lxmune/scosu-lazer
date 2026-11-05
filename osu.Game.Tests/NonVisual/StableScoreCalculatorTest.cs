// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// Plays the scosu server scored with its own stable score calculator (scosu-rust's <c>src/stable_score/shared_plays.rs</c>):
    /// <see cref="StableScoreCalculator"/> must give each the same score, or the score shown in game differs from the leaderboard's.
    /// The server's <c>make score-plays</c> writes <c>Resources/scosu-stable-score-plays.json</c> again after a change.
    /// </summary>
    [TestFixture]
    public class StableScoreCalculatorTest
    {
        [TestCaseSource(nameof(sharedPlays))]
        public void TestServerScore(SharedPlay play)
        {
            long score = StableScoreCalculator.Calculate(play.Attributes, play.RulesetId, play.Statistics, play.MaximumStatistics, play.MaxCombo, play.Mods,
                play.JudgementOrder);

            Assert.That(score, Is.EqualTo(play.Score));
        }

        private static IEnumerable<TestCaseData> sharedPlays() => loadSharedPlays().Select(play => new TestCaseData(play).SetName(play.Name));

        private static List<SharedPlay> loadSharedPlays()
        {
            using (var stream = TestResources.OpenResource("scosu-stable-score-plays.json"))
            using (var reader = new StreamReader(stream))
                return JsonConvert.DeserializeObject<List<SharedPlay>>(reader.ReadToEnd())!;
        }

        public class SharedPlay
        {
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;

            [JsonProperty("ruleset_id")]
            public int RulesetId { get; set; }

            [JsonProperty("attributes")]
            public StableScoreAttributes Attributes { get; set; } = new StableScoreAttributes();

            [JsonProperty("statistics")]
            public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

            [JsonProperty("maximum_statistics")]
            public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new Dictionary<HitResult, int>();

            [JsonProperty("max_combo")]
            public int MaxCombo { get; set; }

            [JsonProperty("mods")]
            public List<APIMod> Mods { get; set; } = new List<APIMod>();

            [JsonProperty("judgement_order")]
            public string? JudgementOrder { get; set; }

            [JsonProperty("score")]
            public long Score { get; set; }

            public override string ToString() => Name;
        }
    }
}
