// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Placeholders;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics.User;
using osuTK;
using osuTK.Input;

namespace osu.Game.Screens.Ranking.Statistics
{
    public partial class StatisticsPanel : VisibilityContainer
    {
        public const float SIDE_PADDING = 20;

        public readonly Bindable<ScoreInfo?> Score = new Bindable<ScoreInfo?>();

        /// <summary>
        /// The score which was achieved by the local user.
        /// If this is set to a non-null score, an <see cref="OverallRanking"/> component will be displayed showing changes to the local user's ranking and statistics
        /// when a statistics update related to this score is received from spectator server.
        /// </summary>
        public ScoreInfo? AchievedScore { get; init; }

        protected override bool StartHidden => true;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private StableScoreAttributesCache? stableScoreAttributesCache { get; set; }

        private readonly Container content;
        private readonly LoadingSpinner spinner;

        private bool wasOpened;
        private Sample? popInSample;
        private Sample? popOutSample;
        private CancellationTokenSource? loadCancellation;

        /// <summary>
        /// scosu: the last score whose replay couldn't be played back, and why, so it isn't tried again each time it is shown.
        /// </summary>
        private (ScoreInfo Score, LocalisableString Reason)? replayUnavailable;

        public StatisticsPanel()
        {
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Left = ScorePanel.EXPANDED_WIDTH + SIDE_PADDING * 2,
                    Right = SIDE_PADDING,
                },
                Children = new Drawable[]
                {
                    content = new Container { RelativeSizeAxes = Axes.Both },
                    spinner = new LoadingSpinner()
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            Score.BindValueChanged(populateStatistics, true);

            popInSample = audio.Samples.Get(@"Results/statistics-panel-pop-in");
            popOutSample = audio.Samples.Get(@"Results/statistics-panel-pop-out");
        }

