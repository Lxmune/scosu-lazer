// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Database;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Models;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.Leaderboards;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Ranking.Expanded;
using osu.Game.Screens.Ranking.Expanded.Statistics;
using osu.Game.Screens.Ranking.Statistics;
using osu.Game.Tests.Resources;
using osu.Game.Tests.Visual.Spectator;
using Realms;

namespace osu.Game.Tests.Visual.Ranking
{
    /// <summary>
    /// scosu: the results screen of a play just submitted, with what the server said it did (the panel's status, place and ranked score,
    /// its score bar, and the standing) and where its combo broke.
    /// </summary>
    public partial class TestSceneScosuPlayOutcome : OsuTestScene
    {
        private const int object_count = 400;

        /// <summary>
        /// The objects missed, by index: a choke late in the map, two misses in a row, and an early one.
        /// </summary>
        private static readonly int[] misses = { 40, 41, 250, 333 };

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private OutcomeResultsScreen? screen;
        private ResultsContainer? container;
        private bool attributesRequested;

        /// <summary>
        /// Whether the server has the replay of the play yet (a play just set has it once the server has processed it).
        /// </summary>
        private bool replaySaved;

        private int missRequests;

        /// <summary>
        /// Whether the server answers for the beatmap's score attributes.
        /// </summary>
        private bool answerAttributes;

        /// <summary>
        /// Whether the server answers for the beatmap's difficulty over its length.
        /// </summary>
        private bool answerStrains;

        /// <summary>
        /// The online ID of the beatmap played, when not the test beatmap's (attributes once fetched are kept per beatmap).
        /// </summary>
        private int? beatmapOnlineID;

