// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Graphics.Sprites;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Overlays.Toolbar;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Scoring;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual.Menus
{
    [TestFixture]
    public partial class TestSceneToolbarUserButton : OsuManualInputManagerTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        public TestSceneToolbarUserButton()
        {
            Container mainContainer;

            Children = new Drawable[]
            {
                mainContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    Height = Toolbar.HEIGHT,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Black,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = Color4.DarkRed,
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 2,
                                },
                                new ToolbarUserButton(),
                                new Box
                                {
                                    Colour = Color4.DarkRed,
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 2,
                                },
                            }
                        },
                    }
                },
            };

            AddSliderStep("scale", 0.5, 4, 1, scale => mainContainer.Scale = new Vector2((float)scale));
        }

        [Test]
        public void TestLoginLogout()
        {
            AddStep("Log out", () => dummyAPI.Logout());
            AddStep("Log in", () => dummyAPI.Login("wang", "jang"));
            AddStep("Authenticate via second factor", () => dummyAPI.AuthenticateSecondFactor("abcdefgh"));
        }

        [Test]
        public void TestStates()
        {
            AddStep("Log in", () => dummyAPI.Login("wang", "jang"));
            AddStep("Authenticate via second factor", () => dummyAPI.AuthenticateSecondFactor("abcdefgh"));

            foreach (var state in Enum.GetValues<APIState>())
            {
                AddStep($"Change state to {state}", () => dummyAPI.SetState(state));
            }
        }

        [Test]
        public void TestTransientUserStatisticsDisplay()
        {
            AddStep("Log in", () => dummyAPI.Login("wang", "jang"));

            AddStep("Gain", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 123_456,
                        PP = 1234
                    },
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357
                    });
            });

            AddStep("Loss", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357
                    },
                    new UserStatistics
                    {
                        GlobalRank = 123_456,
                        PP = 1234
                    });
            });

            // Tests flooring logic works as expected.
            AddStep("Tiny increase in PP", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357.6m
                    },
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1358.1m
                    });
            });

            // cross-reference: `TestSceneOverallRanking.TestRoundingTreatment()`.
            AddStep("Test rounding treatment", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 5071.495M
                    },
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 5072.99M
                    });
            });

            AddStep("No change 1", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357m
                    },
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357.1m
                    });
            });

            AddStep("Was null", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = null,
                        PP = null
                    },
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357
                    });
            });

            AddStep("Became null", () =>
            {
                var transientUpdateDisplay = this.ChildrenOfType<TransientUserStatisticsUpdateDisplay>().Single();
                transientUpdateDisplay.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                    new ScoreInfo(),
                    new UserStatistics
                    {
                        GlobalRank = 111_111,
                        PP = 1357
                    },
                    new UserStatistics
                    {
                        GlobalRank = null,
                        PP = null
                    });
            });
        }

        private Container? stableScoreDisplayContainer;
        private TransientUserStatisticsUpdateDisplay? stableScoreDisplay;

        [Test]
        public void TestRankedScoreOnStableScoreServers()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);

            // The display decides what it shows when it loads, so it is created after the server kind is set.
            AddStep("create display", () => Add(stableScoreDisplayContainer = new Container
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                AutoSizeAxes = Axes.X,
                Height = Toolbar.HEIGHT,
                Child = stableScoreDisplay = new TransientUserStatisticsUpdateDisplay(),
            }));
            AddUntilStep("wait for display", () => stableScoreDisplay?.IsLoaded == true);

            AddStep("gain ranked score", () => stableScoreDisplay!.LatestUpdate.Value = new ScoreBasedUserStatisticsUpdate(
                new ScoreInfo(),
                new UserStatistics
                {
                    GlobalRank = 123_456,
                    PP = 1234,
                    RankedScore = 1_000_000,
                },
                new UserStatistics
                {
                    GlobalRank = 111_111,
                    PP = 1357,
                    RankedScore = 1_250_000,
                }));

            AddAssert("ranked score shown", () => statisticShown(UsersStrings.ShowStatsRankedScore));
            AddAssert("pp hidden", () => !statisticShown(RankingsStrings.StatPerformance));
            AddAssert("rank shown", () => statisticShown(UsersStrings.ShowRankGlobalSimple));
            AddUntilStep("gain shown", () => displayedTexts().Contains("+250,000"));
            AddUntilStep("ranked score rolls to its new value", () => displayedTexts().Contains("1,250,000"));
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("remove stable score display", () =>
            {
                if (stableScoreDisplayContainer != null)
                    Remove(stableScoreDisplayContainer, true);

                stableScoreDisplayContainer = null;
                stableScoreDisplay = null;
            });
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        private string[] displayedTexts() => stableScoreDisplay!.ChildrenOfType<OsuSpriteText>().Select(t => t.Text.ToString()).ToArray();

        /// <summary>
        /// Whether the statistic titled <paramref name="title"/> is shown (its title itself fades out once the change shows).
        /// </summary>
        private bool statisticShown(LocalisableString title)
        {
            var titleText = stableScoreDisplay!.ChildrenOfType<OsuSpriteText>().Single(t => t.Text.ToString() == title.ToString());

            for (Drawable? drawable = titleText.Parent; drawable != null && drawable != stableScoreDisplay; drawable = drawable.Parent)
            {
                if (drawable.Alpha == 0)
                    return false;
            }

            return true;
        }
    }
}
