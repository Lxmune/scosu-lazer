// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: what the statistics panel shows for a score without its judgements, while it gets them from the score's replay:
    /// the saved replay, or the server's, downloaded. See <see cref="ReplaySimulator"/>.
    /// </summary>
    public partial class ReplayAnalysisView : CompositeDrawable
    {
        /// <summary>
        /// Invoked with the play's judgements once the replay has been played out.
        /// </summary>
        public Action<List<HitEvent>>? Completed;

        /// <summary>
        /// Invoked with why when there's no replay to play out.
        /// </summary>
        public Action<LocalisableString>? Unavailable;

        /// <summary>
        /// How long to wait for the server's judgements once the replay has been played out.
        /// </summary>
        private const double judgement_order_timeout = 10000;

        /// <summary>
        /// How much of the replay is played back before the time left is told.
        /// </summary>
        private const double time_left_from = 0.05;

        private readonly ScoreInfo score;
        private readonly IBeatmap playableBeatmap;

        [Resolved]
        private ScoreManager scoreManager { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private ScoreModelDownloader? scoreDownloader { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private readonly Bindable<DownloadState> downloadState = new Bindable<DownloadState>();
        private readonly BindableDouble progress = new BindableDouble();

        private OsuSpriteText status = null!;
        private Box fill = null!;

        /// <summary>
        /// The order of the play's judgements as the server keeps it, or null when it doesn't (see <see cref="JudgementOrderMerge"/>).
        /// </summary>
        private readonly TaskCompletionSource<string?> judgementOrder = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool downloadRequested;
        private bool downloadStarted;
        private bool finished;

        public ReplayAnalysisView(ScoreInfo score, IBeatmap playableBeatmap)
        {
            this.score = score;
            this.playableBeatmap = playableBeatmap;

            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Icon = FontAwesome.Solid.ChartLine,
                        Size = new Vector2(28),
                        Colour = colours.Lime1,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = ScosuOutcomeStrings.AnalysingReplay,
                        Font = OsuFont.Torus.With(size: 22, weight: FontWeight.SemiBold),
                    },
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(320, 4),
                        Margin = new MarginPadding { Top = 4 },
                        Masking = true,
                        CornerRadius = 2,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = OsuColour.Gray(0.2f),
                            },
                            fill = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0,
                                Colour = colours.Lime1,
                            },
                        }
                    },
                    status = new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = ScosuOutcomeStrings.FindingReplay,
                        Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                        Colour = colours.GrayC,
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // A beatmap not downloaded has nothing to play the replay on.
            if (playableBeatmap.HitObjects.Count == 0)
            {
                unavailable(ScosuOutcomeStrings.BeatmapNeededForReplay);
                return;
            }

            fetchJudgementOrder();

            loadReplay((replayScore, beatmap) =>
            {
                if (replayScore != null)
                    simulate(replayScore, beatmap);
                else if (score.HasOnlineReplay && scoreDownloader != null && api.IsLoggedIn)
                    download();
                else
                    unavailable(ScosuOutcomeStrings.NoReplayToAnalyse);
            });
        }

        /// <summary>
        /// Gets the score's saved replay, if it has one, and the beatmap to play it on, off the update thread: reading the replay is decoding it,
        /// which takes a moment for long plays.
        /// </summary>
        /// <remarks>
        /// The beatmap is converted with the replay's own mods where they differ from the score's here: a replay from the old client is
        /// imported with Classic, which changes how its sliders are judged.
        /// </remarks>
        private void loadReplay(Action<Score?, IBeatmap> then)
        {
            Task.Run<(Score? Score, IBeatmap Beatmap)>(() =>
            {
                var replayScore = scoreManager.GetScore(score);

                if (replayScore?.Replay?.Frames.Count > 0 == false)
                    return (null, playableBeatmap);

                var replayMods = replayScore!.ScoreInfo.Mods;
                bool sameMods = replayMods.Select(m => m.Acronym).OrderBy(a => a).SequenceEqual(score.Mods.Select(m => m.Acronym).OrderBy(a => a));

                return sameMods
                    ? (replayScore, playableBeatmap)
                    : (replayScore, beatmapManager.GetWorkingBeatmap(score.BeatmapInfo).GetPlayableBeatmap(score.Ruleset, replayMods));
            }).ContinueWith(t => Schedule(() =>
            {
                (Score? replayScore, IBeatmap beatmap) = t.IsCompletedSuccessfully ? t.GetResultSafely() : (null, playableBeatmap);
                then(replayScore, beatmap);
            }));
        }

        /// <summary>
        /// Asks the server for the order of the play's judgements, while the replay is found and played out.
        /// Only servers that rank by osu!stable score keep it, for plays set with this client.
        /// </summary>
        private void fetchJudgementOrder()
        {
            if (!api.Endpoints.RanksByStableScore || !api.IsLoggedIn || score.OnlineID <= 0)
            {
                judgementOrder.SetResult(null);
                return;
            }

            var request = new GetScoreRequest(score.OnlineID);
            request.Success += info => judgementOrder.TrySetResult(string.IsNullOrEmpty(info.JudgementOrder) ? null : info.JudgementOrder);
            request.Failure += _ => judgementOrder.TrySetResult(null);
            api.Queue(request);
        }

        protected override void Update()
        {
            base.Update();

            fill.Width = (float)Interpolation.DampContinuously(fill.Width, progress.Value, 60, Time.Elapsed);
        }

        private void download()
        {
            status.Text = ScosuOutcomeStrings.DownloadingReplay;

            AddInternal(new ScoreDownloadTracker(score) { State = { BindTarget = downloadState } });

            downloadState.BindValueChanged(state =>
            {
                switch (state.NewValue)
                {
                    case DownloadState.NotDownloaded:
                        // Back to not downloaded once it started: the download failed.
                        if (downloadStarted)
                            unavailable(ScosuOutcomeStrings.ReplayDownloadFailed);
                        else if (!downloadRequested)
                        {
                            downloadRequested = true;
                            scoreDownloader!.Download(score);
                        }

                        break;

                    case DownloadState.Downloading:
                    case DownloadState.Importing:
                        downloadStarted = true;
                        break;

                    case DownloadState.LocallyAvailable:
                        loadReplay((replayScore, beatmap) =>
                        {
                            if (replayScore != null)
                                simulate(replayScore, beatmap);
                            else
                                unavailable(ScosuOutcomeStrings.ReplayDownloadFailed);
                        });
                        break;
                }
            }, true);
        }

        private void simulate(Score replayScore, IBeatmap beatmap)
        {
            if (finished)
                return;

            status.Text = ScosuOutcomeStrings.ReplayingPlay(0);

            ReplaySimulator simulator;

            try
            {
                simulator = new ReplaySimulator(replayScore, beatmap);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Couldn't replay score {score.OnlineID}");
                unavailable(ScosuOutcomeStrings.NoReplayToAnalyse);
                return;
            }

            double? startedAt = null;

            simulator.Progress.BindValueChanged(p =>
            {
                progress.Value = p.NewValue;
                startedAt ??= Time.Current;

                int percent = (int)(p.NewValue * 100);
                double elapsed = Time.Current - startedAt.Value;

                // How long the rest will take at the pace so far, once there's a pace to go by.
                if (p.NewValue >= time_left_from && p.NewValue < 1 && elapsed > 1000)
                {
                    double left = elapsed * (1 - p.NewValue) / p.NewValue;
                    status.Text = ScosuOutcomeStrings.ReplayingPlayTimeLeft(percent, (int)Math.Ceiling(left / 1000));
                }
                else
                    status.Text = ScosuOutcomeStrings.ReplayingPlay(percent);
            });

            simulator.Completed += hitEvents =>
            {
                if (ReplaySimulator.MissesDisagree(replayScore.ScoreInfo, hitEvents) is var (scored, replayed))
                {
                    Logger.Log($"Replaying score {score.OnlineID} went astray ({replayed} misses instead of {scored}): not showing what it gave");
                    unavailable(ScosuOutcomeStrings.ReplayWentAstray(scored, replayed));
                    return;
                }

                if (!judgementOrder.Task.IsCompleted)
                {
                    status.Text = ScosuOutcomeStrings.GettingJudgements;
                    // Not kept waiting on a server that doesn't answer: the replay's own judgements are shown then.
                    Scheduler.AddDelayed(() => judgementOrder.TrySetResult(null), judgement_order_timeout);
                }

                judgementOrder.Task.ContinueWith(t => Schedule(() => complete(hitEvents, t.GetResultSafely())));
            };

            LoadComponentAsync(simulator, AddInternal);
        }

        /// <summary>
        /// Hands over the replayed judgements, corrected with the play's own where the server keeps them.
        /// </summary>
        private void complete(List<HitEvent> hitEvents, string? order)
        {
            if (finished)
                return;

            if (order != null)
            {
                var merged = JudgementOrderMerge.Merge(hitEvents, order, out string outcome);
                Logger.Log($"Replaying score {score.OnlineID}: {outcome}");
                hitEvents = merged ?? hitEvents;
            }

            finished = true;
            Completed?.Invoke(hitEvents);
        }

        private void unavailable(LocalisableString reason)
        {
            if (finished)
                return;

            finished = true;
            Unavailable?.Invoke(reason);
        }
    }
}
