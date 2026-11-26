// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// What a submitted play did, as the scosu server answers the submission (<c>scosu_outcome</c>, see scosu-rust's <c>tests/play_outcome.rs</c>),
    /// and how the results screen words it.
    /// </summary>
    [TestFixture]
    public class ScosuPlayOutcomeTest
    {
        private CultureInfo previousCulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture;

        private static string text(LocalisableString? value) => value?.ToString() ?? string.Empty;

        private const string counted_json = @"{
            ""counted"": true, ""not_counted"": [], ""mode"": 0, ""gives_ranked_score"": true,
            ""ranked_score_gain"": 1234567, ""new_best"": true,
            ""map_rank_before"": 7, ""map_rank"": 3, ""took_first_from"": null,
            ""next_on_map"": {""id"": 5, ""username"": ""PlayerY"", ""rank"": 2, ""score_needed"": 3477888},
            ""global_rank_before"": 128, ""global_rank"": 121,
            ""next_in_ranking"": {""id"": 9, ""username"": ""PlayerZ"", ""rank"": 120, ""score_needed"": 4200}
        }";

        [Test]
        public void TestTheStrainsCarryPrecisionAndSpeed()
        {
            // As scosu-rust's `BeatmapStrains` serializes them.
            var strains = JsonConvert.DeserializeObject<APIScoreStrains>(@"{
                ""stars"": 5.31, ""precision_stars"": 3.42, ""speed_stars"": 2.87, ""section_seconds"": 1.5,
                ""total"": [1.0, 2.5, 1.5], ""precision"": [0.8, 2.0, 1.0], ""speed"": [0.5, 1.2, 1.4], ""miss_times"": [2.0]
            }")!;

            Assert.That(strains.Stars, Is.EqualTo(5.31));
            Assert.That(strains.PrecisionStars, Is.EqualTo(3.42));
            Assert.That(strains.SpeedStars, Is.EqualTo(2.87));
            Assert.That(strains.SectionSeconds, Is.EqualTo(1.5));
            Assert.That(strains.Total, Is.EqualTo(new[] { 1.0, 2.5, 1.5 }));
            Assert.That(strains.Precision, Is.EqualTo(new[] { 0.8, 2.0, 1.0 }));
            Assert.That(strains.Speed, Is.EqualTo(new[] { 0.5, 1.2, 1.4 }));
            Assert.That(strains.HasSkillSplit, Is.True);

            // osu!catch and osu!mania: no split (the server sends the total three times).
            var catchStrains = JsonConvert.DeserializeObject<APIScoreStrains>(@"{
                ""stars"": 4.1, ""precision_stars"": null, ""speed_stars"": null, ""section_seconds"": 1.5,
                ""total"": [1.0, 2.0], ""precision"": [1.0, 2.0], ""speed"": [1.0, 2.0]
            }")!;

            Assert.That(catchStrains.PrecisionStars, Is.Null);
            Assert.That(catchStrains.HasSkillSplit, Is.False);
        }

        private static APIPlayOutcome parse(string json) => JsonConvert.DeserializeObject<APIPlayOutcome>(json)!;

        [Test]
        public void TestTheSubmissionAnswerCarriesTheOutcome()
        {
            var score = JsonConvert.DeserializeObject<MultiplayerScore>($@"{{""id"": 1, ""scosu_outcome"": {counted_json}}}")!;

            Assert.That(score.ScosuOutcome, Is.Not.Null);
            Assert.That(score.ScosuOutcome!.RankedScoreGain, Is.EqualTo(1234567));
            Assert.That(score.ScosuOutcome.NextOnMap!.Username, Is.EqualTo("PlayerY"));
            Assert.That(score.ScosuOutcome.NextInRanking!.ScoreNeeded, Is.EqualTo(4200));

            // Servers that don't send one (osu!'s own, older scosu).
            Assert.That(JsonConvert.DeserializeObject<MultiplayerScore>(@"{""id"": 1}")!.ScosuOutcome, Is.Null);
        }

        [Test]
        public void TestACountedPlay()
        {
            var outcome = parse(counted_json);

            Assert.That(text(ScosuPlayOutcomeFormatter.StatusTooltip(outcome)), Is.Empty);

            var place = ScosuPlayOutcomeFormatter.MapPlace(outcome)!.Value;
            Assert.That(place.Rank, Is.EqualTo(3));
            Assert.That(place.PlacesGained, Is.EqualTo(4));
            Assert.That(place.TookFirst, Is.False);
            Assert.That(text(place.Tooltip), Does.Contain("#7 before"));

            var gain = ScosuPlayOutcomeFormatter.RankedScoreGain(outcome);
            Assert.That(gain.Amount, Is.EqualTo(1234567));
            Assert.That(text(gain.Label), Is.EqualTo("+1,234,567"));
        }

        [Test]
        public void TestTheNextTargets()
        {
            var outcome = parse(counted_json);

            Assert.That(text(ScosuPlayOutcomeFormatter.FormatTarget(outcome.NextOnMap!)), Is.EqualTo("PlayerY (+3,477,888)"));
            Assert.That(text(ScosuPlayOutcomeFormatter.FormatTarget(outcome.NextInRanking!)), Is.EqualTo("PlayerZ (+4,200)"));
        }

        [Test]
        public void TestTakingFirstPlace()
        {
            var outcome = parse(counted_json);
            outcome.MapRank = 1;
            outcome.MapRankBefore = 2;
            outcome.TookFirstFrom = new APIPlayOutcome.Player { Id = 5, Username = "PlayerY" };

            var place = ScosuPlayOutcomeFormatter.MapPlace(outcome)!.Value;

            Assert.That(place.Rank, Is.EqualTo(1));
            Assert.That(place.TookFirst, Is.True);
            Assert.That(text(place.Tooltip), Does.Contain("taken from PlayerY"));
            Assert.That(place.PlacesGained, Is.EqualTo(1));
        }

        [Test]
        public void TestAPlayBelowTheBestAddsNothing()
        {
            var outcome = parse(counted_json);
            outcome.NewBest = false;
            outcome.RankedScoreGain = 0;
            outcome.MapRankBefore = outcome.MapRank;

            var gain = ScosuPlayOutcomeFormatter.RankedScoreGain(outcome);

            Assert.That(gain.Amount, Is.Zero);
            Assert.That(text(gain.Label), Is.EqualTo("Below your best"));
            // Nothing moved.
            Assert.That(ScosuPlayOutcomeFormatter.MapPlace(outcome)!.Value.PlacesGained, Is.Null);
        }

        [Test]
        public void TestAQualifiedBeatmapGivesNoRankedScoreYet()
        {
            var outcome = parse(counted_json);
            outcome.GivesRankedScore = false;

            var gain = ScosuPlayOutcomeFormatter.RankedScoreGain(outcome);

            Assert.That(gain.Amount, Is.Zero);
            Assert.That(text(gain.Tooltip), Does.Contain("qualified"));
        }

        [Test]
        public void TestRelaxHasItsOwnLeaderboard()
        {
            var outcome = parse(counted_json);
            outcome.Mode = 4;

            Assert.That(text(ScosuPlayOutcomeFormatter.StatusTooltip(outcome)), Is.EqualTo("On the Relax leaderboard"));
            Assert.That(text(ScosuPlayOutcomeFormatter.LeaderboardName(5)), Is.EqualTo("Autopilot"));
            Assert.That(ScosuPlayOutcomeFormatter.LeaderboardName(3), Is.Null);
        }

        [Test]
        public void TestAPlayThatDidntCountSaysWhy()
        {
            // As the server sends it for a failed play with HD's settings changed and WU (play_outcome.rs).
            var outcome = parse(@"{
                ""counted"": false,
                ""not_counted"": [{""code"": ""failed""}, {""code"": ""mod_settings"", ""acronym"": ""HD""}, {""code"": ""unranked_mod"", ""acronym"": ""WU""}],
                ""mode"": 0, ""gives_ranked_score"": true, ""ranked_score_gain"": 0, ""new_best"": false,
                ""map_rank_before"": null, ""map_rank"": null, ""took_first_from"": null, ""next_on_map"": null,
                ""global_rank_before"": null, ""global_rank"": null, ""next_in_ranking"": null
            }");

            string why = text(ScosuPlayOutcomeFormatter.StatusTooltip(outcome));

            Assert.That(why.Split('\n'), Is.EqualTo(new[] { "The play was failed.", "HD is only ranked with its default settings.", "WU is never ranked." }));
            Assert.That(ScosuPlayOutcomeFormatter.MapPlace(outcome), Is.Null);

            var gain = ScosuPlayOutcomeFormatter.RankedScoreGain(outcome);
            Assert.That(gain.Amount, Is.Zero);
            Assert.That(text(gain.Label), Is.EqualTo("Not counted"));
            Assert.That(text(gain.Tooltip), Is.EqualTo(why));
        }

        [Test]
        public void TestABeatmapWithoutALeaderboardNamesItsStatus()
        {
            var reason = new APIPlayOutcome.NotCountedReason { Code = "no_leaderboard", Status = "graveyard" };

            Assert.That(text(ScosuPlayOutcomeFormatter.Reason(reason)), Is.EqualTo("This beatmap has no leaderboard (Graveyard)."));

            // A reason this client doesn't know yet still says something.
            Assert.That(text(ScosuPlayOutcomeFormatter.Reason(new APIPlayOutcome.NotCountedReason { Code = "something_new" })), Is.EqualTo("something_new"));
        }

        [Test]
        public void TestPlacesGained()
        {
            Assert.That(ScosuPlayOutcomeFormatter.PlacesGained(10, 3), Is.EqualTo(7));
            Assert.That(ScosuPlayOutcomeFormatter.PlacesGained(3, 3), Is.Null);
            Assert.That(ScosuPlayOutcomeFormatter.PlacesGained(null, 3), Is.Null);
            Assert.That(ScosuPlayOutcomeFormatter.PlacesGained(3, null), Is.Null);
        }
    }
}
