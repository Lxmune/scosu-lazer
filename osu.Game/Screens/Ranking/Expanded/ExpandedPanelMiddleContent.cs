// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Ranking.Expanded.Accuracy;
using osu.Game.Screens.Ranking.Expanded.Statistics;
using osuTK;

namespace osu.Game.Screens.Ranking.Expanded
{
    /// <summary>
    /// The content that appears in the middle section of the <see cref="ScorePanel"/>.
    /// </summary>
    public partial class ExpandedPanelMiddleContent : CompositeDrawable
    {
        private const float padding = 10;

        private readonly ScoreInfo score;
        private readonly bool withFlair;

        private readonly List<StatisticDisplay> statisticDisplays = new List<StatisticDisplay>();

        private RollingCounter<long> scoreCounter = null!;

        [Resolved]
        private ScoreManager scoreManager { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        /// <summary>
        /// scosu: where the play's combo broke, when its judgements are known.
        /// </summary>
        private ComboBreakStrip? breakStrip;

        /// <summary>
        /// scosu: the accuracy circle's full height, which it gives up some of when the statistics need the room (see <see cref="UpdateAfterChildren"/>).
        /// </summary>
        private const float accuracy_circle_height = 230;

        private const float accuracy_circle_min_height = 150;

        /// <summary>
        /// scosu: the space kept between the last row of statistics and the "Played on" line.
        /// </summary>
        private const float played_on_gap = 6;

        private Container accuracyCircleContainer = null!;
        private FillFlowContainer statisticsFlow = null!;
        private Drawable? playedOn;

        /// <summary>
        /// Creates a new <see cref="ExpandedPanelMiddleContent"/>.
        /// </summary>
        /// <param name="score">The score to display.</param>
        /// <param name="withFlair">Whether to add flair for a new score being set.</param>
        public ExpandedPanelMiddleContent(ScoreInfo score, bool withFlair = false)
        {
            this.score = score;
            this.withFlair = withFlair;

            RelativeSizeAxes = Axes.Both;
            Masking = true;

            Padding = new MarginPadding(padding);
        }

        [BackgroundDependencyLoader]
        private void load(RealmAccess realmAccess, BeatmapDifficultyCache beatmapDifficultyCache)
        {
            var beatmap = score.BeatmapInfo!;
            var metadata = beatmap.BeatmapSet?.Metadata ?? beatmap.Metadata;
            string creator = metadata.Author.Username;

            StarDifficulty starDifficulty = new StarDifficulty(beatmap.StarRating, 0);

            // In some cases, the beatmap ferried through ScoreInfo actually represents an online beatmap.
            // If it isn't, we may be able to compute a more accurate difficulty from the ruleset and mods.
            if (realmAccess.Run(r => r.Find<BeatmapInfo>(score.BeatmapInfo!.ID)) != null)
                starDifficulty = beatmapDifficultyCache.GetDifficultyAsync(score.BeatmapInfo!, score.Ruleset, score.Mods).GetResultSafely() ?? starDifficulty;

            var topStatistics = new List<StatisticDisplay>
            {
                new AccuracyStatistic(score.Accuracy),
                new ComboStatistic(score.MaxCombo, score.GetMaximumAchievableCombo()),
            };

            // scosu: a play just submitted shows the ranked score it added (scosu ranks by score); its pp move to the line under the panel.
            // Other plays show no pp on a server that ranks by score: it isn't what the play is ranked by there.
            if (score.ScosuOutcome is APIPlayOutcome outcome)
                topStatistics.Add(new RankedScoreStatistic(outcome));
            else if (!api.Endpoints.RanksByStableScore)
                topStatistics.Add(new PerformanceStatistic(score));

            var bottomStatistics = new List<HitResultStatistic>();

            foreach (var result in score.GetStatisticsForDisplay())
                bottomStatistics.Add(new HitResultStatistic(result));

            var judgementStatistics = bottomStatistics.Where(s => s.Result <= HitResult.Perfect).Cast<StatisticDisplay>().ToList();

            // scosu: osu! plays count their slider breaks as a judgement, as osu!stable players do.
            if (api.Endpoints.RanksByStableScore && score.Ruleset.OnlineID == 0)
                judgementStatistics.Add(new SliderBreakStatistic(score.Statistics));

            statisticDisplays.AddRange(topStatistics);
            statisticDisplays.AddRange(judgementStatistics);
            statisticDisplays.AddRange(bottomStatistics.Where(s => s.Result > HitResult.Perfect));

            // scosu: where the combo broke, for a play whose judgements are known (one just played, or a replay just watched).
            if (score.HitEvents.Count > 0 && score.Ruleset.IsLegacyRuleset())
            {
                var analysis = ComboBreakAnalysis.FromHitEvents(score.HitEvents);

                if (analysis.Breaks.Count > 0)
                    breakStrip = new ComboBreakStrip(score, analysis);
            }

            AddInternal(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(20),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new ClickableMetadata(beatmap.OnlineID, metadata),
                            accuracyCircleContainer = new Container
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Top = 40 },
                                RelativeSizeAxes = Axes.X,
                                Height = accuracy_circle_height,
                                Child = new AccuracyCircle(score, withFlair)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    FillMode = FillMode.Fit,
                                }
                            },
                            scoreCounter = new TotalScoreCounter(!withFlair)
                            {
                                Margin = new MarginPadding { Top = 0, Bottom = 5 },
                                Current = { Value = 0 },
                                Alpha = 0,
                                AlwaysPresent = true
                            },
                            // scosu: the score against the beatmap's maximum, and what breaks and judgements cost.
                            ScoreShareBar.IsAvailable(score, api)
                                ? new ScoreShareBar(score, withFlair)
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Bottom = 8 },
                                }
                                : Empty(),
                            new FillFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(5, 0),
                                Children = new Drawable[]
                                {
                                    new StarRatingDisplay(starDifficulty)
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft
                                    },
                                    new DifficultyIcon(beatmap, score.Ruleset)
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(20),
                                        TooltipType = DifficultyIconTooltipType.Extended,
                                    },
                                    new ModDisplay
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        ExpansionMode = ExpansionMode.AlwaysExpanded,
                                        Scale = new Vector2(0.5f),
                                        Current = { Value = score.Mods }
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Direction = FillDirection.Vertical,
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new TruncatingSpriteText
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        Text = beatmap.DifficultyName,
                                        Font = OsuFont.Torus.With(size: 16, weight: FontWeight.SemiBold),
                                        MaxWidth = ScorePanel.EXPANDED_WIDTH - padding * 2,
                                    },
                                    new OsuTextFlowContainer(s => s.Font = OsuFont.Torus.With(size: 12))
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                    }.With(t =>
                                    {
                                        if (!string.IsNullOrEmpty(creator))
                                        {
                                            t.AddText("mapped by ");
                                            t.AddText(creator, s => s.Font = s.Font.With(weight: FontWeight.SemiBold));
                                        }
                                    })
                                }
                            },
                        }
                    },
                    statisticsFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5),
                        Children = new Drawable[]
                        {
                            breakStrip?.With(s => s.Margin = new MarginPadding { Bottom = 2 }) ?? Empty(),
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Content = new[] { topStatistics.Cast<Drawable>().ToArray() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                }
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Content = new[] { judgementStatistics.Cast<Drawable>().ToArray() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                }
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Content = new[] { bottomStatistics.Where(s => s.Result > HitResult.Perfect).ToArray() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                }
                            }
                        }
                    }
                }
            });

            if (score.Date != default)
            {
                if (score.ScosuOutcome == null)
                    AddInternal(playedOn = new PlayedOnText(score.Date, true));
                else
                    AddInternal(playedOn = createPlayedOnWithPerformance(beatmapDifficultyCache));
            }
        }

        /// <summary>
        /// scosu: "Played on ... · 312 pp", for a play whose ranked score took the pp's place.
        /// </summary>
        private Drawable createPlayedOnWithPerformance(BeatmapDifficultyCache difficultyCache)
        {
            OsuSpriteText performance;

            var line = new FillFlowContainer
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new PlayedOnText(score.Date, true)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    performance = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
                    },
                }
            };

            ScorePerformance.CalculateAsync(score, difficultyCache).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.GetResultSafely() is double pp)
                {
                    Schedule(() => performance.Text = LocalisableString.Interpolate(
                        $@" · {ScosuOutcomeStrings.Performance(Math.Round(pp, MidpointRounding.AwayFromZero).ToLocalisableString(@"N0"))}"));
                }
            });

            return line;
        }

        /// <summary>
        /// scosu: the panel is a fixed size, and the score bar and break strip take room that its statistics need (osu! plays with slider ticks have
        /// a third row of them). The accuracy circle gives up as much height as it takes for the last row to end above the "Played on" line,
        /// and takes it back when there is room.
        /// </summary>
        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            float statisticsBottom = statisticsFlow.ToSpaceOfOtherDrawable(new Vector2(0, statisticsFlow.DrawHeight), this).Y;
            float limit = playedOn != null
                ? playedOn.ToSpaceOfOtherDrawable(Vector2.Zero, this).Y - played_on_gap
                : DrawHeight - Padding.Bottom;

            float height = Math.Clamp(accuracyCircleContainer.Height - (statisticsBottom - limit), accuracy_circle_min_height, accuracy_circle_height);

            // Half a pixel either way is settled: the layout converges in a frame or two, before the content fades in.
            if (Math.Abs(height - accuracyCircleContainer.Height) > 0.5f)
                accuracyCircleContainer.Height = height;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Score counter value setting must be scheduled so it isn't transferred instantaneously
            ScheduleAfterChildren(() =>
            {
                using (BeginDelayedSequence(AccuracyCircle.ACCURACY_TRANSFORM_DELAY))
                {
                    scoreCounter.FadeIn();
                    scoreCounter.Current = scoreManager.GetBindableTotalScore(score);

                    double delay = 0;

                    breakStrip?.Appear();

                    foreach (var stat in statisticDisplays)
                    {
                        using (BeginDelayedSequence(delay))
                            stat.Appear();

                        delay += 200;
                    }
                }

                if (!withFlair)
                    FinishTransforms(true);
            });
        }

        internal partial class ClickableMetadata : OsuHoverContainer
        {
            [Resolved]
            private OsuGame? game { get; set; }

            public ClickableMetadata(int beatmapId, IBeatmapMetadataInfo metadata)
            {
                AutoSizeAxes = Axes.Both;

                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new TruncatingSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                            Font = OsuFont.Torus.With(size: 20, weight: FontWeight.SemiBold),
                            MaxWidth = ScorePanel.EXPANDED_WIDTH - padding * 2,
                        },
                        new TruncatingSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                            MaxWidth = ScorePanel.EXPANDED_WIDTH - padding * 2,
                        }
                    }
                };

                if (beatmapId > 0)
                    Action = () => game?.ShowBeatmap(beatmapId);
            }
        }
    }
}