        /// <summary>
        /// Whether the play has slider ticks and ends (a third row of statistics) and its beatmap every line of metadata: the fullest panel.
        /// </summary>
        private bool withSliderParts;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            realm.Run(r =>
            {
                var beatmapInfo = r.All<BeatmapInfo>()
                                   .Filter($"{nameof(BeatmapInfo.Ruleset)}.{nameof(RulesetInfo.OnlineID)} = $0", 0)
                                   .FirstOrDefault();

                if (beatmapInfo != null)
                    Beatmap.Value = beatmaps.GetWorkingBeatmap(beatmapInfo);
            });
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("log in", () => dummyAPI.Login("PlayerX", "password"));
            AddStep("set up requests", () =>
            {
                attributesRequested = false;
                replaySaved = true;
                missRequests = 0;
                answerAttributes = true;
                answerStrains = true;
                beatmapOnlineID = null;
                withSliderParts = false;
                dummyAPI.HandleRequest = request =>
                {
                    switch (request)
                    {
                        case GetScoreMissesRequest missesRequest:
                            missRequests++;
                            missesRequest.TriggerSuccess(replaySaved
                                ? new APIMissAnalysis
                                {
                                    ScoreId = 1234,
                                    Available = true,
                                    Misses = new List<APIMissAnalysis.Miss>
                                    {
                                        new APIMissAnalysis.Miss { Time = 15_000, Kind = "miss", Cause = MissCauses.LATE, Offset = 140 },
                                        new APIMissAnalysis.Miss { Time = 15_350, Kind = "miss", Cause = MissCauses.NO_PRESS },
                                        new APIMissAnalysis.Miss { Time = 88_500, Kind = "miss", Cause = MissCauses.LATE, Offset = 120 },
                                        new APIMissAnalysis.Miss { Time = 117_550, Kind = "miss", Cause = MissCauses.AIMED_SHORT, Offset = 10, Distance = 1.42 },
                                    },
                                    Summary = new APIMissAnalysis.MissSummary
                                    {
                                        Misses = 4,
                                        Causes = new Dictionary<string, int> { [MissCauses.LATE] = 2, [MissCauses.NO_PRESS] = 1, [MissCauses.AIMED_SHORT] = 1 },
                                    },
                                }
                                : new APIMissAnalysis
                                {
                                    ScoreId = 1234,
                                    Reason = APIMissAnalysis.NO_REPLAY,
                                    Message = "This score has no replay, so its misses can't be analysed.",
                                });
                            return true;

                        case GetScoreStrainsRequest strainsRequest when answerStrains:
                            // Precision builds up to a hard end, speed peaks in the middle.
                            var precision = Enumerable.Range(0, 96).Select(i => 1 + Math.Sin(i / 96.0 * Math.PI * 3) * 0.4 + i / 96.0).ToList();
                            var speed = Enumerable.Range(0, 96).Select(i => 0.8 + Math.Sin(i / 96.0 * Math.PI * 2 + 1) * 0.5).ToList();

                            strainsRequest.TriggerSuccess(new APIScoreStrains
                            {
                                Stars = 5.31,
                                PrecisionStars = 3.42,
                                SpeedStars = 2.87,
                                SectionSeconds = 1.5,
                                Total = precision.Zip(speed, (p, s) => p + s).ToList(),
                                Precision = precision,
                                Speed = speed,
                            });
                            return true;

                        case GetScoresRequest scoresRequest when scoresRequest.Scope == BeatmapLeaderboardScope.Friend:
                            // The friends' leaderboard: one friend ahead, one behind.
                            scoresRequest.TriggerSuccess(new APIScoresCollection
                            {
                                ScoresCount = 3,
                                Scores = new List<SoloScoreInfo>
                                {
                                    new SoloScoreInfo { UserID = 7, User = new APIUser { Id = 7, Username = "FriendA" }, TotalScore = 40_000_000 },
                                    new SoloScoreInfo { UserID = dummyAPI.LocalUser.Value.Id, User = dummyAPI.LocalUser.Value, TotalScore = 38_402_115 },
                                    new SoloScoreInfo { UserID = 8, User = new APIUser { Id = 8, Username = "FriendB" }, TotalScore = 12_000_000 },
                                },
                            });
                            return true;

                        case GetStableScoreAttributesRequest attributesRequest:
                            attributesRequested = true;

                            if (!answerAttributes)
                                return false;

                            attributesRequest.TriggerSuccess(new StableScoreAttributes
                            {
                                AccuracyScore = 300L * object_count,
                                ComboScore = 300L * object_count * object_count / 25,
                                MaxCombo = object_count,
                                ObjectCount = object_count,
                                CircleSize = 4,
                                DrainRate = 5,
                                OverallDifficulty = 8,
                            });
                            return true;

                        case GetScoreBreakdownRequest breakdownRequest:
                            breakdownRequest.TriggerSuccess(new APIScoreBreakdown
                            {
                                ScoreId = 1234,
                                Score = 38_402_115,
                                MaxScore = 52_970_440,
                                Missing = 14_568_325,
                                Split = new APIScoreBreakdown.ScoreSplit { ComboLoss = 12_904_300, JudgementLoss = 1_664_025, Exact = true },
                            });
                            return true;

                        case GetBeatmapAttemptsRequest attemptsRequest:
                            attemptsRequest.TriggerSuccess(new APIBeatmapAttempts
                            {
                                Plays = 14,
                                Passes = 9,
                                PassRate = 9 / 14.0,
                                Closest = new APIBeatmapAttempts.ClosestAttempt { MaxCombo = 351, BeatmapMaxCombo = object_count, Misses = 1 },
                            });
                            return true;
                    }

                    return false;
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestCountedPlay()
        {
            loadResults(new APIPlayOutcome
            {
                Counted = true,
                GivesRankedScore = true,
                NewBest = true,
                RankedScoreGain = 1_234_567,
                MapRankBefore = 7,
                MapRank = 3,
                NextOnMap = new APIPlayOutcome.Target { Id = 5, Username = "PlayerY", Rank = 2, ScoreNeeded = 3_477_888 },
                GlobalRankBefore = 128,
                GlobalRank = 121,
                NextInRanking = new APIPlayOutcome.Target { Id = 9, Username = "PlayerZ", Rank = 120, ScoreNeeded = 4_200 },
            });

            AddUntilStep("status and place in the top bar", () => panelTexts<ScorePanelStatusBar>(), () => Is.SupersetOf(new[] { "COUNTED", "#3", "4" }));
            AddAssert("ranked score instead of pp", () => expandedPanel.ChildrenOfType<RankedScoreStatistic>().Count() == 1 && !expandedPanel.ChildrenOfType<PerformanceStatistic>().Any());
            AddUntilStep("ranked score counted up", () => panelTexts<RankedScoreStatistic>(), () => Does.Contain("+1,234,567"));
            AddUntilStep("score bar filled", () => expandedPanel.ChildrenOfType<ScoreShareBar>().Single().Alpha, () => Is.EqualTo(1));
            AddAssert("score bar legend", () => panelTexts<ScoreShareBar>(), () => Is.EquivalentTo(new[] { "72.5% of max", "−12.9M breaks", "−1.66M judgements" }));
            AddAssert("a mark for every break", () => expandedPanel.ChildrenOfType<ComboBreakStrip>().Single().ChildrenOfType<CompositeDrawable>()
                                                                   .Count(d => d.GetType().Name == "Mark"), () => Is.EqualTo(misses.Length));
            AddAssert("slider breaks counted", () => expandedPanel.ChildrenOfType<SliderBreakStatistic>().Count(), () => Is.EqualTo(1));

            openStatistics();

            AddUntilStep("where it broke shown", () => this.ChildrenOfType<ComboBreakTimeline>().Any());
            AddUntilStep("costliest breaks shown", () => this.ChildrenOfType<CostliestBreaks>().Any());
            AddUntilStep("standing shown", () => this.ChildrenOfType<PlayStanding>().Any());
            AddAssert("the answers first: breaks and misses, then the loss, the timeline, the timing", () => cardTitles().Take(6).ToArray(),
                () => Is.EqualTo(new[] { "Costliest Breaks", "Why You Missed", "Score Lost", "Your Standing", "Combo Timeline", "Timing Distribution" }));
            // The cards that wait for the server are hidden until it answers: they must still show once it has.
            AddUntilStep("score lost shown", () => this.ChildrenOfType<ScoreLostBreakdown>().SingleOrDefault()?.FindClosestParent<StatisticItemContainer>()?.Alpha, () => Is.EqualTo(1));
            AddUntilStep("why you missed shown", () => this.ChildrenOfType<MissCauseBreakdown>().SingleOrDefault()?.FindClosestParent<StatisticItemContainer>()?.Alpha, () => Is.EqualTo(1));
            AddUntilStep("history shown", () => this.ChildrenOfType<BeatmapHistorySummary>().SingleOrDefault()?.FindClosestParent<StatisticItemContainer>()?.Alpha, () => Is.EqualTo(1));
            AddUntilStep("break costs asked for", () => attributesRequested);
            AddUntilStep("why you missed, by cause", () => texts<MissCauseBreakdown>(), () => Is.SupersetOf(new[] { "MISSES", "4", "Too late", "2", "50%", "No press", "Aimed short" }));
            AddAssert("misses asked for once", () => missRequests, () => Is.EqualTo(1));
            AddUntilStep("score and timing lanes", () => texts<ComboBreakTimeline>(), () => Is.SupersetOf(new[] { "SCORE", "TIMING" }));
            AddAssert("no combo lane", () => texts<ComboBreakTimeline>(), () => Does.Not.Contain("COMBO ONLY"));
            // The beatmap's difficulty, said to be the map's.
            AddUntilStep("map difficulty lane", () => texts<ComboBreakTimeline>(), () => Is.SupersetOf(new[] { "MAP DIFFICULTY", "Precision", "★ 3.42", "Speed", "★ 2.87" }));
            AddAssert("said to be the map's, not the play's", () => texts<ComboBreakTimeline>(), () => Has.Some.Contains("not how you played"));
            AddAssert("full combo shown for a play above a tenth of it", () => texts<ComboBreakTimeline>(), () => Has.Some.StartsWith("full combo "));
            AddUntilStep("place among friends", () => texts<PlayStanding>(), () => Is.SupersetOf(new[] { "FRIENDS", "#2", "of 3", "FriendA (+1.6M)" }));
            AddUntilStep("costliest breaks show their cost", () => this.ChildrenOfType<CostliestBreaks>().Single().ChildrenOfType<OsuSpriteText>()
                                                                     .Count(t => t.Text.ToString().StartsWith('−')), () => Is.EqualTo(3));
        }

        [Test]
        public void TestWaitingForTheReplay()
        {
            AddStep("replay not saved yet", () => replaySaved = false);

            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = true, RankedScoreGain = 1_000, MapRank = 3, GlobalRank = 121 });
            openStatistics();

            AddUntilStep("waits for the replay", () => string.Concat(texts<MissCauseBreakdown>()), () => Is.EqualTo("Waiting for the replay…"));
            AddStep("server saves the replay", () => replaySaved = true);
            AddAssert("not asked again before it is processed", () => missRequests, () => Is.EqualTo(1));
            AddStep("server says the play is processed",
                () => ((ISpectatorClient)container!.SpectatorClient).UserScoreProcessed(dummyAPI.LocalUser.Value.Id, 1234));
            AddUntilStep("misses shown", () => texts<MissCauseBreakdown>(), () => Does.Contain("Too late"));
            AddAssert("asked again once", () => missRequests, () => Is.EqualTo(2));
        }

        [Test]
        public void TestTakingFirstPlace()
        {
            loadResults(new APIPlayOutcome
            {
                Counted = true,
                GivesRankedScore = true,
                NewBest = true,
                RankedScoreGain = 9_870_001,
                MapRankBefore = 2,
                MapRank = 1,
                TookFirstFrom = new APIPlayOutcome.Player { Id = 5, Username = "PlayerY" },
                GlobalRankBefore = 12,
                GlobalRank = 9,
                NextInRanking = new APIPlayOutcome.Target { Id = 9, Username = "PlayerZ", Rank = 8, ScoreNeeded = 120_000 },
            });

            AddUntilStep("first place in the top bar", () => panelTexts<ScorePanelStatusBar>(), () => Is.SupersetOf(new[] { "COUNTED", "#1" }));
        }

        [Test]
        public void TestPlayThatDidNotCount()
        {
            loadResults(new APIPlayOutcome
            {
                Counted = false,
                NotCounted = new[]
                {
                    new APIPlayOutcome.NotCountedReason { Code = "failed" },
                    new APIPlayOutcome.NotCountedReason { Code = "mod_settings", Acronym = "HD" },
                },
            });

            AddUntilStep("not counted, no place", () => panelTexts<ScorePanelStatusBar>(), () => Is.EquivalentTo(new[] { "NOT COUNTED" }));
            AddUntilStep("no ranked score", () => panelTexts<RankedScoreStatistic>(), () => Does.Contain("0"));

            openStatistics();

            AddUntilStep("standing says why", () => this.ChildrenOfType<PlayStanding>().Any());
        }

        [Test]
        public void TestFullCombo()
        {
            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = false, MapRank = 4, GlobalRank = 300 }, Array.Empty<int>());

