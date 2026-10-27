// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestSceneRankedScoreGainCounter : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private RankedScoreGainCounter counter = null!;
        private GameplayState gameplayState = null!;

        /// <summary>
        /// The play's stable score, as a watched player's client would send it.
        /// </summary>
        private readonly BindableLong stableScore = new BindableLong();

        private string? requestedLeaderboard;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("set up requests", () =>
            {
                requestedLeaderboard = null;
                dummyAPI.HandleRequest = request =>
                {
                    if (request is not GetUserBestStableScoreRequest bestRequest)
                        return false;

                    requestedLeaderboard = bestRequest.LeaderboardRuleset.ShortName;
                    bestRequest.TriggerSuccess(new APIScoresCollection
                    {
                        UserScore = new APIScoreWithPosition { Score = new SoloScoreInfo { LegacyTotalScore = 1_000_000 } },
                    });
                    return true;
                };
            });
            AddStep("reset score", () => stableScore.Value = 0);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestGainAboveBest()
        {
            createCounter(BeatmapOnlineStatus.Ranked, Array.Empty<Mod>());
            AddUntilStep("best looked up", () => requestedLeaderboard, () => Is.EqualTo("osu"));

            AddStep("score below best", () => stableScore.Value = 900_000);
            AddAssert("no gain", () => counter.Current.Value, () => Is.Zero);

            AddStep("score above best", () => stableScore.Value = 1_500_000);
            AddAssert("gain is the difference", () => counter.Current.Value, () => Is.EqualTo(500_000));
        }

        [Test]
        public void TestRelaxComparesWithRelaxBest()
        {
            createCounter(BeatmapOnlineStatus.Ranked, new Mod[] { new OsuModRelax() });
            AddUntilStep("best looked up on the Relax leaderboard", () => requestedLeaderboard, () => Is.EqualTo("osurx"));
        }

        [Test]
        public void TestNoGainWhenUnranked()
        {
            createCounter(BeatmapOnlineStatus.Ranked, new Mod[] { new OsuModDeflate() });
            AddStep("score above best", () => stableScore.Value = 1_500_000);
            AddAssert("no gain", () => counter.Current.Value, () => Is.Zero);

            createCounter(BeatmapOnlineStatus.Qualified, Array.Empty<Mod>());
            AddStep("score above best", () => stableScore.Value = 1_600_000);
            AddAssert("no gain", () => counter.Current.Value, () => Is.Zero);
            AddAssert("best not looked up", () => requestedLeaderboard, () => Is.Null);
        }

        [Test]
        public void TestNoGainUntilStableScoreIsKnown()
        {
            createCounter(BeatmapOnlineStatus.Ranked, Array.Empty<Mod>(), followStableScore: false);
            AddUntilStep("best looked up", () => requestedLeaderboard, () => Is.Not.Null);
            AddAssert("no gain", () => counter.Current.Value, () => Is.Zero);
        }

        private void createCounter(BeatmapOnlineStatus status, Mod[] mods, bool followStableScore = true)
        {
            AddStep("create counter", () =>
            {
                requestedLeaderboard = null;

                var ruleset = new OsuRuleset();
                var beatmap = new TestBeatmap(ruleset.RulesetInfo);
                beatmap.BeatmapInfo.OnlineID = 1234;
                beatmap.BeatmapInfo.Status = status;

                var score = new Score { ScoreInfo = { User = API.LocalUser.Value, Ruleset = ruleset.RulesetInfo } };
                gameplayState = new GameplayState(beatmap, ruleset, mods, score, new OsuScoreProcessor());

                if (followStableScore)
                    gameplayState.StableScore.FollowSpectatedPlayer(stableScore);

                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (Type, object)[] { (typeof(GameplayState), gameplayState) },
                    Child = counter = new RankedScoreGainCounter
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Scale = new osuTK.Vector2(2),
                    },
                };
            });
        }
    }
}
