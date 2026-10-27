// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Online.Leaderboards;
using osu.Game.Scoring;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class LeaderboardTotalScoreTest
    {
        // Local plays keep lazer's score as their total; the server's osu!stable score is the one shown.
        // The lazer scores here order the plays the other way round, as with a DT play against a Difficulty Adjust one.
        private static ScoreInfo[] scores() => new[]
        {
            new ScoreInfo { OnlineID = 1, TotalScore = 900_000, LegacyTotalScore = 1_561_138, Accuracy = 0.95 },
            new ScoreInfo { OnlineID = 2, TotalScore = 500_000, LegacyTotalScore = 6_653_297, Accuracy = 0.95 },
            // Never submitted: no stable score, so its own total stands.
            new ScoreInfo { OnlineID = 3, TotalScore = 2_000_000, Accuracy = 0.95 },
        };

        [Test]
        public void TestStableScoreServersRankByStableScore()
        {
            Assert.That(scores().OrderByTotalScore(ranksByStableScore: true).Select(s => s.OnlineID), Is.EqualTo(new long[] { 2, 3, 1 }));
            Assert.That(scores().OrderByCriteria(LeaderboardSortMode.Score, ranksByStableScore: true).Select(s => s.OnlineID), Is.EqualTo(new long[] { 2, 3, 1 }));
            // Ties on accuracy fall back to the same score.
            Assert.That(scores().OrderByCriteria(LeaderboardSortMode.Accuracy, ranksByStableScore: true).Select(s => s.OnlineID), Is.EqualTo(new long[] { 2, 3, 1 }));
        }

        [Test]
        public void TestOtherServersRankByTotalScore()
        {
            Assert.That(scores().OrderByTotalScore().Select(s => s.OnlineID), Is.EqualTo(new long[] { 3, 1, 2 }));
            Assert.That(scores().OrderByCriteria(LeaderboardSortMode.Score).Select(s => s.OnlineID), Is.EqualTo(new long[] { 3, 1, 2 }));
        }
    }
}
