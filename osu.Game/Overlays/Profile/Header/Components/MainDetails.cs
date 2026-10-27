// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class MainDetails : CompositeDrawable
    {
        public const float BADGE_HEIGHT = 36;

        private readonly Dictionary<ScoreRank, ScoreRankInfo> scoreRankInfos = new Dictionary<ScoreRank, ScoreRankInfo>();
        private ProfileValueDisplay medalInfo = null!;
        private ProfileValueDisplay ppInfo = null!;
        private GlobalRankDisplay detailGlobalRank = null!;
        private ProfileValueDisplay detailCountryRank = null!;
        private RankGraph rankGraph = null!;
        private RankedScoreGraph rankedScoreGraph = null!;
        private GridContainer graphs = null!;
        private ProfileGraphPanel rankGraphPanel = null!;
        private ProfileGraphPanel rankedScoreGraphPanel = null!;

        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                AutoSizeDuration = 200,
                AutoSizeEasing = Easing.OutQuint,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 15),
                Children = new Drawable[]
                {
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.Absolute, 20),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        Content = new[]
                        {
                            new[]
                            {
                                detailGlobalRank = new GlobalRankDisplay(),
                                Empty(),
                                detailCountryRank = new ProfileValueDisplay(true)
                                {
                                    Title = UsersStrings.ShowRankCountrySimple,
                                },
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    Spacing = new Vector2(20),
                                    Direction = FillDirection.Horizontal,
                                    AutoSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new MatchmakingStatsDisplay
                                        {
                                            User = { BindTarget = User }
                                        },
                                        new DailyChallengeStatsDisplay
                                        {
                                            User = { BindTarget = User },
                                        }
                                    }
                                }
                            }
                        }
                    },
                    graphs = new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 100,
                        ColumnDimensions = graphColumns(false),
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                rankGraphPanel = new ProfileGraphPanel(UsersStrings.ShowRankGlobalSimple, rankGraph = new RankGraph
                                {
                                    RelativeSizeAxes = Axes.Both,
                                })
                                {
                                    RelativeSizeAxes = Axes.Both,
                                },
                                Empty(),
                                rankedScoreGraphPanel = new ProfileGraphPanel(UsersStrings.ShowStatsRankedScore, rankedScoreGraph = new RankedScoreGraph
                                {
                                    RelativeSizeAxes = Axes.Both,
                                })
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                },
                            }
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    medalInfo = new ProfileValueDisplay
                                    {
                                        Title = UsersStrings.ShowStatsMedals,
                                    },
                                    ppInfo = new ProfileValueDisplay
                                    {
                                        Title = "pp",
                                        // Servers ranking by osu!stable score show the ranked score instead (in the extended details).
                                        Alpha = api.Endpoints.RanksByStableScore ? 0 : 1,
                                    },
                                    new TotalPlayTime
                                    {
                                        User = { BindTarget = User }
                                    },
                                }
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5),
                                Children = new[]
                                {
                                    scoreRankInfos[ScoreRank.XH] = new ScoreRankInfo(ScoreRank.XH),
                                    scoreRankInfos[ScoreRank.X] = new ScoreRankInfo(ScoreRank.X),
                                    scoreRankInfos[ScoreRank.SH] = new ScoreRankInfo(ScoreRank.SH),
                                    scoreRankInfos[ScoreRank.S] = new ScoreRankInfo(ScoreRank.S),
                                    scoreRankInfos[ScoreRank.A] = new ScoreRankInfo(ScoreRank.A),
                                }
                            }
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(e => updateDisplay(e.NewValue), true);
        }

        private void updateDisplay(UserProfileData? data)
        {
            var user = data?.User;

            medalInfo.Content.Text = user?.Achievements?.Length.ToString() ?? "0";
            ppInfo.Content.Text = user?.Statistics?.PP?.ToLocalisableString("#,##0") ?? (LocalisableString)"0";
            ppInfo.Content.TooltipText = getPPInfoTooltipText(user);

            foreach (var scoreRankInfo in scoreRankInfos)
                scoreRankInfo.Value.RankCount = user?.Statistics?.GradesCount[scoreRankInfo.Key] ?? 0;

            detailGlobalRank.HighestRank.Value = user?.RankHighest;
            detailGlobalRank.UserStatistics.Value = user?.Statistics;

            detailCountryRank.Content.Text = user?.Statistics?.CountryRank?.ToLocalisableString("\\##,##0") ?? (LocalisableString)"-";
            detailCountryRank.Content.TooltipText = getCountryRankTooltipText(user);

            rankGraph.Statistics.Value = user?.Statistics;
            rankedScoreGraph.History.Value = user?.RankedScoreHistory;

            // Servers ranking by osu!stable score rank players by ranked score. Its graph takes no room when there's nothing to show.
            bool showRankedScore = api.Endpoints.RanksByStableScore && RankedScoreGraph.HasRankedScore(user?.RankedScoreHistory);
            rankedScoreGraphPanel.Alpha = showRankedScore ? 1 : 0;
            graphs.ColumnDimensions = graphColumns(showRankedScore);

            updateRankSummary(user);
            updateRankedScoreSummary(user?.RankedScoreHistory);
        }

        /// <summary>
        /// The rank graph alone takes the whole row; with the ranked score graph, each takes half.
        /// </summary>
        private static Dimension[] graphColumns(bool showRankedScore) => showRankedScore
            ? new[] { new Dimension(), new Dimension(GridSizeMode.Absolute, 10), new Dimension() }
            : new[] { new Dimension(), new Dimension(GridSizeMode.Absolute, 0), new Dimension(GridSizeMode.Absolute, 0) };

        /// <summary>
        /// How many places the player climbed (or fell) over the graph's days.
        /// </summary>
        private void updateRankSummary(APIUser? user)
        {
            int[] ranks = user?.Statistics?.IsRanked == true ? user.Statistics.RankHistory?.Data.Where(rank => rank != 0).ToArray() ?? [] : [];

            if (ranks.Length < 2)
            {
                rankGraphPanel.SetSummary(default, colourProvider.Foreground1);
                return;
            }

            int climbed = ranks[0] - ranks[^1];

            if (climbed > 0)
                rankGraphPanel.SetSummary(climbed.ToLocalisableString(@"N0"), colours.Lime1, FontAwesome.Solid.CaretUp);
            else if (climbed < 0)
                rankGraphPanel.SetSummary((-climbed).ToLocalisableString(@"N0"), colours.Red1, FontAwesome.Solid.CaretDown);
            else
                rankGraphPanel.SetSummary(@"no change", colourProvider.Foreground1);
        }

        /// <summary>
        /// How much ranked score the player gained over the graph's days.
        /// </summary>
        private void updateRankedScoreSummary(APIRankedScoreHistory? history)
        {
            long gained = history?.Data.Length > 1 ? history.Data[^1] - history.Data[0] : 0;

            if (gained > 0)
                rankedScoreGraphPanel.SetSummary(LocalisableString.Interpolate($"+{gained.ToLocalisableString(@"N0")}"), colours.Lime1);
            else
                rankedScoreGraphPanel.SetSummary(@"no change", colourProvider.Foreground1);
        }

        private static LocalisableString getCountryRankTooltipText(APIUser? user)
        {
            var variants = user?.Statistics?.Variants;

            LocalisableString? result = null;

            if (variants?.Count > 0)
            {
                foreach (var variant in variants)
                {
                    if (variant.CountryRank != null)
                    {
                        var variantText = LocalisableString.Interpolate($"{variant.VariantType.GetLocalisableDescription()}: {variant.CountryRank.ToLocalisableString("\\##,##0")}");

                        if (result == null)
                            result = variantText;
                        else
                            result = LocalisableString.Interpolate($"{result}\n{variantText}");
                    }
                }
            }

            return result ?? default;
        }

        private static LocalisableString getPPInfoTooltipText(APIUser? user)
        {
            var variants = user?.Statistics?.Variants;

            LocalisableString? result = null;

            if (variants?.Count > 0)
            {
                foreach (var variant in variants)
                {
                    var variantText = LocalisableString.Interpolate($"{variant.VariantType.GetLocalisableDescription()}: {variant.PP.ToLocalisableString("#,##0")}");

                    if (result == null)
                        result = variantText;
                    else
                        result = LocalisableString.Interpolate($"{result}\n{variantText}");
                }
            }

            return result ?? default;
        }

        private partial class ScoreRankInfo : CompositeDrawable
        {
            private readonly OsuSpriteText rankCount;

            public int RankCount
            {
                set => rankCount.Text = value.ToLocalisableString("#,##0");
            }

            public ScoreRankInfo(ScoreRank rank)
            {
                AutoSizeAxes = Axes.Both;
                InternalChild = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    Width = 44,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new DrawableRank(rank)
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 22,
                        },
                        rankCount = new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        }
                    }
                };
            }
        }
    }
}
