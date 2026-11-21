// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneScoreLostBreakdown : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private APIScoreBreakdown? response;
        private bool requested;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("set up requests", () =>
            {
                requested = false;
                response = null;
                dummyAPI.HandleRequest = request =>
                {
                    if (request is not GetScoreBreakdownRequest breakdownRequest)
                        return false;

                    requested = true;
                    // `null` like the server answers when it can't work the maximum out.
                    breakdownRequest.TriggerSuccess(response!);
                    return true;
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestExactSplit()
        {
            AddStep("exact breakdown", () => response = createBreakdown(exact: true));
            createDisplay(onlineId: 1);
            AddUntilStep("requested", () => requested);
        }

        [Test]
        public void TestEstimatedSplit()
        {
            AddStep("estimated breakdown", () => response = createBreakdown(exact: false));
            createDisplay(onlineId: 1);
            AddUntilStep("requested", () => requested);
        }

        [Test]
        public void TestNoSplit()
        {
            AddStep("breakdown without split", () => response = new APIScoreBreakdown { ScoreId = 1, Score = 600_000, MaxScore = 1_000_000, Missing = 400_000 });
            createDisplay(onlineId: 1);
            AddUntilStep("requested", () => requested);
        }

        [Test]
        public void TestScoreNotSubmittedYet()
        {
            createDisplay(onlineId: -1);
            AddAssert("nothing requested", () => !requested);
        }

        private static APIScoreBreakdown createBreakdown(bool exact) => new APIScoreBreakdown
        {
            ScoreId = 1,
            Score = 750_000,
            MaxScore = 1_000_000,
            Missing = 250_000,
            Split = new APIScoreBreakdown.ScoreSplit { ComboLoss = 100_000, JudgementLoss = 150_000, Exact = exact },
        };

        private void createDisplay(long onlineId) => AddStep("create display", () =>
        {
            requested = false;

            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
                AutoSizeAxes = Axes.Y,
                Child = new ScoreLostBreakdown(new ScoreInfo { OnlineID = onlineId }),
            };
        });
    }
}
