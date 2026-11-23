// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class DailyTaskFormattingTest
    {
        private static APIDailyTasks.DailyTask task(string kind, long param = 0, long target = 1, long progress = 0, bool completed = false)
            => new APIDailyTasks.DailyTask { Kind = kind, Param = param, Target = target, Progress = progress, Completed = completed };

        [Test]
        public void TestDescriptions()
        {
            Assert.That(DailyTaskFormatting.Describe(task("gain_ranked_score", target: 3_000_000)).ToString(), Does.Contain("3M"));
            Assert.That(DailyTaskFormatting.Describe(task("full_combo_above", param: 400)).ToString(), Does.Contain("4★"));
            Assert.That(DailyTaskFormatting.Describe(task("rising_stars", target: 3)).ToString(), Does.Contain("3 maps"));
            Assert.That(DailyTaskFormatting.Describe(task("something_new")).ToString(), Is.EqualTo("something_new"));
        }

        [Test]
        public void TestProgress()
        {
            Assert.That(DailyTaskFormatting.Progress(task("gain_ranked_score", target: 1_000, progress: 250)), Is.EqualTo(0.25f));
            Assert.That(DailyTaskFormatting.Progress(task("gain_ranked_score", target: 1_000, progress: 5_000)), Is.EqualTo(1f));
            Assert.That(DailyTaskFormatting.Progress(task("one_miss", completed: true)), Is.EqualTo(1f));
            Assert.That(DailyTaskFormatting.Progress(task("one_miss", target: 0)), Is.EqualTo(0f));
        }

        [TestCase(15L, 12L, "+3")]
        [TestCase(12L, 1_512L, "-1,500")]
        [TestCase(12L, 12L, null)]
        [TestCase(null, 12L, null)]
        public void TestRankDelta(long? before, long? now, string? expected) => Assert.That(DailyTaskFormatting.RankDelta(before, now), Is.EqualTo(expected));

        [Test]
        public void TestNothingChanged()
        {
            var briefing = new APIBriefing
            {
                GlobalRank = new APIBriefing.RankChange { Before = 10, Now = 10 },
                RankedScore = new APIBriefing.ScoreChange { Before = 5, Now = 5 },
            };

            Assert.That(DailyTaskFormatting.ChangeLines(briefing), Is.Empty);
            // The first session has nothing to compare with.
            Assert.That(DailyTaskFormatting.ChangeLines(new APIBriefing()), Is.Empty);
        }

        [Test]
        public void TestChanges()
        {
            var briefing = new APIBriefing
            {
                GlobalRank = new APIBriefing.RankChange { Before = 20, Now = 15 },
                RankedScore = new APIBriefing.ScoreChange { Before = 1_000_000, Now = 3_400_000 },
                Snipes = new List<APIBriefing.Snipe>
                {
                    new APIBriefing.Snipe { Title = "Map A", By = new APIBriefing.PlayerRef { Id = 2, Username = "bob" } },
                    new APIBriefing.Snipe { Title = "Map B" },
                },
                SnipeCount = 5,
                UnreadMessages = 1,
            };

            var lines = DailyTaskFormatting.ChangeLines(briefing).Select(l => l.ToString()).ToList();

            Assert.That(lines, Has.Count.EqualTo(6));
            Assert.That(lines[0], Does.Contain("#15").And.Contain("+5"));
            Assert.That(lines[1], Does.Contain("+2.4M"));
            Assert.That(lines[2], Does.Contain("bob").And.Contain("Map A"));
            Assert.That(lines[3], Does.Contain("Map B"));
            Assert.That(lines[4], Does.Contain("3"));
            Assert.That(lines[5], Is.EqualTo("1 unread message"));
        }
    }
}
