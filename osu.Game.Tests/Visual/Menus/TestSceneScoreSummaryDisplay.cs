// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.Visual.Menus
{
    public partial class TestSceneScoreSummaryDisplay : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private ScoreSummaryDisplay display = null!;

        private GetScoreSummaryRequest? lastRequest;

        // `ChildrenOfType` starts with the display itself, which is always visible here.
        private Container content => display.ChildrenOfType<Container>().First(c => c != display);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("forget last request", () => lastRequest = null);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestNothingAskedOfOtherServers()
        {
            AddStep("other server", () => dummyAPI.Endpoints.RanksByStableScore = false);
            respondWith(new APIScoreSummary { Rank = 41 });
            createDisplay();

            AddWaitStep("wait", 3);
            AddAssert("no request", () => lastRequest, () => Is.Null);
            AddAssert("content hidden", () => content.Alpha, () => Is.Zero);
        }

        [Test]
        public void TestSummaryShown()
        {
            respondWith(new APIScoreSummary
            {
                Today = new APIScoreSummary.Period
                {
                    RankedScore = 4_812_330,
                    PlayCount = 23,
                    RankStart = 45,
                    Points = new List<APIScoreSummary.Point>
                    {
                        new APIScoreSummary.Point { At = DateTimeOffset.Now.AddHours(-3), RankedScore = 120_000 },
                        new APIScoreSummary.Point { At = DateTimeOffset.Now.AddHours(-2), RankedScore = 2_400_000 },
                        new APIScoreSummary.Point { At = DateTimeOffset.Now.AddHours(-1), RankedScore = 4_812_330 },
                    },
                },
                Week = new APIScoreSummary.Period { RankedScore = 18_204_112, PlayCount = 97, RankStart = 52 },
                Rank = 41,
                Next = new APIScoreSummary.NextRank { Rank = 40, UserId = 7, Username = "PlayerY", RankedScoreNeeded = 1_203_442 },
            });
            createDisplay();

            AddAssert("requested local midnight", () => lastRequest?.Today, () => Is.EqualTo(new DateTimeOffset(DateTime.Today, TimeZoneInfo.Local.GetUtcOffset(DateTime.Today))));
            AddUntilStep("content shown", () => content.Alpha, () => Is.EqualTo(1));
            AddAssert("next rank shown", () => display.ChildrenOfType<OsuSpriteText>().Any(t => t.Text.ToString().Contains("PlayerY") && t.IsPresent));
        }

        [Test]
        public void TestNothingGainedYet()
        {
            respondWith(new APIScoreSummary());
            createDisplay();

            AddUntilStep("content shown", () => content.Alpha, () => Is.EqualTo(1));
            AddAssert("no next rank", () => display.ChildrenOfType<OsuSpriteText>().All(t => !t.Text.ToString().Contains("more to pass") || !t.IsPresent));
        }

        [Test]
        public void TestHiddenOnFailure()
        {
            AddStep("fail requests", () => dummyAPI.HandleRequest = request =>
            {
                if (request is not GetScoreSummaryRequest summaryRequest)
                    return false;

                summaryRequest.TriggerFailure(new WebException());
                return true;
            });
            createDisplay();

            AddWaitStep("wait", 5);
            AddAssert("content hidden", () => content.Alpha, () => Is.EqualTo(0));
        }

        [Test]
        public void TestHiddenWhenLoggedOut()
        {
            respondWith(new APIScoreSummary { Today = new APIScoreSummary.Period { RankedScore = 1000, PlayCount = 1 } });
            createDisplay();

            AddUntilStep("content shown", () => content.Alpha, () => Is.EqualTo(1));
            AddStep("log out", () => dummyAPI.Logout());
            AddUntilStep("content hidden", () => content.Alpha, () => Is.EqualTo(0));
            AddStep("log in", () => dummyAPI.Login("user", "password"));
            AddStep("authenticate via second factor", () => dummyAPI.AuthenticateSecondFactor("abcdefgh"));
            AddUntilStep("content shown again", () => content.Alpha, () => Is.EqualTo(1));
        }

        private void respondWith(APIScoreSummary summary) => AddStep("set up response", () => dummyAPI.HandleRequest = request =>
        {
            if (request is not GetScoreSummaryRequest summaryRequest)
                return false;

            lastRequest = summaryRequest;
            summaryRequest.TriggerSuccess(summary);
            return true;
        });

        private void createDisplay() => AddStep("create display", () =>
        {
            Child = display = new ScoreSummaryDisplay
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                State = { Value = Visibility.Visible },
            };
        });
    }
}