        private void populateStatistics(ValueChangedEvent<ScoreInfo?> score)
        {
            loadCancellation?.Cancel();
            loadCancellation = null;

            foreach (var child in content)
                child.FadeOut(150).Expire();

            spinner.Hide();

            var newScore = score.NewValue;

            if (newScore == null)
                return;

            spinner.Show();

            var localCancellationSource = loadCancellation = new CancellationTokenSource();

            var workingBeatmap = beatmapManager.GetWorkingBeatmap(newScore.BeatmapInfo);

            // Todo: The placement of this is temporary. Eventually we'll both generate the playable beatmap _and_ run through it in a background task to generate the hit events.
            Task.Run(() => workingBeatmap.GetPlayableBeatmap(newScore.Ruleset, newScore.Mods), loadCancellation.Token).ContinueWith(task => Schedule(() =>
            {
                bool hitEventsAvailable = newScore.HitEvents.Count != 0;
                Container<Drawable> container;

                var playableBeatmap = task.GetResultSafely();
                var statisticItems = CreateStatisticItems(newScore, playableBeatmap).ToArray();
                bool anyRequiredHitEvents = !hitEventsAvailable && statisticItems.Any(item => item.RequiresHitEvents);

                // scosu: the statistics a score's judgements give come from its replay, played back here, rather than only after watching it.
                LocalisableString? noReplayReason = replayUnavailable?.Score.Equals(newScore) == true ? replayUnavailable.Value.Reason : (LocalisableString?)null;

                bool analysing = anyRequiredHitEvents && noReplayReason == null;

                if (!analysing && anyRequiredHitEvents && statisticItems.All(c => c.RequiresHitEvents))
                {
                    container = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new MessagePlaceholder(noReplayReason!.Value)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    };
                }
                else
                {
                    var flow = new StatisticItemFlow
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    };

                    // scosu: the cards start at the top and scroll, rather than spilling under the toolbar and the buttons when they don't fit.
                    container = new StatisticsScrollContainer
                    {
                        Clicked = ToggleVisibility,
                        RelativeSizeAxes = Axes.Both,
                        // The panel's edges are the screen's: the cards scroll out of sight under the toolbar and the buttons.
                        Masking = false,
                        ScrollbarOverlapsContent = false,
                        Alpha = 0,
                        Child = flow,
                    };

                    var shownItems = statisticItems.Where(item => hitEventsAvailable || !item.RequiresHitEvents).ToArray();

                    // While the replay is played back for the cards that need it, the others show under it.
                    if (analysing)
                        shownItems = shownItems.Prepend(createAnalysisItem(newScore, playableBeatmap)).ToArray();

                    // scosu: placed by the flow, in two columns (see StatisticItemFlow).
                    foreach (var item in shownItems)
                        flow.Add(new StatisticItemContainer(item));

                    if (anyRequiredHitEvents && !analysing)
                    {
                        flow.Add(new MessagePlaceholder(noReplayReason!.Value)
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                        });
                    }
                }

                LoadComponentAsync(container, d =>
                {
                    if (Score.Value?.Equals(newScore) != true)
                        return;

                    spinner.Hide();
                    content.Add(d);
                    d.FadeIn(250, Easing.OutQuint);
                }, localCancellationSource.Token);
            }), localCancellationSource.Token);
        }

        /// <summary>
        /// scosu: the card that plays the score's replay back, to get the judgements the other cards need (see <see cref="ReplayAnalysisView"/>).
        /// </summary>
        private StatisticItem createAnalysisItem(ScoreInfo score, IBeatmap playableBeatmap) => new StatisticItem(ScosuOutcomeStrings.ReplayAnalysisHeader, () => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 150,
            Child = new ReplayAnalysisView(score, playableBeatmap)
            {
                Completed = hitEvents =>
                {
                    score.HitEvents = hitEvents;
                    repopulate(score);
                },
                Unavailable = reason =>
                {
                    replayUnavailable = (score, reason);
                    repopulate(score);
                },
            }
        });

        /// <summary>
        /// scosu: shows <paramref name="score"/>'s statistics again, if it is still the one shown, once its replay was played back (or couldn't be).
        /// </summary>
        private void repopulate(ScoreInfo score)
        {
            if (Score.Value?.Equals(score) == true)
                populateStatistics(new ValueChangedEvent<ScoreInfo?>(score, score));
        }

        /// <summary>
        /// scosu: the cards' scrolling. Scroll containers never get clicks (a press may start a drag), so a press and release in
        /// place outside the cards closes the panel here, as a click on the panel does (see <see cref="OnClick"/>).
        /// </summary>
        private partial class StatisticsScrollContainer : OsuScrollContainer
        {
            /// <summary>
            /// How far the mouse may move between press and release for it to still be a click.
            /// </summary>
            private const float click_distance = 10;

            public Action? Clicked;

            protected override void OnMouseUp(MouseUpEvent e)
            {
                base.OnMouseUp(e);

                if (e.Button != MouseButton.Left || Vector2.Distance(e.MouseDownPosition, e.MousePosition) > click_distance)
                    return;

                if (ScrollContent.ChildrenOfType<StatisticItemContainer>().Any(card => card.ReceivePositionalInputAt(e.ScreenSpaceMousePosition)))
                    return;

                Clicked?.Invoke();
            }
        }

        /// <summary>
        /// scosu: lays the cards out in order: a full-width card across the panel, the half-width ones in two columns, each card going
        /// under the shorter column (so a short card doesn't leave a hole next to a tall one). Before a full-width card, the shorter column's
        /// last card stretches to meet the other's bottom; a half-width card that would end a run with both columns level takes the whole width.
        /// Cards not shown yet (waiting on the server) take no room.
        /// </summary>
        private partial class StatisticItemFlow : Container
        {
            protected override void UpdateAfterChildren()
            {
                base.UpdateAfterChildren();

                var shown = Children.Where(c => c.IsPresent).ToList();
                var stretched = new Dictionary<StatisticItemContainer, float>();
                float left = 0, right = 0;
                StatisticItemContainer? lastLeft = null, lastRight = null;

                void level()
                {
                    float bottom = Math.Max(left, right);

                    if (left < bottom && lastLeft != null)
                        stretched[lastLeft] = lastLeft.ContentHeight + bottom - left;
                    else if (right < bottom && lastRight != null)
                        stretched[lastRight] = lastRight.ContentHeight + bottom - right;

                    left = right = bottom;
                    lastLeft = lastRight = null;
                }

                for (int i = 0; i < shown.Count; i++)
                {
                    var child = shown[i];
                    var card = child as StatisticItemContainer;

                    bool half = card?.FullWidth == false;
                    bool endsRun = i == shown.Count - 1 || shown[i + 1] is not StatisticItemContainer { FullWidth: false };

                    if (half && endsRun && Math.Abs(left - right) < 1)
                        half = false;

                    if (!half)
                    {
                        level();
                        place(child, 0, 1, left);
                        left = right = left + height(child);
                        continue;
                    }

                    bool toLeft = left <= right + 1;
                    place(child, toLeft ? 0 : 0.5f, 0.5f, toLeft ? left : right);

                    if (toLeft)
                    {
                        left += height(child);
                        lastLeft = card;
                    }
                    else
                    {
                        right += height(child);
                        lastRight = card;
                    }
                }

                level();

                // Set once, as they are now: setting them on the way would change the cards' heights every frame.
                foreach (var card in Children.OfType<StatisticItemContainer>())
                    card.MinimumHeight = stretched.GetValueOrDefault(card);
            }

            /// <summary>
            /// A card's own height, before any stretching.
            /// </summary>
            private static float height(Drawable child) => child is StatisticItemContainer card ? card.ContentHeight + card.Padding.TotalVertical : child.DrawHeight;

            private static void place(Drawable child, float x, float width, float y)
            {
                child.Y = y;

                // Something else than a card (a message) keeps its own width, in the middle.
                if (!child.RelativeSizeAxes.HasFlag(Axes.X))
                {
                    child.Anchor = Anchor.TopCentre;
                    child.Origin = Anchor.TopCentre;
                    child.X = 0;
                    return;
                }

                child.Anchor = Anchor.TopLeft;
                child.Origin = Anchor.TopLeft;
                child.RelativePositionAxes = Axes.X;
                child.X = x;
                child.Width = width;
            }
        }

        /// <summary>
        /// Creates the <see cref="StatisticItem"/>s to be displayed in this panel for a given <paramref name="newScore"/>.
        /// </summary>
        /// <param name="newScore">The score to create the rows for.</param>
        /// <param name="playableBeatmap">The beatmap on which the score was set.</param>
        protected virtual IEnumerable<StatisticItem> CreateStatisticItems(ScoreInfo newScore, IBeatmap playableBeatmap)
        {
            // scosu: the answers first (where the play broke and why), then what it cost and where it put the player, then how it went over time,
            // the timing (the ruleset's own statistics), and the player's history on the map.
            ComboBreakAnalysis? analysis = null;
            Task? costs = null;

            // scosu: where the combo broke, and what the breaks cost (from the play's judgements, so only with them).
            if (newScore.HitEvents.Count > 0 && newScore.Ruleset.IsLegacyRuleset())
            {
                analysis = ComboBreakAnalysis.FromHitEvents(newScore.HitEvents);
                costs = ComboBreakDisplay.AnalyseStableScoreAsync(newScore, analysis, api, stableScoreAttributesCache, loadCancellation?.Token ?? CancellationToken.None);

                yield return new StatisticItem(ScosuOutcomeStrings.CostliestBreaksHeader, () => new CostliestBreaks(newScore, analysis, costs), requiresHitEvents: true, fullWidth: false);
            }

            // scosu: why the play missed, as the server reads it from the replay (next to the costliest breaks).
            if (MissCauseBreakdown.IsAvailable(newScore, api))
                yield return new StatisticItem(ScosuOutcomeStrings.WhyYouMissedHeader, () => new MissCauseBreakdown(newScore), fullWidth: false);

            // scosu: how much of the beatmap's best score the play earned, and what it lost.
            if (api.Endpoints.RanksByStableScore && api.IsLoggedIn && newScore.OnlineID > 0 && newScore.Ruleset.IsLegacyRuleset())
            {
                yield return new StatisticItem(ScosuResultsStrings.ScoreLostHeader, () => new ScoreLostBreakdown(newScore)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }, fullWidth: false);
            }

            // scosu: where a play just submitted put the player, as the server answered it.
            if (newScore.ScosuOutcome is APIPlayOutcome outcome)
                yield return new StatisticItem(ScosuOutcomeStrings.StandingHeader, () => new PlayStanding(newScore, outcome), fullWidth: false);

            if (analysis != null)
                yield return new StatisticItem(ScosuOutcomeStrings.ComboTimelineHeader, () => new ComboBreakTimeline(newScore, analysis, costs!), requiresHitEvents: true);

            foreach (var statistic in newScore.Ruleset.CreateInstance().CreateStatisticsForScore(newScore, playableBeatmap))
                yield return statistic;

            // scosu: the local user's plays on the beatmap, this one included.
            if (api.Endpoints.RanksByStableScore && api.IsLoggedIn && newScore.UserID == api.LocalUser.Value.Id && newScore.OnlineID > 0
                && newScore.Ruleset.IsLegacyRuleset())
            {
                yield return new StatisticItem(ScosuOutcomeStrings.HistoryHeader, () => new BeatmapHistorySummary(newScore), fullWidth: false);
            }

            if (AchievedScore != null
                && newScore.UserID > 1
                && newScore.UserID == AchievedScore.UserID
                && newScore.OnlineID > 0
                && newScore.OnlineID == AchievedScore.OnlineID)
            {
                yield return new StatisticItem("Overall Ranking", () => new OverallRanking(newScore)
                {
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });
            }

            // scosu: no "Beatmap tags" voting card after a play.
        }

        protected override bool OnClick(ClickEvent e)
        {
            ToggleVisibility();
            return true;
        }

        protected override void PopIn()
        {
            this.FadeIn(350, Easing.OutQuint);

            popInSample?.Play();
            wasOpened = true;
        }

        protected override void PopOut()
        {
            this.FadeOut(250, Easing.OutQuint);

            if (wasOpened)
            {
                popOutSample?.Play();
                this.HidePopover(); // targeted at the user tag control
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            loadCancellation?.Cancel();

            base.Dispose(isDisposing);
        }
    }
}
