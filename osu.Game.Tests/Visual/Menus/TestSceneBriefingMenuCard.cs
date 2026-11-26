// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Menus
{
    /// <summary>
    /// scosu: the login briefing's daily tasks line opens today's tasks under it.
    /// </summary>
    public partial class TestSceneBriefingMenuCard : OsuManualInputManagerTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private BriefingMenuCard card = null!;

        private Drawable tasksRow => card.ChildrenOfType<OsuClickableContainer>().Last(r => r.GetType().Name == "BriefingRow");

        private Drawable taskList => card.ChildrenOfType<Container>().Single(c => c.GetType().Name == "TaskList");

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("log in", () =>
            {
                dummyAPI.Login("PlayerX", "password");
                dummyAPI.AuthenticateSecondFactor("abcdefgh");
            });
            AddStep("respond", () => dummyAPI.HandleRequest = request =>
            {
                switch (request)
                {
                    case GetBriefingRequest briefingRequest:
                        briefingRequest.TriggerSuccess(new APIBriefing
                        {
                            Since = DateTimeOffset.Now.AddDays(-1),
                            GlobalRank = new APIBriefing.RankChange { Before = 20, Now = 15 },
                            DailyTasks = new APIBriefing.TasksSummary { CompletedCount = 1, Total = 2, DailyStreak = 3 },
                        });
                        return true;

                    case GetDailyTasksRequest tasksRequest:
                        tasksRequest.TriggerSuccess(new APIDailyTasks
                        {
                            ResetsAt = DateTimeOffset.Now.AddHours(3),
                            CompletedCount = 1,
                            Tasks = new List<APIDailyTasks.DailyTask>
                            {
                                new APIDailyTasks.DailyTask { Kind = "take_first", Target = 1, Progress = 1, Completed = true },
                                new APIDailyTasks.DailyTask { Kind = "one_miss", Target = 1 },
                            },
                        });
                        return true;
                }

                return false;
            });
            AddStep("forget the briefings given", () =>
            {
                var briefed = (HashSet<int>)typeof(BriefingMenuCard).GetField("briefed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                                                                    .GetValue(null)!;
                briefed.Clear();
            });
            AddStep("create card", () => Child = card = new BriefingMenuCard
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            }.With(c => c.Show()));
            AddUntilStep("tasks line shown", () => card.ChildrenOfType<OsuClickableContainer>().Count(r => r.GetType().Name == "BriefingRow") == 2);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestTasksOpenAndClose()
        {
            // Open from the start.
            AddUntilStep("tasks listed", () => taskList.ChildrenOfType<DailyTaskRow>().Count(), () => Is.EqualTo(2));
            AddUntilStep("list open", () => taskList.DrawHeight, () => Is.GreaterThan(40));

            clickTasks();
            AddUntilStep("list closed", () => taskList.DrawHeight, () => Is.LessThan(1));

            clickTasks();
            AddUntilStep("list open again", () => taskList.DrawHeight, () => Is.GreaterThan(40));
        }

        /// <summary>
        /// Opening the list again while it is still closing crashed the game (2026.1125.2).
        /// </summary>
        [Test]
        public void TestTogglingQuickly()
        {
            AddUntilStep("tasks listed", () => taskList.ChildrenOfType<DailyTaskRow>().Count(), () => Is.EqualTo(2));
            AddWaitStep("let it open", 5);

            // Closed and opened again twice, each before the last transition ended: open from the start, so it ends open.
            for (int i = 0; i < 4; i++)
                clickTasks();

            AddWaitStep("let the transitions run", 10);
            AddUntilStep("list open", () => taskList.DrawHeight, () => Is.GreaterThan(40));
        }

        private void clickTasks() => AddStep("click the tasks line", () =>
        {
            InputManager.MoveMouseTo(tasksRow);
            InputManager.Click(MouseButton.Left);
        });
    }
}
