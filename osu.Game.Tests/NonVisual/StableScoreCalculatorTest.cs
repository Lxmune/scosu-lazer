// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// The expected scores are the server's (scosu's <c>stable_score::play_score</c>) for the same plays.
    /// </summary>
    [TestFixture]
    public class StableScoreCalculatorTest
    {
        private static readonly StableScoreAttributes osu = new StableScoreAttributes
        {
            AccuracyScore = 61_350,
            ComboScore = 2_345_678,
            BonusScore = 4_000,
            MaxCombo = 650,
            ObjectCount = 200,
            SliderCount = 80,
            NestedScore = 3_350,
            NestedCount = 450,
        };

        private static readonly Dictionary<HitResult, int> osu_misses = new Dictionary<HitResult, int>
        {
            [HitResult.Great] = 180,
            [HitResult.Ok] = 12,
            [HitResult.Meh] = 3,
            [HitResult.Miss] = 5,
            [HitResult.LargeTickHit] = 350,
            [HitResult.LargeTickMiss] = 20,
            [HitResult.SliderTailHit] = 70,
        };

        [Test]
        public void TestOsu()
        {
            var fullCombo = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 200,
                [HitResult.LargeTickHit] = 370,
                [HitResult.SliderTailHit] = 80,
                [HitResult.SmallBonus] = 20,
                [HitResult.LargeBonus] = 1,
            };
            Assert.That(calculate(osu, 0, fullCombo, 650), Is.EqualTo(2_412_128));

            Assert.That(calculate(osu, 0, osu_misses, 220, mods: new[] { new APIMod { Acronym = "HD" }, new APIMod { Acronym = "DT" } }), Is.EqualTo(714_364));

            var classic = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 190,
                [HitResult.Ok] = 10,
                [HitResult.LargeTickHit] = 400,
                [HitResult.SmallTickHit] = 70,
                [HitResult.SmallTickMiss] = 10,
            };
            Assert.That(calculate(osu, 0, classic, 400, mods: new[] { new APIMod { Acronym = "CL" }, new APIMod { Acronym = "HR" } }), Is.EqualTo(1_197_454));

            var partial = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 40,
                [HitResult.Ok] = 2,
                [HitResult.LargeTickHit] = 60,
                [HitResult.SliderTailHit] = 15,
            };
            Assert.That(calculate(osu, 0, partial, 117), Is.EqualTo(85_954));
        }

        [Test]
        public void TestDifficultyAdjustHalvesComboBonusUnlessOnlyApproachRateChanges()
        {
            var overallDifficulty = new APIMod { Acronym = "DA", Settings = { ["overall_difficulty"] = 5f } };
            Assert.That(calculate(osu, 0, osu_misses, 220, mods: new[] { overallDifficulty }), Is.EqualTo(334_839));

            var approachRate = new APIMod { Acronym = "DA", Settings = { ["approach_rate"] = 10f, ["extended_limits"] = true } };
            Assert.That(calculate(osu, 0, osu_misses, 220, mods: new[] { approachRate }), Is.EqualTo(calculate(osu, 0, osu_misses, 220)));
        }

        [Test]
        public void TestTaiko()
        {
            var taiko = new StableScoreAttributes { AccuracyScore = 300_000, ComboScore = 1_500_000, BonusScore = 50_000, MaxCombo = 1000, ObjectCount = 1000 };
            var statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 950,
                [HitResult.Ok] = 40,
                [HitResult.Miss] = 10,
                [HitResult.SmallBonus] = 30,
                [HitResult.LargeBonus] = 2,
            };
            var maximum = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 1000,
                [HitResult.SmallBonus] = 60,
                [HitResult.LargeBonus] = 4,
            };
            Assert.That(calculate(taiko, 1, statistics, 500, maximum, new[] { new APIMod { Acronym = "HD" } }), Is.EqualTo(1_599_066));
        }

        [Test]
        public void TestCatch()
        {
            var catchAttributes = new StableScoreAttributes { ComboScore = 3_000_000, BonusScore = 22_000, MaxCombo = 800 };
            var statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 500,
                [HitResult.LargeTickHit] = 290,
                [HitResult.SmallTickHit] = 300,
                [HitResult.Miss] = 5,
                [HitResult.LargeTickMiss] = 5,
                [HitResult.LargeBonus] = 15,
            };
            Assert.That(calculate(catchAttributes, 2, statistics, 400, mods: new[] { new APIMod { Acronym = "HR" } }), Is.EqualTo(1_619_051));
        }

        [Test]
        public void TestMania()
        {
            var mania = new StableScoreAttributes { ComboScore = 1_000_000, ConvertedColumns = 7 };
            var statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Perfect] = 900,
                [HitResult.Great] = 80,
                [HitResult.Good] = 10,
                [HitResult.Ok] = 5,
                [HitResult.Meh] = 3,
                [HitResult.Miss] = 2,
            };
            var maximum = new Dictionary<HitResult, int> { [HitResult.Perfect] = 1000 };

            Assert.That(calculate(mania, 3, statistics, 0, maximum, new[] { new APIMod { Acronym = "4K" } }), Is.EqualTo(749_649));
            Assert.That(calculate(mania, 3, statistics, 0, maximum, new[] { new APIMod { Acronym = "NF" } }), Is.EqualTo(480_544));
            Assert.That(calculate(mania, 3, new Dictionary<HitResult, int> { [HitResult.Perfect] = 1000 }, 0, maximum), Is.EqualTo(1_000_000));
        }

        [Test]
        public void TestScoreGrowsDuringPlay()
        {
            var mania = new StableScoreAttributes { ComboScore = 1_000_000 };
            var maximum = new Dictionary<HitResult, int> { [HitResult.Perfect] = 1000 };

            long previous = 0;

            foreach (int judged in new[] { 1, 100, 500, 999, 1000 })
            {
                long score = calculate(mania, 3, new Dictionary<HitResult, int> { [HitResult.Perfect] = judged }, 0, maximum);
                Assert.That(score, Is.GreaterThan(previous));
                previous = score;
            }
        }

        [Test]
        public void TestJudgementOrder()
        {
            var osuStatistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 50,
                [HitResult.Ok] = 5,
                [HitResult.Miss] = 2,
                [HitResult.LargeTickHit] = 30,
                [HitResult.LargeTickMiss] = 1,
                [HitResult.SliderTailHit] = 10,
            };
            string osuOrder = new string('G', 30) + new string('T', 20) + "x" + new string('o', 5) + new string('G', 20) + new string('S', 10) + "t" + new string('T', 10) + "x";
            Assert.That(calculate(osu, 0, osuStatistics, 50, order: osuOrder), Is.EqualTo(35_445));

            var taiko = new StableScoreAttributes { AccuracyScore = 300_000, ComboScore = 1_500_000, MaxCombo = 1000, ObjectCount = 1000 };
            var taikoStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 95, [HitResult.Ok] = 4, [HitResult.Miss] = 1 };
            string taikoOrder = new string('G', 60) + "x" + new string('o', 4) + new string('G', 35);
            Assert.That(calculate(taiko, 1, taikoStatistics, 60, order: taikoOrder), Is.EqualTo(61_293));

            var catchAttributes = new StableScoreAttributes { ComboScore = 3_000_000, BonusScore = 22_000, MaxCombo = 800 };
            var catchStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 40, [HitResult.LargeTickHit] = 20, [HitResult.Miss] = 2 };
            string catchOrder = new string('G', 20) + new string('T', 10) + "x" + new string('G', 20) + new string('T', 10) + "x";
            Assert.That(calculate(catchAttributes, 2, catchStatistics, 30, order: catchOrder), Is.EqualTo(22_166));

            var mania = new StableScoreAttributes { ComboScore = 1_000_000 };
            var maniaStatistics = new Dictionary<HitResult, int> { [HitResult.Perfect] = 90, [HitResult.Great] = 5, [HitResult.Miss] = 5 };
            string maniaOrder = new string('P', 45) + new string('x', 5) + new string('G', 5) + new string('P', 45);
            Assert.That(calculate(mania, 3, maniaStatistics, 50, new Dictionary<HitResult, int> { [HitResult.Perfect] = 100 }, order: maniaOrder), Is.EqualTo(856_953));
        }

        [TestCase("GGGtTT")]
        [TestCase("GGGtTTSS")]
        [TestCase("GGGtTT?")]
        [TestCase("GGGTTtS")]
        public void TestOrderThatDisagreesWithPlayIsIgnored(string order)
        {
            var statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 3,
                [HitResult.LargeTickHit] = 2,
                [HitResult.LargeTickMiss] = 1,
                [HitResult.SliderTailHit] = 1,
            };
            Assert.That(calculate(osu, 0, statistics, 4, order: order), Is.EqualTo(calculate(osu, 0, statistics, 4)));
        }

        [Test]
        public void TestGameplayStableScoreFollowsJudgements()
        {
            var circles = Enumerable.Range(0, 3).Select(i => new HitCircle { StartTime = i * 1000 }).ToList<HitObject>();
            var beatmap = new TestBeatmap(new OsuRuleset().RulesetInfo) { HitObjects = circles };
            var scoreProcessor = new ScoreProcessor(new OsuRuleset());
            scoreProcessor.ApplyBeatmap(beatmap);

            var stableScore = new GameplayStableScore(scoreProcessor, new OsuRuleset(), Array.Empty<Mod>())
            {
                Attributes = new StableScoreAttributes { AccuracyScore = 900, ComboScore = 1000, MaxCombo = 3, ObjectCount = 3 }
            };

            var results = new[] { HitResult.Great, HitResult.Miss, HitResult.Great }
                          .Select((type, i) => new JudgementResult(circles[i], new OsuJudgement()) { Type = type })
                          .ToArray();

            foreach (var result in results)
                scoreProcessor.ApplyResult(result);

            Assert.That(stableScore.JudgementOrder, Is.EqualTo("GxG"));
            Assert.That(stableScore.TotalScore.Value, Is.EqualTo(StableScoreCalculator.Calculate(stableScore.Attributes, 0, scoreProcessor.Statistics,
                scoreProcessor.MaximumStatistics, 1, Array.Empty<APIMod>(), "GxG")));
            Assert.That(stableScore.GetDisplayScore(ScoringMode.Classic), Is.EqualTo(stableScore.TotalScore.Value));

            scoreProcessor.RevertResult(results[2]);
            Assert.That(stableScore.JudgementOrder, Is.EqualTo("Gx"));
        }

        [Test]
        public void TestComboProgress()
        {
            Assert.That(StableScoreCalculator.ComboProgress(StableScoreCalculator.ComboCurve.Quadratic, 500, 500, 500, 0), Is.EqualTo(1));
            Assert.That(StableScoreCalculator.ComboProgress(StableScoreCalculator.ComboCurve.Capped, 500, 500, 500, 0), Is.EqualTo(1));
            Assert.That(StableScoreCalculator.ComboProgress(StableScoreCalculator.ComboCurve.Quadratic, 500, 499, 499, 1), Is.GreaterThan(0.99));
            Assert.That(StableScoreCalculator.ComboProgress(StableScoreCalculator.ComboCurve.Quadratic, 500, 250, 499, 1), Is.InRange(0.45, 0.55));
        }

        [Test]
        public void TestMaximumScoreIsAPerfectPlay()
        {
            var perfect = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = 200,
                [HitResult.LargeTickHit] = 370,
                [HitResult.SliderTailHit] = 80,
                [HitResult.SmallBonus] = 29,
                [HitResult.LargeBonus] = 1,
            };
            var hidden = new[] { new APIMod { Acronym = "HD" } };
            // 300 per object and the slider parts.
            var attributes = new StableScoreAttributes
            {
                AccuracyScore = 63_350,
                ComboScore = 2_345_678,
                BonusScore = 4_000,
                MaxCombo = 650,
                ObjectCount = 200,
                SliderCount = 80,
                NestedScore = 3_350,
                NestedCount = 450,
            };

            Assert.That(StableScoreCalculator.MaximumScore(attributes, 0, Array.Empty<APIMod>()), Is.EqualTo(63_350 + 2_345_678 + 4_000));
            Assert.That(StableScoreCalculator.MaximumScore(attributes, 0, hidden), Is.EqualTo(calculate(attributes, 0, perfect, 650, mods: hidden)));

            // osu!mania: 1,000,000 times the multiplier, which includes the key count penalty of a converted beatmap.
            var converted = new StableScoreAttributes { ComboScore = 1_000_000, ConvertedColumns = 7 };
            Assert.That(StableScoreCalculator.MaximumScore(converted, 3, new[] { new APIMod { Acronym = "4K" } }), Is.EqualTo(780_000));
            Assert.That(StableScoreCalculator.MaximumScore(converted, 3, new[] { new APIMod { Acronym = "EZ" } }), Is.EqualTo(500_000));
        }

        [TestCase(0, "HD", null, true)]
        [TestCase(0, "HD", "only_fade_approach_circles", false)]
        [TestCase(0, "DT", "adjust_pitch", true)]
        [TestCase(0, "DT", "speed_change", false)]
        [TestCase(0, "CL", "no_slider_head_accuracy", true)]
        [TestCase(0, "MR", "reflection", true)]
        [TestCase(0, "RX", null, true)]
        [TestCase(0, "AP", null, true)]
        [TestCase(0, "AC", "minimum_accuracy", true)]
        [TestCase(0, "SV2", null, false)]
        [TestCase(0, "WG", null, false)]
        [TestCase(1, "AC", "minimum_accuracy", true)]
        [TestCase(1, "MR", null, false)]
        [TestCase(1, "SW", null, true)]
        [TestCase(2, "NS", null, true)]
        [TestCase(2, "SO", null, false)]
        [TestCase(3, "7K", null, true)]
        [TestCase(3, "MR", null, true)]
        [TestCase(3, "MR", "reflection", false)]
        [TestCase(3, "RX", null, false)]
        public void TestRankedMods(int rulesetId, string acronym, string? setting, bool ranked)
        {
            var mod = new APIMod { Acronym = acronym };
            if (setting != null)
                mod.Settings[setting] = 1.3;

            Assert.That(StableScoreCalculator.IsRankedMod(rulesetId, mod), Is.EqualTo(ranked));
        }

        [Test]
        public void TestDifficultyAdjustIsRankedUnlessApproachRateIsBelowZero()
        {
            Assert.That(StableScoreCalculator.IsRankedMod(0, new APIMod { Acronym = "DA", Settings = { ["circle_size"] = 3f, ["drain_rate"] = 8f } }), Is.True);
            Assert.That(StableScoreCalculator.IsRankedMod(2, new APIMod { Acronym = "DA", Settings = { ["approach_rate"] = 11f, ["extended_limits"] = true } }), Is.True);
            Assert.That(StableScoreCalculator.IsRankedMod(0, new APIMod { Acronym = "DA", Settings = { ["approach_rate"] = -5f, ["extended_limits"] = true } }), Is.False);
        }

        [Test]
        public void TestModEffects()
        {
            var osuRuleset = new OsuRuleset();
            var beatmap = new BeatmapInfo(osuRuleset.RulesetInfo, new BeatmapDifficulty { OverallDifficulty = 8, CircleSize = 4 })
            {
                TotalObjectCount = 100,
                EndTimeObjectCount = 10,
            };

            var approachRate = new OsuModDifficultyAdjust { ApproachRate = { Value = 9.5f } };
            Assert.That(StableScoreCalculator.GetModEffects(osuRuleset, beatmap, new Mod[] { approachRate, new OsuModHidden() }), Is.EqualTo((1.06, true)));

            var circleSize = new OsuModDifficultyAdjust { ApproachRate = { Value = 9.5f }, CircleSize = { Value = 3 } };
            Assert.That(StableScoreCalculator.GetModEffects(osuRuleset, beatmap, new Mod[] { circleSize, new OsuModHidden() }).Multiplier, Is.EqualTo(0.53).Within(1e-9));

            var lowApproachRate = new OsuModDifficultyAdjust { ExtendedLimits = { Value = true }, ApproachRate = { Value = -5 } };
            Assert.That(StableScoreCalculator.GetModEffects(osuRuleset, beatmap, new Mod[] { lowApproachRate }).Ranked, Is.False);

            Assert.That(StableScoreCalculator.GetModEffects(osuRuleset, beatmap, new Mod[] { new OsuModClassic(), new OsuModMirror() }), Is.EqualTo((1.0, true)));

            // The converter gives this beatmap 7 keys: fewer cost 0.04 more each.
            var maniaRuleset = new ManiaRuleset();
            Assert.That(StableScoreCalculator.GetModEffects(maniaRuleset, beatmap, new Mod[] { new ManiaModKey4() }).Multiplier, Is.EqualTo(0.78).Within(1e-9));
            Assert.That(StableScoreCalculator.GetModEffects(maniaRuleset, beatmap, new Mod[] { new ManiaModKey7() }).Multiplier, Is.EqualTo(1));
        }

        private static long calculate(StableScoreAttributes attributes, int rulesetId, Dictionary<HitResult, int> statistics, int maxCombo,
                                      Dictionary<HitResult, int>? maximumStatistics = null, IEnumerable<APIMod>? mods = null, string? order = null)
            => StableScoreCalculator.Calculate(attributes, rulesetId, statistics, maximumStatistics ?? new Dictionary<HitResult, int>(), maxCombo, mods?.ToArray() ?? [], order);
    }
}