            AddAssert("no break strip", () => expandedPanel.ChildrenOfType<ComboBreakStrip>().Any(), () => Is.False);

            openStatistics();

            AddUntilStep("where it broke shown", () => this.ChildrenOfType<ComboBreakTimeline>().Any());
            // Nothing broke, but the score lane still races the play against a full combo.
            AddUntilStep("score lane", () => texts<ComboBreakTimeline>(), () => Does.Contain("SCORE"));
        }

        [Test]
        public void TestEveryStatisticFitsThePanel()
        {
            AddStep("play with slider ticks and ends", () => withSliderParts = true);

            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = true, RankedScoreGain = 1_000, MapRank = 3, GlobalRank = 121 });

            AddUntilStep("score bar, break strip and three rows", () => expandedPanel.ChildrenOfType<ScoreShareBar>().Any()
                                                                       && expandedPanel.ChildrenOfType<ComboBreakStrip>().Any()
                                                                       && expandedPanel.ChildrenOfType<HitResultStatistic>().Any(s => s.Result == HitResult.LargeTickHit));
            // The panel is a fixed size: the additions must not push the last row onto the "Played on" line, or out of the panel.
            // A few pixels apart, measured on screen (the panel is drawn smaller or larger than its own size).
            AddUntilStep("last row clear of played on", () =>
            {
                var playedOn = expandedPanel.ChildrenOfType<PlayedOnText>().Single();
                float scale = playedOn.ScreenSpaceDrawQuad.Height / playedOn.DrawHeight;
                float gap = playedOn.ScreenSpaceDrawQuad.TopLeft.Y - expandedPanel.ChildrenOfType<StatisticDisplay>().Max(s => s.ScreenSpaceDrawQuad.BottomLeft.Y);
                return gap / scale;
            }, () => Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void TestComboWithoutScoreAttributes()
        {
            AddStep("server has no attributes", () =>
            {
                answerAttributes = false;
                // A beatmap whose attributes no other test has fetched (they are kept once fetched).
                beatmapOnlineID = 4243;
            });

            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = true, RankedScoreGain = 1_000, MapRank = 3, GlobalRank = 121 });
            openStatistics();

            AddUntilStep("attributes asked for", () => attributesRequested);
            AddUntilStep("combo instead of score", () => texts<ComboBreakTimeline>(), () => Is.SupersetOf(new[] { "COMBO ONLY", "TIMING" }));
            AddAssert("no score lane", () => texts<ComboBreakTimeline>(), () => Does.Not.Contain("SCORE"));
        }

        [Test]
        public void TestNoDifficultyLaneWithoutStrains()
        {
            AddStep("server has no strains", () => answerStrains = false);

            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = true, RankedScoreGain = 1_000, MapRank = 3, GlobalRank = 121 });
            openStatistics();

            AddUntilStep("score and timing lanes", () => texts<ComboBreakTimeline>(), () => Is.SupersetOf(new[] { "SCORE", "TIMING" }));
            AddAssert("no difficulty lane", () => texts<ComboBreakTimeline>(), () => Does.Not.Contain("MAP DIFFICULTY"));
        }

        /// <summary>
        /// A play breaking hundreds of times draws a few marks (long ones made the screen crawl), each telling how many breaks it stands for.
        /// </summary>
        [Test]
        public void TestManyBreaks()
        {
            int[] everyThird = Enumerable.Range(0, object_count).Where(i => i % 3 == 2).ToArray();

            loadResults(new APIPlayOutcome { Counted = true, GivesRankedScore = true, NewBest = false, MapRank = 40, GlobalRank = 300 }, everyThird);

            AddAssert("a few marks on the strip", () => expandedPanel.ChildrenOfType<ComboBreakStrip>().Single().ChildrenOfType<CompositeDrawable>()
                                                                     .Count(d => d.GetType().Name == "Mark"), () => Is.InRange(10, 41));
            AddAssert("marks tell how many breaks they stand for", () => expandedPanel.ChildrenOfType<ComboBreakStrip>().Single().ChildrenOfType<IHasTooltip>()
                                                                                       .Any(t => t.TooltipText.ToString().Contains("more breaks around it")));

            openStatistics();

            AddUntilStep("where it broke shown", () => this.ChildrenOfType<ComboBreakTimeline>().Any());
            AddAssert("a few guides", () => this.ChildrenOfType<ComboBreakTimeline>().Single().ChildrenOfType<Drawable>()
                                                .Count(d => d.GetType().Name == "DashedLine"), () => Is.LessThan(100));

            // Far below a full combo, the play is first shown against itself.
            AddUntilStep("score lane", () => texts<ComboBreakTimeline>(), () => Does.Contain("SCORE"));
            AddAssert("full combo hidden", () => texts<ComboBreakTimeline>(), () => Has.None.StartsWith("full combo "));
            AddAssert("the toggle takes clicks through the breaks' guides", () =>
            {
                var overlay = this.ChildrenOfType<Drawable>().Single(d => d.GetType().Name == "BreakOverlay");
                return overlay.ReceivePositionalInputAt(fullComboToggle.ScreenSpaceDrawQuad.Centre);
            }, () => Is.False);

            AddStep("show the full combo", () => fullComboToggle.TriggerClick());
            AddUntilStep("full combo shown", () => texts<ComboBreakTimeline>(), () => Has.Some.StartsWith("full combo "));
        }

        private OsuClickableContainer fullComboToggle => this.ChildrenOfType<OsuClickableContainer>().Single(c => c.GetType().Name == "FullComboToggle");

        [Test]
        public void TestAnotherPlayersScore()
        {
            // Without an outcome (not this player's play just submitted): no pp (the server ranks by score), the place from the leaderboard.
            AddStep("load results", () =>
            {
                var score = createScore(misses);
                score.Position = 12;
                score.HitEvents = new List<HitEvent>();

                Child = container = new ResultsContainer(screen = new OutcomeResultsScreen(score));
            });
            AddUntilStep("wait for load", () => screen?.IsLoaded == true);

            AddUntilStep("place in the top bar", () => panelTexts<ScorePanelStatusBar>(), () => Is.EquivalentTo(new[] { "#12" }));
            AddAssert("no pp on a server ranking by score", () => expandedPanel.ChildrenOfType<PerformanceStatistic>().Count(), () => Is.Zero);
            AddAssert("no break strip without judgements", () => expandedPanel.ChildrenOfType<ComboBreakStrip>().Any(), () => Is.False);

            // Its judgements come from its replay: the cards that don't need them show while it is played back.
            openStatistics();
            AddUntilStep("replay analysis first", () => cardTitles().FirstOrDefault(), () => Is.EqualTo("Replay Analysis"));
            AddAssert("score lost shown meanwhile", () => cardTitles(), () => Does.Contain("Score Lost"));
        }

        /// <summary>
        /// The statistics cards' titles, in order.
        /// </summary>
        private string[] cardTitles() => this.ChildrenOfType<StatisticItemContainer>()
                                             .Select(c => c.ChildrenOfType<StatisticItemHeader>().FirstOrDefault()?.Text.ToString() ?? string.Empty)
                                             .ToArray();

        private ScorePanel expandedPanel => screen!.ChildrenOfType<ScorePanel>().Single(p => p.State == PanelState.Expanded);

        /// <summary>
        /// The texts shown by the expanded panel's <typeparamref name="T"/>.
        /// </summary>
        private string[] panelTexts<T>() where T : Drawable
            => expandedPanel.ChildrenOfType<T>().SelectMany(d => d.ChildrenOfType<SpriteText>())
                            .Select(t => t.Text.ToString()).Where(t => t.Length > 0).ToArray();

        /// <summary>
        /// The texts shown by the statistics panel's <typeparamref name="T"/>.
        /// </summary>
        private string[] texts<T>() where T : Drawable
            => this.ChildrenOfType<T>().SelectMany(d => d.ChildrenOfType<SpriteText>())
                   .Select(t => t.Text.ToString()).Where(t => t.Length > 0).ToArray();

        private void openStatistics()
        {
            AddUntilStep("panel expanded", () => screen?.ChildrenOfType<ScorePanel>().Any(p => p.State == PanelState.Expanded) == true);
            AddStep("open statistics", () => screen!.ChildrenOfType<ScorePanel>().Single(p => p.State == PanelState.Expanded).TriggerClick());
        }

        private void loadResults(APIPlayOutcome outcome, int[]? missed = null)
        {
            AddStep("load results", () =>
            {
                var score = createScore(missed ?? misses);
                score.ScosuOutcome = outcome;

                Child = container = new ResultsContainer(screen = new OutcomeResultsScreen(score));
            });

            AddUntilStep("wait for load", () => screen?.IsLoaded == true);
        }

        private ScoreInfo createScore(int[] missed)
        {
            var score = TestResources.CreateTestScoreInfo();

            score.User = API.LocalUser.Value;
            score.OnlineID = 1234;
            score.Mods = Array.Empty<Mod>();
            // An online beatmap: break costs and the history are only asked for those.
            score.BeatmapInfo = Beatmap.Value.BeatmapInfo.Detach();
            if (beatmapOnlineID != null)
                score.BeatmapInfo.OnlineID = beatmapOnlineID.Value;
            else if (score.BeatmapInfo.OnlineID <= 0)
                score.BeatmapInfo.OnlineID = 4242;

            var hitEvents = new List<HitEvent>();
            int combo = 0, maxCombo = 0;

            for (int i = 0; i < object_count; i++)
            {
                bool miss = missed.Contains(i);
                combo = miss ? 0 : combo + 1;
                maxCombo = Math.Max(maxCombo, combo);

                var circle = new HitCircle { StartTime = 1000 + i * 350 };
                circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

                hitEvents.Add(new HitEvent(miss ? 0 : timingError(i), 1, miss ? HitResult.Miss : HitResult.Great, circle, null, null));
            }

            score.HitEvents = hitEvents;
            score.MaxCombo = maxCombo;
            score.Statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Great] = object_count - missed.Length,
                [HitResult.Miss] = missed.Length,
            };
            score.MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = object_count };

            if (withSliderParts)
            {
                score.Statistics[HitResult.LargeTickHit] = score.MaximumStatistics[HitResult.LargeTickHit] = 120;
                score.Statistics[HitResult.SliderTailHit] = score.MaximumStatistics[HitResult.SliderTailHit] = 60;

                // A beatmap with every line of metadata, and mods, which the panel has room for too.
                var metadata = new BeatmapMetadata { Title = "over the top", Artist = "xi", Author = new RealmUser { Username = "Weriko" } };
                score.BeatmapInfo.Metadata = metadata;
                foreach (var beatmap in score.BeatmapInfo.BeatmapSet?.Beatmaps ?? Enumerable.Empty<BeatmapInfo>())
                    beatmap.Metadata = metadata;
                score.BeatmapInfo.DifficultyName = "FOUR DIMENSIONS";
                score.Mods = new Mod[] { new OsuModHidden(), new OsuModDifficultyAdjust { ApproachRate = { Value = 10 } } };
            }
            score.Accuracy = (double)(object_count - missed.Length) / object_count;
            score.Rank = missed.Length == 0 ? ScoreRank.S : ScoreRank.A;
            // The osu!stable score the panel shows on servers ranking by it, as the mocked score breakdown has it.
            score.LegacyTotalScore = 38_402_115;

            return score;
        }

        /// <summary>
        /// A hit's timing error: scattered, a few hits far off, drifting late towards the end.
        /// </summary>
        private static double timingError(int index)
        {
            double noise = Math.Sin(index * 12.9898) * 43758.5453 % 1;
            return Math.Round(noise * (index % 11 == 0 ? 42 : 18) + Math.Max(0, index - 300) * 0.08);
        }

        private partial class ResultsContainer : Container
        {
            [Cached(typeof(Player))]
            public readonly Player Player = new TestPlayer();

            [Cached(typeof(SpectatorClient))]
            public readonly TestSpectatorClient SpectatorClient = new TestSpectatorClient();

            [Cached(typeof(UserStatisticsWatcher))]
            public readonly UserStatisticsWatcher UserStatisticsWatcher = new UserStatisticsWatcher(new LocalUserStatisticsProvider());

            public ResultsContainer(IScreen screen)
            {
                RelativeSizeAxes = Axes.Both;

                OsuScreenStack stack;
                InternalChildren = new Drawable[]
                {
                    SpectatorClient,
                    UserStatisticsWatcher,
                    stack = new OsuScreenStack { RelativeSizeAxes = Axes.Both },
                };
                stack.Push(screen);
            }
        }

        private partial class OutcomeResultsScreen : SoloResultsScreen
        {
            public StatisticsPanel Statistics => StatisticsPanel;

            public OutcomeResultsScreen(ScoreInfo score)
                : base(score)
            {
                AllowRetry = true;
                IsLocalPlay = true;
            }
        }
    }
}
