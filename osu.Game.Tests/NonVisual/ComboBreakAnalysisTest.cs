// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ComboBreakAnalysisTest
    {
        [Test]
        public void TestBreaksResetTheComboBeforeThem()
        {
            var analysis = fromOrder("GGGxGGtG");

            Assert.That(analysis.MaxCombo, Is.EqualTo(3));
            Assert.That(analysis.Breaks.Select(b => b.ComboLost), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(analysis.Breaks.Select(b => b.Time), Is.EqualTo(new[] { 300d, 600d }));
            Assert.That(analysis.Breaks.Select(b => b.Result), Is.EqualTo(new[] { HitResult.Miss, HitResult.LargeTickMiss }));
            Assert.That(analysis.Points.Select(p => p.Combo), Is.EqualTo(new[] { 1, 2, 3, 0, 1, 2, 0, 1 }));
        }

        [Test]
        public void TestAMissRightAfterAnotherResetsNothing()
        {
            var analysis = fromOrder("GxxG");

            Assert.That(analysis.Breaks.Select(b => b.ComboLost), Is.EqualTo(new[] { 1, 0 }));
        }

        [Test]
        public void TestJudgementsThatDontChangeTheComboAreLeftOut()
        {
            var analysis = new ComboBreakAnalysis(new[]
            {
                new ComboBreakAnalysis.JudgedObject(0, HitResult.Great, HitResult.Great),
                new ComboBreakAnalysis.JudgedObject(50, HitResult.SmallTickMiss, HitResult.SmallTickHit),
                new ComboBreakAnalysis.JudgedObject(60, HitResult.IgnoreMiss, HitResult.IgnoreHit),
                new ComboBreakAnalysis.JudgedObject(100, HitResult.Great, HitResult.Great),
                new ComboBreakAnalysis.JudgedObject(900, HitResult.SmallBonus, HitResult.SmallBonus),
            });

            Assert.That(analysis.Breaks, Is.Empty);
            Assert.That(analysis.Points.Select(p => p.Combo), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(analysis.MaxCombo, Is.EqualTo(2));
            // The play still spans every judgement.
            Assert.That(analysis.EndTime, Is.EqualTo(900));
        }

        [Test]
        public void TestComboColumns()
        {
            // At 0, 100, ..., 700: combo 1 2 3 0 1 2 0 1.
            var analysis = fromOrder("GGGxGGtG");

            Assert.That(analysis.ComboColumns(1), Is.EqualTo(new[] { 3 }));
            // Slices of 175ms: [0, 175), [175, 350), [350, 525), [525, 700]. A slice starts at the combo carried into it.
            Assert.That(analysis.ComboColumns(4), Is.EqualTo(new[] { 2, 3, 2, 2 }));
            Assert.That(analysis.ComboColumns(0), Is.Empty);
            Assert.That(new ComboBreakAnalysis(Enumerable.Empty<ComboBreakAnalysis.JudgedObject>()).ComboColumns(3), Is.EqualTo(new[] { 0, 0, 0 }));
        }

        [Test]
        public void TestComboIsCarriedThroughSlicesWithoutJudgements()
        {
            var analysis = new ComboBreakAnalysis(new[]
            {
                new ComboBreakAnalysis.JudgedObject(0, HitResult.Great, HitResult.Great),
                new ComboBreakAnalysis.JudgedObject(10, HitResult.Great, HitResult.Great),
                // A long gap in the map.
                new ComboBreakAnalysis.JudgedObject(1000, HitResult.Great, HitResult.Great),
            });

            Assert.That(analysis.ComboColumns(4), Is.EqualTo(new[] { 2, 2, 2, 3 }));
        }

        [Test]
        public void TestFewBreaksAreDrawnEachOnItsOwn()
        {
            var analysis = new ComboBreakAnalysis(Enumerable.Range(0, 100).Select(i => new ComboBreakAnalysis.JudgedObject(i * 10, i % 30 == 29 ? HitResult.Miss : HitResult.Great, HitResult.Great)));

            Assert.That(analysis.Breaks, Has.Count.EqualTo(3));
            Assert.That(analysis.GroupBreaks(60).Select(g => (g.Shown, g.Count)), Is.EqualTo(analysis.Breaks.Select(b => (b, 1))));
        }

        /// <summary>
        /// A long play with hundreds of breaks draws a few marks, each standing for the breaks around it.
        /// </summary>
        [Test]
        public void TestManyBreaksAreDrawnTogether()
        {
            // 600 breaks over a minute, growing combos between them.
            var objects = new List<ComboBreakAnalysis.JudgedObject>();

            for (int i = 0; i < 600; i++)
            {
                for (int hit = 0; hit < i % 7; hit++)
                    objects.Add(new ComboBreakAnalysis.JudgedObject(objects.Count * 10, HitResult.Great, HitResult.Great));
                objects.Add(new ComboBreakAnalysis.JudgedObject(objects.Count * 10, HitResult.Miss, HitResult.Great));
            }

            var analysis = new ComboBreakAnalysis(objects);
            Assert.That(analysis.Breaks, Has.Count.EqualTo(600));

            var groups = analysis.GroupBreaks(60);

            Assert.That(groups, Has.Count.InRange(30, 61));
            Assert.That(groups.Sum(g => g.Count), Is.EqualTo(600), "every break is in a group");

            int first = 0;

            foreach (var group in groups)
            {
                var inGroup = analysis.Breaks.Skip(first).Take(group.Count).ToArray();
                Assert.That(group.Shown.ComboLost, Is.EqualTo(inGroup.Max(b => b.ComboLost)), "the group shows its worst break");
                Assert.That(inGroup, Does.Contain(group.Shown));
                first += group.Count;
            }
        }

        [Test]
        public void TestTheCostOfAChokeIsWhatTheServerScoredIt()
        {
            // Plays of the same beatmap scored by the server: one perfect, one missing only its very last object.
            var perfect = sharedPlay("osu 75.osu perfect (in order)");
            var choke = sharedPlay("osu 75.osu choke at the end (in order)");

            var analysis = fromOrder(choke.JudgementOrder!);
            Assert.That(analysis.Breaks, Has.Count.EqualTo(1));
            Assert.That(analysis.Breaks[0].ComboLost, Is.EqualTo(choke.MaxCombo));

            Assert.That(analysis.ComputeCosts(choke.Attributes, 0, choke.Statistics, choke.MaximumStatistics, multiplier(choke)), Is.True);
            Assert.That(analysis.HasCosts, Is.True);

            // Hitting that one object makes the play perfect.
            Assert.That(analysis.Breaks[0].Cost, Is.EqualTo(perfect.Score - choke.Score));
        }

        [Test]
        public void TestEveryBreakCostsAtMostTheScoreMissing()
        {
            var perfect = sharedPlay("osu 75.osu perfect (in order)");
            var play = sharedPlay("osu 75.osu misses and dropped ticks (in order)");

            var analysis = fromOrder(play.JudgementOrder!);
            Assert.That(analysis.Breaks, Has.Count.EqualTo(play.Statistics[HitResult.Miss] + play.Statistics[HitResult.LargeTickMiss]));
            Assert.That(analysis.MaxCombo, Is.EqualTo(play.MaxCombo));

            Assert.That(analysis.ComputeCosts(play.Attributes, 0, play.Statistics, play.MaximumStatistics, multiplier(play)), Is.True);

            foreach (var comboBreak in analysis.Breaks)
            {
                Assert.That(comboBreak.Cost, Is.Not.Null);
                Assert.That(comboBreak.Cost, Is.GreaterThan(0).And.LessThan(perfect.Score - play.Score));
            }
        }

        [Test]
        public void TestCostsAreUnknownWhenTheJudgementsDontMatchThePlay()
        {
            var play = sharedPlay("osu 75.osu choke at the end (in order)");

            // A rewound play, say: one judgement fewer than the play's statistics.
            var analysis = fromOrder(play.JudgementOrder![1..]);

            Assert.That(analysis.ComputeCosts(play.Attributes, 0, play.Statistics, play.MaximumStatistics, multiplier(play)), Is.False);
            Assert.That(analysis.HasCosts, Is.False);
            Assert.That(analysis.Breaks.Select(b => b.Cost), Is.All.Null);
        }

        [TestCase("osu 75.osu misses and dropped ticks (in order)")]
        [TestCase("osu 75.osu choke at the end (in order)")]
        [TestCase("osu 75.osu misses with HDDT")]
        [TestCase("osu 100.osu misses and dropped ticks (in order)")]
        public void TestTheScoreRaceEndsAtThePlaysScoreAndTheMaximum(string name)
        {
            var play = sharedPlay(name);
            var analysis = fromPlay(play);
            double playMultiplier = multiplier(play);

            Assert.That(analysis.ComputeScoreRace(play.Attributes, 0, play.Statistics, play.MaximumStatistics, playMultiplier, 160), Is.True);

            var race = analysis.Race!;
            Assert.That(race.Times, Has.Length.EqualTo(160));
            Assert.That(race.Times[0], Is.EqualTo(analysis.StartTime));
            Assert.That(race.Times[^1], Is.EqualTo(analysis.EndTime));

            // What the server scored the play, and the most the beatmap gives with its mods: the Score Lost card's two numbers.
            Assert.That(race.Play[^1], Is.EqualTo(play.Score));
            Assert.That(race.FullCombo[^1], Is.EqualTo(StableScoreCalculator.MaximumScore(play.Attributes, 0, playMultiplier)));

            for (int i = 0; i < race.Times.Length; i++)
            {
                Assert.That(race.FullCombo[i], Is.GreaterThanOrEqualTo(race.Play[i]), $"a full combo is never behind, at sample {i}");

                if (i > 0)
                    Assert.That(race.FullCombo[i], Is.GreaterThanOrEqualTo(race.FullCombo[i - 1]), $"a full combo never loses score, at sample {i}");
            }
        }

        [Test]
        public void TestBeforeAChokeThePlayIsAFullComboSoFar()
        {
            var play = sharedPlay("osu 75.osu choke at the end (in order)");
            var analysis = fromOrder(play.JudgementOrder!);

            Assert.That(analysis.ComputeScoreRace(play.Attributes, 0, play.Statistics, play.MaximumStatistics, multiplier(play), 160), Is.True);

            var race = analysis.Race!;
            double length = analysis.EndTime - analysis.StartTime;

            // Every sample before the last object is a perfect play so far. A full combo also has its spinner bonus,
            // spread over the play here (the judgements of this play don't include the spinners').
            for (int i = 0; i < race.Times.Length - 1; i++)
            {
                long bonus = (long)System.Math.Round(play.Attributes.BonusScore * (race.Times[i] - analysis.StartTime) / length, System.MidpointRounding.AwayFromZero);
                Assert.That(race.FullCombo[i] - race.Play[i], Is.EqualTo(bonus), $"sample {i}");
            }

            Assert.That(race.Play[^2], Is.GreaterThan(race.Play[0]));
        }

        [Test]
        public void TestTheScoreRaceIsUnknownWhenTheJudgementsDontMatchThePlay()
        {
            var play = sharedPlay("osu 75.osu choke at the end (in order)");
            var analysis = fromOrder(play.JudgementOrder![1..]);

            Assert.That(analysis.ComputeScoreRace(play.Attributes, 0, play.Statistics, play.MaximumStatistics, multiplier(play), 160), Is.False);
            Assert.That(analysis.Race, Is.Null);
        }

        /// <summary>
        /// A play of osu! judgements (in <see cref="StableScoreCalculator.EncodeJudgement"/>'s codes), one every 100ms.
        /// </summary>
        private static ComboBreakAnalysis fromOrder(string order)
            => new ComboBreakAnalysis(order.Select((code, i) => new ComboBreakAnalysis.JudgedObject(i * 100, decode(code), maxResult(code))));

        /// <summary>
        /// A play of <see cref="fromOrder"/>, with the judgements its order leaves out because they don't change the combo,
        /// as a play's hit events have them: a missed slider tail, say, is judged <see cref="HitResult.IgnoreMiss"/> at best <see cref="HitResult.SliderTailHit"/>.
        /// </summary>
        private static ComboBreakAnalysis fromPlay(StableScoreCalculatorTest.SharedPlay play)
        {
            string order = play.JudgementOrder!;
            var judgements = order.Select((code, i) => new ComboBreakAnalysis.JudgedObject(i * 100, decode(code), maxResult(code))).ToList();

            foreach (var (result, count) in play.MaximumStatistics)
            {
                int inOrder = judgements.Count(j => j.MaxResult == result);

                for (int i = inOrder; i < count; i++)
                    judgements.Add(new ComboBreakAnalysis.JudgedObject((order.Length - 1) * 100, HitResult.IgnoreMiss, result));
            }

            return new ComboBreakAnalysis(judgements);
        }

        private static HitResult decode(char code) => code switch
        {
            'P' => HitResult.Perfect,
            'G' => HitResult.Great,
            'g' => HitResult.Good,
            'o' => HitResult.Ok,
            'm' => HitResult.Meh,
            'x' => HitResult.Miss,
            'T' => HitResult.LargeTickHit,
            't' => HitResult.LargeTickMiss,
            'S' => HitResult.SliderTailHit,
            _ => throw new System.ArgumentException($"no osu! judgement {code}"),
        };

        private static HitResult maxResult(char code) => code switch
        {
            'T' or 't' => HitResult.LargeTickHit,
            'S' => HitResult.SliderTailHit,
            _ => HitResult.Great,
        };

        private static double multiplier(StableScoreCalculatorTest.SharedPlay play)
            => StableScoreCalculator.ScoreMultiplier(new OsuRuleset(), new BeatmapDifficulty
            {
                CircleSize = play.Attributes.CircleSize,
                DrainRate = play.Attributes.DrainRate,
                OverallDifficulty = play.Attributes.OverallDifficulty,
            }, play.Mods.Select(m => m.ToMod(new OsuRuleset())));

        private static StableScoreCalculatorTest.SharedPlay sharedPlay(string name)
        {
            using (var stream = TestResources.OpenResource("scosu-stable-score-plays.json"))
            using (var reader = new StreamReader(stream))
                return JsonConvert.DeserializeObject<List<StableScoreCalculatorTest.SharedPlay>>(reader.ReadToEnd())!.Single(p => p.Name == name);
        }
    }
}
