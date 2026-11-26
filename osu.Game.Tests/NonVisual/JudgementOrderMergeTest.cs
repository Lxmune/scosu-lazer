// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// scosu: a replayed play's judgements, corrected with the ones the server kept from the play itself.
    /// </summary>
    [TestFixture]
    public class JudgementOrderMergeTest
    {
        [Test]
        public void TestCloseCallsAreJudgedAsTheyWerePlayed()
        {
            // As played: a 100 on the second circle, the slider tick held, a miss on the last circle.
            const string played = "GoTGx";

            // As replayed: the 100 came out a 50, the tick was dropped, and the miss was hit.
            var replayed = events(
                (new HitCircle { StartTime = 0 }, HitResult.Great),
                (new HitCircle { StartTime = 100 }, HitResult.Meh),
                (new SliderTick { StartTime = 200 }, HitResult.LargeTickMiss),
                // Doesn't change the combo: not in the order, and left as it is.
                (new SpinnerBonusTick { StartTime = 250 }, HitResult.SmallBonus),
                (new HitCircle { StartTime = 300 }, HitResult.Great),
                (new HitCircle { StartTime = 400 }, HitResult.Ok));

            var merged = JudgementOrderMerge.Merge(replayed, played, out string outcome)!;

            Assert.That(merged, Is.Not.Null, outcome);
            Assert.That(outcome, Does.Contain("corrected 3"));
            Assert.That(merged.Select(e => e.Result), Is.EqualTo(new[] { HitResult.Great, HitResult.Ok, HitResult.LargeTickHit, HitResult.SmallBonus, HitResult.Great, HitResult.Miss }));
            Assert.That(merged.Select(e => e.HitObject), Is.EqualTo(replayed.Select(e => e.HitObject)), "every judgement stays on its object");
            Assert.That(merged.Select(e => e.TimeOffset), Is.EqualTo(replayed.Select(e => e.TimeOffset)), "the replay's timing is kept");

            // Scored from the merged judgements, the play is the one the server kept: its breaks, its combo.
            var analysis = ComboBreakAnalysis.FromHitEvents(merged);
            var statistics = merged.GroupBy(e => e.Result).ToDictionary(g => g.Key, g => g.Count());

            Assert.That(StableScoreCalculator.IsValidOrder(played, statistics, 4), Is.True);
            Assert.That(analysis.MaxCombo, Is.EqualTo(4));
            Assert.That(analysis.Breaks.Single().Time, Is.EqualTo(400));
        }

        [Test]
        public void TestReplayJudgedAsPlayed()
        {
            var replayed = events((new HitCircle { StartTime = 0 }, HitResult.Great), (new HitCircle { StartTime = 100 }, HitResult.Miss));

            var merged = JudgementOrderMerge.Merge(replayed, "Gx", out string outcome);

            Assert.That(merged!.Select(e => e.Result), Is.EqualTo(replayed.Select(e => e.Result)));
            Assert.That(outcome, Does.Contain("as it was played"));
        }

        /// <summary>
        /// A slider end missed leaves nothing in the order, so the play and its replay may count different numbers of judgements.
        /// </summary>
        [Test]
        public void TestSliderEndsJudgedDifferently()
        {
            // As played: the first slider's end held, the second's missed.
            const string played = "GSGG";

            var replayed = events(
                (new HitCircle { StartTime = 0 }, HitResult.Great),
                (new SliderTailCircle(new Slider()) { StartTime = 100 }, HitResult.IgnoreMiss),
                (new HitCircle { StartTime = 200 }, HitResult.Great),
                (new SliderTailCircle(new Slider()) { StartTime = 300 }, HitResult.SliderTailHit),
                (new HitCircle { StartTime = 400 }, HitResult.Great));

            var merged = JudgementOrderMerge.Merge(replayed, played, out string outcome)!;

            Assert.That(merged, Is.Not.Null, outcome);
            Assert.That(merged.Select(e => e.Result), Is.EqualTo(new[] { HitResult.Great, HitResult.SliderTailHit, HitResult.Great, HitResult.IgnoreMiss, HitResult.Great }));
            Assert.That(ComboBreakAnalysis.FromHitEvents(merged).Breaks, Is.Empty, "a slider end missed doesn't break the combo");
        }

        [TestCase("GG", TestName = "Too few")]
        [TestCase("GGGG", TestName = "Too many")]
        [TestCase("GTG", TestName = "A tick's judgement on a circle")]
        [TestCase("G?G", TestName = "An unknown code")]
        public void TestOrderThatDoesNotLineUpIsNotUsed(string order)
        {
            var replayed = events(
                (new HitCircle { StartTime = 0 }, HitResult.Great),
                (new HitCircle { StartTime = 100 }, HitResult.Ok),
                (new HitCircle { StartTime = 200 }, HitResult.Great));

            Assert.That(JudgementOrderMerge.Merge(replayed, order, out string outcome), Is.Null);
            Assert.That(outcome, Is.Not.Empty);
        }

        private static List<HitEvent> events(params (HitObject HitObject, HitResult Result)[] judged)
            => judged.Select((j, i) => new HitEvent(i * 3 - 4, 1, j.Result, j.HitObject, null, null)).ToList();
    }
}
