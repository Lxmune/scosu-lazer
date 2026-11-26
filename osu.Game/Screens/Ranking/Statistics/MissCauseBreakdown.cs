// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Expanded.Statistics;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: "Why You Missed": an osu! play's misses by their likely cause (too late, aimed short, left the slider...),
    /// as the server reads them from the replay (see <see cref="APIMissAnalysis"/>).
    /// A play just set has its replay saved when the server has processed it, so until then the card waits for that.
    /// Stays hidden until the server answers, and if it can't read the replay.
    /// </summary>
    public partial class MissCauseBreakdown : CompositeDrawable
    {
        /// <summary>
        /// How long to wait for the server to say the play is processed before asking for its misses one last time.
        /// </summary>
        private const double processing_timeout = 10000;

        private readonly ScoreInfo score;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private SpectatorClient? spectatorClient { get; set; }

        private FillFlowContainer rows = null!;
        private Drawable? card;

        private GetScoreMissesRequest? request;
        private bool waitingForProcessing;
        private ScheduledDelegate? lastAttempt;

        /// <summary>
        /// Whether the server can tell <paramref name="score"/>'s misses: osu! plays it has, with misses or slider breaks.
        /// </summary>
        public static bool IsAvailable(ScoreInfo score, IAPIProvider api)
            => api.Endpoints.RanksByStableScore && api.IsLoggedIn && score.OnlineID > 0 && score.Ruleset.OnlineID == 0
               && score.Statistics.Where(s => s.Key.BreaksCombo()).Sum(s => s.Value) > 0;

        public MissCauseBreakdown(ScoreInfo score)
        {
            this.score = score;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = rows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            card = this.FindClosestParent<StatisticItemContainer>();
            card?.Hide();

            // The local user's play just submitted: listen before asking, so the moment it is processed isn't missed in between.
            waitingForProcessing = spectatorClient != null && score.ScosuOutcome != null && score.UserID == api.LocalUser.Value.Id;

            if (waitingForProcessing)
                spectatorClient!.OnUserScoreProcessed += scoreProcessed;

            fetch();
        }

        private void scoreProcessed(int userId, long scoreId)
        {
            if (scoreId != score.OnlineID)
                return;

            stopWaiting();
            fetch();
        }

        private void fetch()
        {
            request?.Cancel();

            var req = request = new GetScoreMissesRequest(score.OnlineID);
            // Not scheduled on this drawable: its card is hidden until now, and hidden drawables don't run their scheduler.
            // The API already calls back on the update thread, and a request cancelled on disposal never calls back.
            req.Success += show;
            api.Queue(req);
        }

        private void show(APIMissAnalysis? analysis)
        {
            if (analysis == null)
                return;

            rows.Clear();

            if (!analysis.Available && analysis.Reason == APIMissAnalysis.NO_REPLAY && waitingForProcessing)
            {
                rows.Add(createMessage(ScosuOutcomeStrings.WaitingForReplay));
                lastAttempt ??= Scheduler.AddDelayed(() =>
                {
                    stopWaiting();
                    fetch();
                }, processing_timeout);
            }
            else if (!analysis.Available)
                // Nothing to read the misses from: the card stays hidden rather than saying so.
                return;
            else if (analysis.Misses.Count == 0)
                rows.Add(createMessage(ScosuOutcomeStrings.NoMissesInReplay));
            else
                addCauses(analysis);

            card?.FadeIn(250, Easing.OutQuint);
            ScosuStatisticRow.Cascade(rows);
        }

        private void addCauses(APIMissAnalysis analysis)
        {
            var summary = analysis.Summary;

            // What was missed, by kind: the same three every time, so cards of different plays read alike.
            // The score's own counts, as the panel shows them: the server's reading of the replay may count a few differently.
            int sliderEnds = Math.Max(0, score.MaximumStatistics.GetValueOrDefault(HitResult.SliderTailHit) - score.Statistics.GetValueOrDefault(HitResult.SliderTailHit));

            var kinds = new ScosuTileGrid(3);
            kinds.AddTile(countTile(ScosuOutcomeStrings.TileMisses, score.Statistics.GetValueOrDefault(HitResult.Miss), colours.Red1));
            kinds.AddTile(countTile(ScosuOutcomeStrings.TileSliderBreaks, SliderBreakStatistic.Count(score.Statistics), colours.Orange1));
            kinds.AddTile(countTile(ScosuOutcomeStrings.TileSliderEnds, score.MaximumStatistics.ContainsKey(HitResult.SliderTailHit) ? sliderEnds : summary.SliderEnds, colours.Orange1));
            rows.Add(kinds);

            var causes = summary.Causes.Where(c => c.Value > 0).OrderByDescending(c => c.Value).ThenBy(c => causeOrder(c.Key)).ToList();
            int found = causes.Sum(c => c.Value);
            double total = Math.Max(1, found);

            // What the shares are of: everything the server found going wrong in the replay, slider ends included.
            rows.Add(createMessage(ScosuOutcomeStrings.CausesFoundInReplay(found)));

            // Why, as one bar split by cause, then a legend with the counts.
            var bar = new ScosuSegmentedBar { Margin = new MarginPadding { Top = 4 } };
            rows.Add(bar);

            foreach (var (cause, count) in causes)
            {
                rows.Add(new ScosuStatisticRow(causeName(cause), count.ToLocalisableString(@"N0"))
                {
                    DotColour = causeColour(cause),
                    Detail = (count / total).ToLocalisableString(@"0%"),
                    TooltipText = tooltip(cause, analysis.Misses.Where(m => m.Cause == cause).ToList()),
                });
            }

            bar.SetParts(causes.Select(c => (c.Value / total, causeColour(c.Key))));
        }

        private ScosuStatTile countTile(LocalisableString caption, int count, Color4 colour) => new ScosuStatTile(caption, count.ToLocalisableString(@"N0"))
        {
            ValueColour = count > 0 ? colour : null,
            Muted = count == 0,
        };

        /// <summary>
        /// What the cause means, and how far off the misses were where the server measured it.
        /// </summary>
        private static LocalisableString tooltip(string cause, IReadOnlyList<APIMissAnalysis.Miss> misses)
        {
            LocalisableString meaning = causeTooltip(cause);
            LocalisableString? average = null;

            var offsets = misses.Where(m => m.Offset != null).Select(m => Math.Abs(m.Offset!.Value)).ToList();
            var distances = misses.Where(m => m.Distance != null).Select(m => m.Distance!.Value).ToList();

            switch (cause)
            {
                case MissCauses.EARLY when offsets.Count > 0:
                    average = ScosuOutcomeStrings.AverageEarly(offsets.Average().ToLocalisableString(@"N0"));
                    break;

                case MissCauses.LATE when offsets.Count > 0:
                    average = ScosuOutcomeStrings.AverageLate(offsets.Average().ToLocalisableString(@"N0"));
                    break;

                case MissCauses.AIMED_SHORT or MissCauses.AIMED_LONG or MissCauses.AIMED_OFF when distances.Count > 0:
                    average = ScosuOutcomeStrings.AverageDistance(distances.Average().ToLocalisableString(@"0.0"));
                    break;
            }

            return average is LocalisableString a ? LocalisableString.Interpolate($"{meaning}\n{a}") : meaning;
        }

        private static LocalisableString causeName(string cause) => cause switch
        {
            MissCauses.EARLY => ScosuOutcomeStrings.CauseEarly,
            MissCauses.LATE => ScosuOutcomeStrings.CauseLate,
            MissCauses.AIMED_SHORT => ScosuOutcomeStrings.CauseAimedShort,
            MissCauses.AIMED_LONG => ScosuOutcomeStrings.CauseAimedLong,
            MissCauses.AIMED_OFF => ScosuOutcomeStrings.CauseAimedOff,
            MissCauses.BLOCKED => ScosuOutcomeStrings.CauseBlocked,
            MissCauses.NO_PRESS => ScosuOutcomeStrings.CauseNoPress,
            MissCauses.FOLLOW_LOST => ScosuOutcomeStrings.CauseFollowLost,
            MissCauses.KEY_RELEASED => ScosuOutcomeStrings.CauseKeyReleased,
            _ => ScosuOutcomeStrings.CauseOther,
        };

        private static LocalisableString causeTooltip(string cause) => cause switch
        {
            MissCauses.EARLY => ScosuOutcomeStrings.CauseEarlyTooltip,
            MissCauses.LATE => ScosuOutcomeStrings.CauseLateTooltip,
            MissCauses.AIMED_SHORT => ScosuOutcomeStrings.CauseAimedShortTooltip,
            MissCauses.AIMED_LONG => ScosuOutcomeStrings.CauseAimedLongTooltip,
            MissCauses.AIMED_OFF => ScosuOutcomeStrings.CauseAimedOffTooltip,
            MissCauses.BLOCKED => ScosuOutcomeStrings.CauseBlockedTooltip,
            MissCauses.NO_PRESS => ScosuOutcomeStrings.CauseNoPressTooltip,
            MissCauses.FOLLOW_LOST => ScosuOutcomeStrings.CauseFollowLostTooltip,
            MissCauses.KEY_RELEASED => ScosuOutcomeStrings.CauseKeyReleasedTooltip,
            _ => default,
        };

        /// <summary>
        /// The order of causes with as many misses: timing, then aim, then sliders.
        /// </summary>
        private static int causeOrder(string cause) => cause switch
        {
            MissCauses.EARLY => 0,
            MissCauses.LATE => 1,
            MissCauses.AIMED_SHORT => 2,
            MissCauses.AIMED_LONG => 3,
            MissCauses.AIMED_OFF => 4,
            MissCauses.BLOCKED => 5,
            MissCauses.NO_PRESS => 6,
            MissCauses.FOLLOW_LOST => 7,
            MissCauses.KEY_RELEASED => 8,
            _ => 9,
        };

        /// <summary>
        /// A colour for each kind of cause: timing, aim, the keys, sliders (in the slider break colour, as on the combo timeline).
        /// </summary>
        private Color4 causeColour(string cause) => cause switch
        {
            MissCauses.EARLY => colours.Blue0,
            MissCauses.LATE => colours.Blue2,
            // Three aim causes, three hues: shades of one colour couldn't be told apart in the bar.
            MissCauses.AIMED_SHORT => colours.Purple1,
            MissCauses.AIMED_LONG => colours.Pink1,
            MissCauses.AIMED_OFF => colours.Green1,
            MissCauses.BLOCKED or MissCauses.NO_PRESS => colours.Gray9,
            MissCauses.FOLLOW_LOST or MissCauses.KEY_RELEASED => colours.Orange1,
            _ => colours.Gray6,
        };

        private Drawable createMessage(LocalisableString text) => new OsuTextFlowContainer(s => s.Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE))
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Text = text,
            Colour = colours.GrayA,
        };

        private void stopWaiting()
        {
            lastAttempt?.Cancel();

            if (waitingForProcessing && spectatorClient != null)
                spectatorClient.OnUserScoreProcessed -= scoreProcessed;

            waitingForProcessing = false;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            stopWaiting();
            request?.Cancel();
        }
    }
}
