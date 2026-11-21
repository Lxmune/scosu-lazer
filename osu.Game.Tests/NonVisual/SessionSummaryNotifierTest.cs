// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class SessionSummaryNotifierTest
    {
        private static ScoreInfo score(ScoreRank rank = ScoreRank.A, bool fullCombo = false, double? pp = null, long totalScore = 100000, double accuracy = 0.95, string title = "Title")
            => new ScoreInfo
            {
                Rank = rank,
                MaxCombo = fullCombo ? 100 : 80,
                MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 100 },
                PP = pp,
                TotalScore = totalScore,
                Accuracy = accuracy,
                BeatmapInfo = new BeatmapInfo(new OsuRuleset().RulesetInfo, new BeatmapDifficulty(), new BeatmapMetadata { Artist = "Artist", Title = title })
                {
                    DifficultyName = "Hard"
                },
            };

        [Test]
        public void TestCounts()
        {
            var stats = new SessionSummaryNotifier.SessionStats(new[]
            {
                score(ScoreRank.XH, fullCombo: true),
                score(ScoreRank.X, fullCombo: true),
                score(ScoreRank.SH),
                score(ScoreRank.S),
                score(ScoreRank.A),
                score(ScoreRank.B),
            }, null, null);

            Assert.That(stats.Passes, Is.EqualTo(6));
            Assert.That(stats.FullCombos, Is.EqualTo(2));
            Assert.That(stats.SRanks, Is.EqualTo(4));
        }

        [Test]
        public void TestNoScores()
        {
            var stats = new SessionSummaryNotifier.SessionStats(new List<ScoreInfo>(), null, null);

            Assert.That(stats.Passes, Is.Zero);
            Assert.That(stats.Best, Is.Null);
            Assert.That(stats.Describe("osu!").ToString(), Is.EqualTo("This session: 0 passes"));
        }

        [Test]
        public void TestBestPlayByPerformance()
        {
            var low = score(pp: 100, totalScore: 900000);
            var high = score(pp: 250, totalScore: 100);

            var stats = new SessionSummaryNotifier.SessionStats(new[] { low, high }, null, null);

            Assert.That(stats.Best, Is.SameAs(high));
        }

        [Test]
        public void TestBestPlayFallsBackToScore()
        {
            var low = score(totalScore: 100);
            var high = score(totalScore: 900);

            var stats = new SessionSummaryNotifier.SessionStats(new[] { low, high }, null, null);

            Assert.That(stats.Best, Is.SameAs(high));
        }

        [Test]
        public void TestDescribeWithoutServerNumbers()
        {
            var stats = new SessionSummaryNotifier.SessionStats(new[]
            {
                score(ScoreRank.S, fullCombo: true, pp: 300, accuracy: 0.9821, title: "Best Song"),
                score(),
                score(),
            }, null, null);

            string text = stats.Describe("osu!").ToString();

            Assert.That(text, Is.EqualTo("This session: 3 passes · 1 full combo · 1 S rank. Best: Artist - Best Song [Hard], 98.21%"));
        }

        [Test]
        public void TestDescribeWithServerNumbers()
        {
            var stats = new SessionSummaryNotifier.SessionStats(new[]
            {
                score(ScoreRank.S, fullCombo: true, pp: 300, accuracy: 0.9821, title: "Best Song"),
                score(ScoreRank.SH),
                score(ScoreRank.A),
            }, 23, 12_300_000);

            string text = stats.Describe("osu!").ToString();

            Assert.That(text, Is.EqualTo("This session: 23 plays · +12.3M ranked score (osu!) · 1 full combo · 2 S ranks. Best: Artist - Best Song [Hard], 98.21%"));
        }

        [Test]
        public void TestDescribeSingularAndOmitsZeroCounts()
        {
            var stats = new SessionSummaryNotifier.SessionStats(new[] { score() }, 1, null);

            string text = stats.Describe("osu!").ToString();

            Assert.That(text, Does.StartWith("This session: 1 play."));
            Assert.That(text, Does.Not.Contain("full combo").And.Not.Contain("S rank"));
        }
    }
}
