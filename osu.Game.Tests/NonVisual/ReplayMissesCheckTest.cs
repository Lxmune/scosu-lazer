// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// scosu: a replay that plays back differently from its score (a full combo replayed with hundreds of misses) isn't trusted.
    /// </summary>
    [TestFixture]
    public class ReplayMissesCheckTest
    {
        [TestCase(0, 0, false)]
        [TestCase(3, 7, false)]
        [TestCase(0, 10, false)]
        [TestCase(0, 11, true)]
        [TestCase(20, 5, true)]
        public void TestShortPlay(int scoredMisses, int replayedMisses, bool disagree)
            => Assert.That(ReplaySimulator.MissesDisagree(score(300, scoredMisses), hitEvents(300, replayedMisses)) != null, Is.EqualTo(disagree));

        [Test]
        public void TestLongPlayAllowsMore()
        {
            // 2% of 4,295 objects.
            Assert.That(ReplaySimulator.MissesDisagree(score(4295, 0), hitEvents(4295, 85)), Is.Null);
            Assert.That(ReplaySimulator.MissesDisagree(score(4295, 0), hitEvents(4295, 400)), Is.EqualTo((0, 400)));
        }

        [Test]
        public void TestScoreWithoutStatisticsIsNotChecked()
            => Assert.That(ReplaySimulator.MissesDisagree(new ScoreInfo(), hitEvents(100, 100)), Is.Null);

        private static ScoreInfo score(int objects, int misses) => new ScoreInfo
        {
            Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = objects - misses,
                [HitResult.Miss] = misses,
            },
        };

        private static List<HitEvent> hitEvents(int objects, int misses)
            => Enumerable.Range(0, objects)
                         .Select(i => new HitEvent(0, 1, i < misses ? HitResult.Miss : HitResult.Great, new HitCircle { StartTime = i * 100 }, null, null))
                         .ToList();
    }
}
