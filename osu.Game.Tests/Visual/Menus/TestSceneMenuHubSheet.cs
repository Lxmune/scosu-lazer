// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Menu.Hub;
using osu.Game.Tests.Visual.Metadata;
using osu.Game.Users;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Menus
{
    public partial class TestSceneMenuHubSheet : OsuManualInputManagerTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private MenuHubSheet sheet = null!;
        private TestMetadataClient metadataClient = null!;
        private ScoreSummaryProvider scoreProvider = null!;
        private WeeklyReportProvider reportProvider = null!;
        private ScosuEventProvider eventProvider = null!;

        private Room? openedRoom;

        private static string lastMonday()
        {
            var today = DateTime.UtcNow.Date;
            return today.AddDays(-(((int)today.DayOfWeek + 6) % 7) - 7).ToString("yyyy-MM-dd");
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("go online", () => dummyAPI.SetState(APIState.Online));
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("forget opened room", () => openedRoom = null);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        // Text flows split their text into one sprite per word, so the sprites are joined back before looking.
        private bool shows(string text) => string.Concat(sheet.ChildrenOfType<OsuSpriteText>().Where(t => t.IsPresent).Select(t => t.Text.ToString())).Contains(text);

        private void createSheet() => AddStep("create sheet", () =>
        {
            metadataClient = new TestMetadataClient();
            scoreProvider = new ScoreSummaryProvider();
            reportProvider = new WeeklyReportProvider();
            eventProvider = new ScosuEventProvider();

            Children = new Drawable[]
            {
                metadataClient,
                scoreProvider,
                reportProvider,
                eventProvider,
                new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (Type, object)[]
                    {
                        (typeof(MetadataClient), metadataClient),
                        (typeof(ScoreSummaryProvider), scoreProvider),
                        (typeof(WeeklyReportProvider), reportProvider),
                        (typeof(ScosuEventProvider), eventProvider),
                    },
                    Child = new OsuContextMenuContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = sheet = new MenuHubSheet
                        {
                            Available = true,
                            OnDailyChallenge = room => openedRoom = room,
                        },
                    },
                },
            };

            sheet.Show();
        });

        [Test]
        public void TestTilesShowData()
        {
            AddStep("respond", () => dummyAPI.HandleRequest = request =>
            {
                switch (request)
                {
                    case GetScoreSummaryRequest summary:
                        summary.TriggerSuccess(new APIScoreSummary
                        {
                            Today = new APIScoreSummary.Period { RankedScore = 4_812_330, PlayCount = 23, RankStart = 45 },
                            Week = new APIScoreSummary.Period { RankedScore = 18_204_112, PlayCount = 97 },
                            Rank = 41,
                            Next = new APIScoreSummary.NextRank { Rank = 40, UserId = 7, Username = "PlayerY", RankedScoreNeeded = 1_203_442 },
                        });
                        return true;

                    case GetWeeklyReportsRequest reports:
                        reports.TriggerSuccess(new List<APIWeeklyReport>
                        {
                            new APIWeeklyReport
                            {
                                WeekStart = lastMonday(),
                                Highlights = new List<APIWeeklyReport.Highlight>
                                {
                                    new APIWeeklyReport.Highlight { Measure = "ranked_score", Change = "improved", Value = 4_200_000, Before = 1_000_000 },
                                },
                            },
                        });
                        return true;

                    case GetScosuEventRequest ev:
                        ev.TriggerSuccess(new GetScosuEventRequest.CurrentEvent
                        {
                            Event = new APIScosuEvent
                            {
                                Title = "Advent Rush 2026",
                                Tagline = "Five weeks of snow and ranked score.",
                                Status = ScosuEventStatus.Upcoming,
                                StartsAt = DateTimeOffset.Now.AddDays(7).AddHours(5),
                                EndsAt = DateTimeOffset.Now.AddDays(40),
                                Objectives = new List<APIScosuEvent.Objective> { new APIScosuEvent.Objective { Label = "Gain 200M", TargetText = "200M" } },
                            },
                        });
                        return true;

                    case GetNewsRequest news:
                        news.TriggerSuccess(new GetNewsResponse
                        {
                            NewsPosts = new[] { new APINewsPost { Title = "Welcome to scosu", Slug = "welcome", PublishedAt = DateTimeOffset.Now.AddDays(-2) } },
                        });
                        return true;

                    case GetRoomRequest room:
                        room.TriggerSuccess(new Room
                        {
                            RoomID = room.RoomId,
                            Name = "Daily challenge",
                            EndDate = DateTimeOffset.Now.AddHours(6),
                            Playlist = new[]
                            {
                                new PlaylistItem(new APIBeatmap
                                {
                                    DifficultyName = "Insane",
                                    StarRating = 5.4,
                                    BeatmapSet = new APIBeatmapSet { Title = "Innocent Sorrow", Artist = "abingdon boys school" },
                                }),
                            },
                        });
                        return true;
                }

                return false;
            });

            AddStep("friends", () =>
            {
                dummyAPI.LocalUserState.Friends.Clear();
                dummyAPI.LocalUserState.Friends.Add(new APIRelation { TargetID = 3001, TargetUser = new APIUser { Id = 3001, Username = "One" } });
                dummyAPI.LocalUserState.Friends.Add(new APIRelation { TargetID = 3002, TargetUser = new APIUser { Id = 3002, Username = "Two" } });
            });

            createSheet();

            AddStep("friend online", () => metadataClient.FriendPresenceUpdated(3001, new UserPresence { Status = UserStatus.Online }));
            AddStep("daily challenge starts", () => metadataClient.DailyChallengeUpdated(new DailyChallengeInfo { RoomID = 1234 }));

            AddUntilStep("score shown", () => shows("+4,812,330") && shows("#41"));
            AddUntilStep("next rank shown", () => shows("PlayerY"));
            AddUntilStep("event shown", () => shows("Advent Rush 2026") && shows("Gain 200M"));
            AddUntilStep("daily challenge shown", () => shows("Innocent Sorrow") && shows("Insane"));
            AddUntilStep("friends shown", () => shows("1 of 2 online"));
            AddUntilStep("report shown", () => shows("Your week is ready"));
            AddUntilStep("news shown", () => shows("Welcome to scosu"));

            AddUntilStep("sheet settled", () => sheet.ChildrenOfType<HubDailyTile>().Single().Alpha, () => Is.EqualTo(1));
            AddStep("click daily challenge", () =>
            {
                InputManager.MoveMouseTo(sheet.ChildrenOfType<HubDailyTile>().Single());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("challenge opened", () => openedRoom?.RoomID, () => Is.EqualTo(1234));
        }

        [Test]
        public void TestPlaceholdersWhenLoggedOut()
        {
            AddStep("log out", () => dummyAPI.Logout());
            AddStep("no answers", () => dummyAPI.HandleRequest = _ => false);
            createSheet();

            AddUntilStep("score placeholder", () => shows("Log in to see your score"));
            AddUntilStep("event placeholder", () => shows("There is no event right now."));
            AddUntilStep("daily challenge placeholder", () => shows("No daily challenge today"));
            AddUntilStep("friends placeholder", () => shows("Log in to see your friends"));
            AddUntilStep("report placeholder", () => shows("Log in to see your weekly report"));
            AddUntilStep("news placeholder", () => shows("No news right now"));
        }
    }
}
