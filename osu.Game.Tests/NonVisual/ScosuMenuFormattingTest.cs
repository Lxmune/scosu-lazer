// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ScosuMenuFormattingTest
    {
        [TestCase(0L, "0")]
        [TestCase(950L, "950")]
        [TestCase(12_300L, "12.3K")]
        [TestCase(2_400_000L, "2.4M")]
        [TestCase(2_449_999L, "2.4M")]
        [TestCase(1_200_000_000L, "1.2B")]
        [TestCase(-4_200_000L, "-4.2M")]
        public void TestCompactScore(long score, string expected) => Assert.That(ScosuMenuFormatting.CompactScore(score), Is.EqualTo(expected));

        private static APIWeeklyReport report(string week) => new APIWeeklyReport { WeekStart = week };

        [Test]
        public void TestUnseenReport()
        {
            var now = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
            var reports = new[] { report("2026-11-02"), report("2026-11-09") };

            Assert.That(ScosuMenuFormatting.FindUnseenReport(reports, string.Empty, now)?.WeekStart, Is.EqualTo("2026-11-09"));
            Assert.That(ScosuMenuFormatting.FindUnseenReport(reports, "2026-11-02", now)?.WeekStart, Is.EqualTo("2026-11-09"));
            Assert.That(ScosuMenuFormatting.FindUnseenReport(reports, "2026-11-09", now), Is.Null);
            Assert.That(ScosuMenuFormatting.FindUnseenReport(reports, "2026-12-01", now), Is.Null);
            Assert.That(ScosuMenuFormatting.FindUnseenReport(Array.Empty<APIWeeklyReport>(), string.Empty, now), Is.Null);
        }

        [Test]
        public void TestOldReportIsNotNews()
        {
            var now = new DateTimeOffset(2026, 12, 20, 0, 0, 0, TimeSpan.Zero);

            Assert.That(ScosuMenuFormatting.FindUnseenReport(new[] { report("2026-11-09") }, string.Empty, now), Is.Null);
            Assert.That(ScosuMenuFormatting.FindUnseenReport(new[] { report("not a date") }, string.Empty, now), Is.Null);
        }

        [Test]
        public void TestHighlights()
        {
            var weekly = new APIWeeklyReport
            {
                Highlights = new List<APIWeeklyReport.Highlight>
                {
                    new APIWeeklyReport.Highlight { Measure = "offset", Change = "worse", Value = -3.14, Before = -1 },
                    new APIWeeklyReport.Highlight { Measure = "ur", Change = "improved", Value = 87, Before = 92 },
                    new APIWeeklyReport.Highlight { Measure = "ranked_score", Mode = "osu", Change = "improved", Value = 4_200_000, Before = 1_000_000 },
                    new APIWeeklyReport.Highlight { Measure = "accuracy", Change = "improved", Value = 0.968, Before = 0.961 },
                },
            };

            var picked = ScosuMenuFormatting.PickHighlights(weekly);

            Assert.That(picked.Select(h => h.Measure), Is.EqualTo(new[] { "ranked_score", "ur", "accuracy" }));
            Assert.That(ScosuMenuFormatting.HighlightText(picked[0]).ToString(), Does.Contain("+4.2M"));
            Assert.That(ScosuMenuFormatting.FormatValue("accuracy", 0.968), Is.EqualTo("96.8%"));
            Assert.That(ScosuMenuFormatting.FormatValue("offset", -3.14), Is.EqualTo("-3.1ms"));
            Assert.That(ScosuMenuFormatting.FormatValue("ur", 91.6), Is.EqualTo("92"));
        }
    }
}
