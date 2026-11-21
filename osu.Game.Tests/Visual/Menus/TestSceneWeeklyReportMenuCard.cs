// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.Visual.Menus
{
    public partial class TestSceneWeeklyReportMenuCard : OsuManualInputManagerTestScene
    {
        [Resolved]
        private OsuConfigManager localConfig { get; set; } = null!;

        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private WeeklyReportMenuCard card = null!;

        private List<APIWeeklyReport> reports = new List<APIWeeklyReport>();

        private Container content => card.ChildrenOfType<Container>().First(c => c != card);

        private static string lastMonday()
        {
            var today = DateTime.UtcNow.Date;
            return today.AddDays(-(((int)today.DayOfWeek + 6) % 7) - 7).ToString("yyyy-MM-dd");
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("reset seen week", () => localConfig.SetValue(OsuSetting.ScosuSeenWeeklyReport, string.Empty));
            AddStep("set reports", () => reports = new List<APIWeeklyReport>
            {
                new APIWeeklyReport
                {
                    WeekStart = lastMonday(),
                    Highlights = new List<APIWeeklyReport.Highlight>
                    {
                        new APIWeeklyReport.Highlight { Measure = "ranked_score", Mode = "osu", Change = "improved", Value = 4_200_000, Before = 1_000_000 },
                        new APIWeeklyReport.Highlight { Measure = "ur", Change = "improved", Value = 87, Before = 92 },
                    },
                },
            });
            AddStep("respond", () => dummyAPI.HandleRequest = request =>
            {
                if (request is not GetWeeklyReportsRequest reportsRequest)
                    return false;

                reportsRequest.TriggerSuccess(reports);
                return true;
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        private void createCard() => AddStep("create card", () => Child = card = new WeeklyReportMenuCard
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            State = { Value = Visibility.Visible },
        });

        [Test]
        public void TestShownWhenUnseen()
        {
            createCard();
            AddUntilStep("card shown", () => content.Alpha, () => Is.EqualTo(1));
        }

        [Test]
        public void TestDismissed()
        {
            createCard();
            AddUntilStep("card shown", () => content.Alpha, () => Is.EqualTo(1));
            AddStep("click dismiss", () =>
            {
                InputManager.MoveMouseTo(card.ChildrenOfType<osu.Game.Graphics.Containers.OsuClickableContainer>().First());
                InputManager.Click(osuTK.Input.MouseButton.Left);
            });
            AddUntilStep("card hidden", () => content.Alpha, () => Is.Zero);
            AddAssert("week remembered", () => localConfig.Get<string>(OsuSetting.ScosuSeenWeeklyReport), () => Is.EqualTo(reports[0].WeekStart));
        }

        [Test]
        public void TestHiddenWhenSeen()
        {
            AddStep("see week", () => localConfig.SetValue(OsuSetting.ScosuSeenWeeklyReport, reports[0].WeekStart));
            createCard();
            AddWaitStep("wait", 5);
            AddAssert("card hidden", () => content.Alpha, () => Is.Zero);
        }

        [Test]
        public void TestHiddenWithoutReports()
        {
            AddStep("no reports", () => reports = new List<APIWeeklyReport>());
            createCard();
            AddWaitStep("wait", 5);
            AddAssert("card hidden", () => content.Alpha, () => Is.Zero);
        }
    }
}
