// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ScosuResultsTest
    {
        private static string text(LocalisableString? value) => value?.ToString() ?? string.Empty;

        [Test]
        public void TestBreakdownResponse()
        {
            var breakdown = JsonConvert.DeserializeObject<APIScoreBreakdown>(
                @"{""score_id"":7,""score"":750000,""max_score"":1000000,""missing"":250000,""split"":{""combo_loss"":100000,""judgement_loss"":150000,""exact"":false}}")!;

            Assert.That(breakdown.Fraction, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(breakdown.Missing, Is.EqualTo(250000));
            Assert.That(breakdown.Split!.ComboLoss + breakdown.Split.JudgementLoss, Is.EqualTo(breakdown.Missing));
            Assert.That(breakdown.Split.Exact, Is.False);
        }

        [Test]
        public void TestBreakdownWithoutSplit()
        {
            var breakdown = JsonConvert.DeserializeObject<APIScoreBreakdown>(@"{""score_id"":7,""score"":1,""max_score"":0,""missing"":0,""split"":null}")!;

            Assert.That(breakdown.Split, Is.Null);
            Assert.That(breakdown.Fraction, Is.Zero);
        }

        [Test]
        public void TestUnavailableBreakdownIsNull()
        {
            Assert.That(JsonConvert.DeserializeObject<APIScoreBreakdown?>("null"), Is.Null);
        }

        [Test]
        public void TestPlaysLine()
        {
            var attempts = new APIBeatmapAttempts { Plays = 12, Passes = 7, PassRate = 7 / 12.0, Unsubmitted = 3 };

            Assert.That(text(ScosuBeatmapHistoryFormatter.FormatPlays(attempts)), Does.Contain("12 plays").And.Contain("58%").And.Contain("3 unfinished"));

            attempts.Unsubmitted = 0;
            Assert.That(text(ScosuBeatmapHistoryFormatter.FormatPlays(attempts)), Does.Not.Contain("unfinished"));

            Assert.That(ScosuBeatmapHistoryFormatter.FormatPlays(new APIBeatmapAttempts()), Is.Null);
        }

        [Test]
        public void TestClosestAndGainLine()
        {
            var attempts = JsonConvert.DeserializeObject<APIBeatmapAttempts>(
                @"{""beatmap_id"":1,""mode"":""osu"",""plays"":2,""passes"":1,""pass_rate"":0.5,""unsubmitted"":0,""closest"":{""score_id"":1,""score"":5,""accuracy"":0.9,""rank"":""A"",""max_combo"":350,""beatmap_max_combo"":412,""misses"":2,""full_combo"":false}}")!;
            var gain = new APIBeatmapGain { Score = 5, Gain = 120_000, Basis = "peers" };

            string line = text(ScosuBeatmapHistoryFormatter.FormatClosest(attempts, gain));
            Assert.That(line, Does.Contain("350x / 412x").And.Contain("2 misses").And.Contain("+120,000"));

            Assert.That(text(ScosuBeatmapHistoryFormatter.FormatClosest(attempts, null)), Does.Not.Contain("+"));
            Assert.That(text(ScosuBeatmapHistoryFormatter.FormatClosest(null, gain)), Does.Contain("+120,000"));
            Assert.That(ScosuBeatmapHistoryFormatter.FormatClosest(null, new APIBeatmapGain()), Is.Null);

            attempts.Closest!.FullCombo = true;
            Assert.That(text(ScosuBeatmapHistoryFormatter.FormatClosest(attempts, null)), Does.Contain("full combo"));
        }
    }
}
