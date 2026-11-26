// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: "Combo Timeline": the play over its length, in three lanes on one time axis.
    /// <list type="bullet">
    /// <item>Score: the play's osu!stable score against a full combo's, the gap between them being the score lost so far
    /// (or, without the beatmap's score attributes, the combo).</item>
    /// <item>Timing: each hit's error, early above and late below, with a rolling average.</item>
    /// <item>Difficulty: the beatmap's precision and speed over time, as the website draws them (from the server, when it answers).</item>
    /// </list>
    /// Every break is marked through all three. Hovering reads all three at one time; clicking a break watches the replay from a little before.
    /// </summary>
    public partial class ComboBreakTimeline : CompositeDrawable
    {
        private const float lane_spacing = 12;

        /// <summary>
        /// The space between a lane's edges and its graph, left and right: the guides and the time labels keep to it too.
        /// </summary>
        private const float graph_inset = 10;

        private const float score_lane_height = 130;
        private const float timing_lane_height = 84;
        private const float difficulty_lane_height = 100;

        /// <summary>
        /// Timing errors beyond this are drawn at the lane's edge, when the hit windows aren't known (it's a little past the 300's window otherwise).
        /// </summary>
        private const double fallback_timing_range = 50;

        /// <summary>
        /// How many hits the timing's rolling average spans.
        /// </summary>
        private const int timing_average_hits = 20;

        /// <summary>
        /// At most about this many breaks are drawn (marks, dots, guides): nearby ones are drawn as one (see <see cref="ComboBreakAnalysis.GroupBreaks"/>).
        /// Long plays with hundreds of breaks made the screen crawl.
        /// </summary>
        private const int max_break_marks = 60;

        /// <summary>
        /// At most this many hits are dotted on the timing lane; past it, every so many on-time hits are left out (the off-time ones always show).
        /// </summary>
        private const int max_timing_dots = 1200;

        /// <summary>
        /// At most this many points make the timing lane's average line.
        /// </summary>
        private const int max_average_points = 300;

        /// <summary>
        /// Misses are drawn on the timing lane at most once per this fraction of the play's length.
        /// </summary>
        private const int miss_columns = 400;

        /// <summary>
        /// A play scoring less than this share of a full combo is first shown against itself: next to the full combo, it'd be a flat line at the bottom.
        /// </summary>
        private const double full_combo_shown_from = 0.1;

        /// <summary>
        /// The website's colour for precision ("sky").
        /// </summary>
        private static readonly Color4 precision_colour = Color4Extensions.FromHex(@"7fb6e8");

        /// <summary>
        /// The colour for speed: lazer's teal (its statistic headers' underline), rather than the website's mint, which is too close to the play's lime line.
        /// </summary>
        private static readonly Color4 speed_colour = Color4Extensions.FromHex(@"66ffcc");

        private readonly ScoreInfo score;
        private readonly ComboBreakAnalysis analysis;
        private readonly Task costs;
        private readonly int rulesetId;

        /// <summary>
        /// The play's hits with a timing, and its misses, read up front (the score's hit events are cleared when the results screen is left).
        /// </summary>
        private readonly TimedHit[] hits;

        /// <summary>
        /// The breaks as they are drawn: nearby ones together.
        /// </summary>
        private readonly IReadOnlyList<ComboBreakAnalysis.BreakGroup> breakGroups;

        private IEnumerable<ComboBreakAnalysis.ComboBreak> shownBreaks => breakGroups.Select(g => g.Shown);

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private FillFlowContainer lanes = null!;
        private Lane scoreLane = null!;

        /// <summary>
        /// Whether the score lane shows the full combo's score along with the play's, or only the play's, scaled to fill the lane.
        /// </summary>
        private readonly BindableBool showFullCombo = new BindableBool(true);
        private BreakOverlay overlay = null!;

        /// <summary>
        /// The play's hit windows, which the timing lane draws its hits against.
        /// </summary>
        private readonly HitWindows? hitWindows;

        private GetScoreStrainsRequest? strainsRequest;

        /// <summary>
        /// How hard the beatmap is over its length, once the server answered.
        /// </summary>
        private APIScoreStrains? strains;

        private double length => Math.Max(1, analysis.EndTime - analysis.StartTime);

        public ComboBreakTimeline(ScoreInfo score, ComboBreakAnalysis analysis, Task costs)
        {
            this.score = score;
            this.analysis = analysis;
            this.costs = costs;

            rulesetId = score.Ruleset.OnlineID;
            hits = score.HitEvents.Where(e => (e.Result.IsMiss() && e.Result.IsBasic()) || HitEventExtensions.AffectsUnstableRate(e))
                        .Select(e => new TimedHit(e.HitObject.StartTime, e.TimeOffset, e.Result))
                        .OrderBy(h => h.Time)
                        .ToArray();
            hitWindows = score.HitEvents.Where(HitEventExtensions.AffectsUnstableRate).Select(e => e.HitObject.HitWindows).FirstOrDefault();
            breakGroups = analysis.GroupBreaks(max_break_marks);

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            lanes = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, lane_spacing),
                                // The score lane's caption and content wait for the score attributes (see showScoreLane).
                                Child = scoreLane = new Lane(score_lane_height),
                            },
                            overlay = new BreakOverlay(analysis, breakGroups, rulesetId, describe, () => costs.IsCompletedSuccessfully && analysis.HasCosts)
                            {
                                RelativeSizeAxes = Axes.Both,
                                // The lanes' own controls (the full combo toggle) are under it.
                                PassThrough = screenSpacePos => scoreLane.HeaderReceivesInputAt(screenSpacePos),
                                Watch = game == null ? null : comboBreak => ComboBreakDisplay.Watch(game, score, comboBreak),
                            },
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = graph_inset },
                        Children = new Drawable[]
                        {
                            timeLabel(analysis.StartTime, Anchor.TopLeft),
                            timeLabel((analysis.StartTime + analysis.EndTime) / 2, Anchor.TopCentre),
                            timeLabel(analysis.EndTime, Anchor.TopRight),
                        }
                    },
                }
            };

            if (hits.Any(h => !h.Result.IsMiss()))
                lanes.Add(createTimingLane());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            overlay.ShowGuides();

            foreach (var lane in lanes.OfType<Lane>())
                lane.Appear();

            // Whether the attributes came or not (another server, a failed request), the lane shows something.
            costs.ContinueWith(_ => Schedule(showScoreLane));

            if (api.Endpoints.RanksByStableScore && api.IsLoggedIn && score.OnlineID > 0)
            {
                var req = strainsRequest = new GetScoreStrainsRequest(score.OnlineID);
                req.Success += s => Schedule(() => showDifficultyLane(s));
                api.Queue(req);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            strainsRequest?.Cancel();
        }

        private float xAt(double time) => (float)Math.Clamp((time - analysis.StartTime) / length, 0, 1);

        private double timeAt(float x) => analysis.StartTime + x * length;

        #region Score

        /// <summary>
        /// The play's score against a full combo's, or its combo when the score isn't known.
        /// </summary>
        private void showScoreLane()
        {
            var race = costs.IsCompletedSuccessfully ? analysis.Race : null;

            if (race != null)
                showScoreRace(race);
            else
                showCombo();
        }

        private void showScoreRace(ComboBreakAnalysis.ScoreRace race)
        {
            scoreLane.SetCaption(ScosuOutcomeStrings.ScoreLane);

            showFullCombo.Value = race.Play[^1] >= race.FullCombo[^1] * full_combo_shown_from;
            scoreLane.AddLegend(new FullComboToggle { Current = showFullCombo });

            bool first = true;

            showFullCombo.BindValueChanged(_ =>
            {
                drawScoreRace(race, first);
                first = false;
            }, true);

            overlay.Race = race;
        }

        /// <param name="race">The play's score over time, and a full combo's.</param>
        /// <param name="appearing">Whether the lane is shown for the first time (rather than redrawn for <see cref="showFullCombo"/>), and plays its intro.</param>
        private void drawScoreRace(ComboBreakAnalysis.ScoreRace race, bool appearing)
        {
            scoreLane.Graph.Clear();

            bool withFullCombo = showFullCombo.Value;

            double top = Math.Max(1, withFullCombo ? Math.Max(race.FullCombo.Max(), race.Play.Max()) : race.Play.Max());
            float[] xs = race.Times.Select(xAt).ToArray();
            float[] play = race.Play.Select(v => (float)(v / top)).ToArray();
            float[] fullCombo = race.FullCombo.Select(v => (float)(v / top)).ToArray();

            long playScore = race.Play[^1];
            long maximum = race.FullCombo[^1];
            long missing = Math.Max(0, maximum - playScore);

            var plots = new List<Plot>();

            var labels = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Child = rightLabel(ScosuOutcomeStrings.ThisPlayScore(ScosuPlayOutcomeFormatter.Compact(playScore)), colours.Lime1, play[^1], above: !withFullCombo),
            };

            if (withFullCombo)
            {
                plots.Add(new PlotArea(xs, play, fullCombo, ColourInfo.GradientVertical(colours.Red1.Opacity(0.24f), colours.Red1.Opacity(0.06f))));
                plots.Add(new PlotLine(xs, fullCombo, 1.5f, dashed: true) { Colour = colours.GrayA, Alpha = 0.8f });
                labels.Add(rightLabel(ScosuOutcomeStrings.FullComboScore(ScosuPlayOutcomeFormatter.Compact(maximum)), colours.GrayA, fullCombo[^1], above: true));
            }

            // The play: a fill fading down under it, a soft glow, and the line itself.
            plots.Add(new PlotArea(xs, new float[xs.Length], play, ColourInfo.GradientVertical(colours.Lime1.Opacity(0.3f), colours.Lime1.Opacity(0.02f))));
            plots.Add(new PlotLine(xs, play, 8) { Colour = colours.Lime1, Alpha = 0.12f });
            plots.Add(new PlotLine(xs, play, 2.5f) { Colour = colours.Lime1 });

            // Where the play ended, glowing.
            labels.Add(new Circle
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.Centre,
                RelativePositionAxes = Axes.Both,
                X = xs[^1],
                Y = -play[^1],
                Size = new Vector2(8),
                Colour = colours.Lime1,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = colours.Lime1.Opacity(0.5f),
                    Radius = 8,
                },
            });

            if (withFullCombo && missing > 0)
            {
                float upper = fullCombo[^1], lower = play[^1];

                // A bracket between the two ends: what the play lost in all (in numbers, in the Score Lost card and the tooltip).
                labels.Add(new Box
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomCentre,
                    RelativePositionAxes = Axes.Y,
                    RelativeSizeAxes = Axes.Y,
                    Y = -lower,
                    Height = upper - lower,
                    Width = 2,
                    Colour = colours.Red1,
                });
            }

            var dots = shownBreaks.Select(b => breakDot(b, xAt(b.Time), (float)(race.At(b.Time).Play / top))).ToArray();

            var grid = scoreGrid(top);

            scoreLane.Graph.Add(grid);
            scoreLane.Graph.AddRange(plots);
            scoreLane.Graph.AddRange(dots);
            scoreLane.Graph.Add(labels);

            grid.FadeIn(appearing ? 600 : 200, Easing.OutQuint);

            // The lines draw in from the start of the play to its end, then the breaks drop onto them (faster when redrawn).
            double duration = appearing ? 1000 : 400;

            foreach (var plot in plots)
                plot.Animate(duration, Easing.OutCubic);

            using (BeginDelayedSequence(duration * 0.7))
                labels.FadeIn(300, Easing.OutQuint);

            popIn(dots, duration * 0.8);
        }

        /// <summary>
        /// Faint lines across the score lane at round scores, labelled on the left, up to <paramref name="top"/>.
        /// </summary>
        private Container scoreGrid(double top)
        {
            var grid = new Container { RelativeSizeAxes = Axes.Both, Alpha = 0 };

            // About three lines, at 1, 2 or 5 times a power of ten.
            double raw = top / 3;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(raw, 1))));
            double step = (raw / magnitude) switch
            {
                < 1.5 => 1,
                < 3.5 => 2,
                < 7.5 => 5,
                _ => 10,
            } * magnitude;

            for (double value = step; value < top * 0.95; value += step)
            {
                float y = (float)(value / top);

                grid.Add(new Box
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativePositionAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Y = -y,
                    Height = 1,
                    Colour = Color4.White,
                    Alpha = 0.05f,
                });

                grid.Add(new OsuSpriteText
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativePositionAxes = Axes.Y,
                    Y = -y,
                    Margin = new MarginPadding { Bottom = 1 },
                    Text = ScosuPlayOutcomeFormatter.Compact((long)value),
                    Font = OsuFont.GetFont(size: 9, weight: FontWeight.SemiBold),
                    Colour = colours.Gray7,
                });
            }

            return grid;
        }

        private void showCombo()
        {
            // Says the score itself isn't known, so the play doesn't look worth less than it is.
            scoreLane.SetCaption(ScosuOutcomeStrings.ComboOnlyLane);

            int[] columns = analysis.ComboColumns(ComboBreakDisplay.RACE_SAMPLES);
            float highest = Math.Max(1, analysis.MaxCombo);
            float[] xs = Enumerable.Range(0, columns.Length).Select(i => (i + 0.5f) / columns.Length).ToArray();
            float[] combo = columns.Select(c => c / highest).ToArray();

            var area = new PlotArea(xs, new float[xs.Length], combo, ColourInfo.GradientVertical(colours.Lime1.Opacity(0.4f), colours.Lime1.Opacity(0.06f)));
            var line = new PlotLine(xs, combo, 2) { Colour = colours.Lime1 };

            var label = new OsuSpriteText
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.BottomRight,
                Y = -4,
                Text = ScosuOutcomeStrings.MaxComboLabel(analysis.MaxCombo.ToLocalisableString(@"N0")),
                Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 1, weight: FontWeight.SemiBold),
                Colour = colours.GrayA,
                Alpha = 0,
            };

            var dots = shownBreaks.Select(b => breakDot(b, xAt(b.Time), b.ComboLost / highest)).ToArray();

            scoreLane.Graph.AddRange(new Drawable[] { area, line });
            scoreLane.Graph.AddRange(dots);
            scoreLane.Graph.Add(label);

            area.Animate(1000, Easing.OutCubic);
            line.Animate(1000, Easing.OutCubic);
            label.Delay(700).FadeIn(300, Easing.OutQuint);
            popIn(dots, 800);
        }

        private static Drawable rightLabel(LocalisableString text, Color4 colour, float y, bool above) => new OsuSpriteText
        {
            Anchor = Anchor.BottomRight,
            Origin = above ? Anchor.BottomRight : Anchor.TopRight,
            RelativePositionAxes = Axes.Y,
            Y = -y,
            Margin = new MarginPadding { Right = 8, Vertical = 4 },
            Text = text,
            Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 1, weight: FontWeight.SemiBold),
            Colour = colour,
        };

        #endregion

        #region Timing

        /// <summary>
        /// The hits' timing over the play: each hit a faint dot in its judgement's colour, against the band of the 300's window, and the timing's
        /// rolling average bold, with how spread the hits around it were (the unstable rate, locally) as a band.
        /// </summary>
        private Lane createTimingLane()
        {
            var lane = new Lane(timing_lane_height);
            lane.SetCaption(ScosuOutcomeStrings.TimingLane);

            var timed = hits.Where(h => !h.Result.IsMiss()).ToArray();

            double windowOf(HitResult result) => hitWindows?.IsHitResultAllowed(result) == true ? hitWindows.WindowFor(result) : 0;

            double great = windowOf(HitResult.Great);
            // A little past the 300's window: the 300s fill the lane, and anything worse sits at its edges, in its own colour.
            double range = great > 0 ? great * 1.25 : fallback_timing_range;

            // Early above the middle, late below.
            float heightOf(double offset) => 0.5f + (float)(Math.Clamp(-offset / range, -1, 1) * 0.45);

            if (great > 0)
            {
                lane.Graph.Add(new Box
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Height = (float)(great / range * 0.9),
                    Colour = colours.ForHitResult(HitResult.Great),
                    Alpha = 0.08f,
                });
            }

            lane.Graph.Add(new Box
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                RelativeSizeAxes = Axes.X,
                Height = 1,
                Colour = Color4.White,
                Alpha = 0.1f,
            });

            var dots = new Container { RelativeSizeAxes = Axes.Both, Alpha = 0 };

            // Plain squares rather than circles (each a masked container), which look the same this small and cost far less.
            // Past a few thousand, every so many of the best hits are left out: the others always show.
            int stride = (int)Math.Ceiling((double)timed.Length / max_timing_dots);

            for (int i = 0; i < timed.Length; i++)
            {
                var hit = timed[i];
                bool best = hit.Result == HitResult.Great || hit.Result == HitResult.Perfect;

                if (best && i % stride != 0)
                    continue;

                dots.Add(new Box
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.Centre,
                    RelativePositionAxes = Axes.Both,
                    X = xAt(hit.Time),
                    Y = -heightOf(hit.Offset),
                    Size = new Vector2(2.2f),
                    Colour = colours.ForHitResult(hit.Result),
                    // The dots are the background: the average says it, the worse hits stand out.
                    Alpha = best ? 0.35f : 0.8f,
                });
            }

            // The rolling average, and the hits' spread around it, at a few hundred points at most.
            int averageStride = (int)Math.Ceiling((double)timed.Length / max_average_points);
            int[] averaged = Enumerable.Range(0, timed.Length).Where(i => i % averageStride == 0 || i == timed.Length - 1).ToArray();

            float[] averageXs = averaged.Select(i => xAt(timed[i].Time)).ToArray();
            var windows = averaged.Select(i =>
            {
                int from = Math.Max(0, i - timing_average_hits / 2);
                int to = Math.Min(timed.Length, from + timing_average_hits);
                double[] offsets = timed[from..to].Select(h => h.Offset).ToArray();
                double mean = offsets.Average();
                double deviation = Math.Sqrt(offsets.Average(o => (o - mean) * (o - mean)));
                return (Mean: mean, Deviation: deviation);
            }).ToArray();

            float[] average = windows.Select(w => heightOf(w.Mean)).ToArray();
            float[] spreadTop = windows.Select(w => heightOf(w.Mean - w.Deviation)).ToArray();
            float[] spreadBottom = windows.Select(w => heightOf(w.Mean + w.Deviation)).ToArray();

            var spread = new PlotArea(averageXs, spreadBottom, spreadTop, Color4.White.Opacity(0.1f));
            var averageLine = new PlotLine(averageXs, average, 2.2f) { Colour = Color4.White, Alpha = 0.9f };

            lane.Graph.AddRange(new Drawable[]
            {
                dots,
                spread,
                averageLine,
                sideLabel(ScosuOutcomeStrings.Early, Anchor.TopRight),
                sideLabel(ScosuOutcomeStrings.Late, Anchor.BottomRight),
            });

            dots.Delay(200).FadeIn(600, Easing.OutQuint);
            spread.Animate(1000, Easing.OutCubic);
            averageLine.Animate(1000, Easing.OutCubic);

            return lane;
        }

        private Drawable sideLabel(LocalisableString text, Anchor anchor) => new OsuSpriteText
        {
            Anchor = anchor,
            Origin = anchor,
            Text = text,
            Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
            Colour = colours.Gray9,
        };

        #endregion

        #region Difficulty

        /// <summary>
        /// The beatmap's difficulty under the play, as the website draws it: precision and speed on one scale, or the total where the ruleset doesn't split it.
        /// Labelled as the map's, so it isn't read as how the play went. Without the server's answer, the card goes without the lane.
        /// </summary>
        private void showDifficultyLane(APIScoreStrains? response)
        {
            if (response == null || response.Total.Count == 0 || response.SectionSeconds <= 0 || response.Total.Max() <= 0)
                return;

            strains = response;

            var lane = new Lane(difficulty_lane_height);
            lane.SetCaption(ScosuOutcomeStrings.MapDifficultyLane);
            lane.SetNote(ScosuOutcomeStrings.MapDifficultyNote);

            int samples = ComboBreakDisplay.RACE_SAMPLES;
            float[] xs = Enumerable.Range(0, samples).Select(i => (float)i / (samples - 1)).ToArray();
            float[] blank = new float[samples];

            var series = new List<(float[] Values, Color4 Colour)>();

            if (response.HasSkillSplit)
            {
                double top = Math.Max(response.Precision.Max(), response.Speed.Max());
                series.Add((sample(response.Precision, top, samples), precision_colour));
                series.Add((sample(response.Speed, top, samples), speed_colour));

                lane.AddLegend(legendItem(ScosuOutcomeStrings.Precision, response.PrecisionStars!.Value, precision_colour));
                lane.AddLegend(legendItem(ScosuOutcomeStrings.Speed, response.SpeedStars!.Value, speed_colour));
            }
            else
                series.Add((sample(response.Total, response.Total.Max(), samples), colours.ForStarDifficulty(response.Stars)));

            int hardest = response.Total.IndexOf(response.Total.Max());

            lane.AddLegend(new OsuSpriteText
            {
                Text = ScosuOutcomeStrings.HardestAt(ComboBreakDisplay.FormatTime(sectionTime(hardest + 0.5))),
                Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
                Colour = colours.Gray9,
                Margin = new MarginPadding { Left = 4 },
            });

            lane.Graph.Add(new DashedLine(horizontal: true) { RelativePositionAxes = Axes.Y, Y = 1 / 3f, Alpha = 0.08f });
            lane.Graph.Add(new DashedLine(horizontal: true) { RelativePositionAxes = Axes.Y, Y = 2 / 3f, Alpha = 0.08f });

            var plots = new List<Plot>();

            foreach (var (values, colour) in series)
            {
                plots.Add(new PlotArea(xs, blank, values, ColourInfo.GradientVertical(colour.Opacity(0.32f), colour.Opacity(0.02f))) { Rises = true });
                plots.Add(new PlotLine(xs, values, 2) { Colour = colour, Rises = true });
            }

            lane.Graph.AddRange(plots);

            // Where the play broke, on the harder of the two curves.
            var dots = shownBreaks.Select(b =>
            {
                float x = xAt(b.Time);
                float y = series.Max(s => valueAt(s.Values, x));
                return breakDot(b, x, y);
            }).ToArray();

            lane.Graph.AddRange(dots);

            lanes.Add(lane);
            lane.Appear();

            // The curves rise from the start of the play to its end, like the combo bars before them.
            foreach (var plot in plots)
                plot.Animate(1100, Easing.None);

            popIn(dots, 900);
        }

        /// <summary>
        /// The beatmap time at <paramref name="section"/> sections of the strains from the beatmap's start.
        /// </summary>
        private double sectionTime(double section)
            // The sections are in the play's time: a beatmap played faster passes through them sooner.
            => section * strains!.SectionSeconds * 1000 * ModUtils.CalculateRateWithMods(score.Mods);

        /// <summary>
        /// The section of the strains <paramref name="time"/> is in, as a fraction, from the middle of the first.
        /// </summary>
        private double sectionAt(double time) => time / sectionTime(1) - 0.5;

        /// <summary>
        /// <paramref name="series"/> at <paramref name="count"/> evenly spaced times of the play, from 0 to 1 of <paramref name="top"/>.
        /// </summary>
        private float[] sample(IReadOnlyList<double> series, double top, int count)
        {
            float[] values = new float[count];

            for (int i = 0; i < count; i++)
                values[i] = (float)(interpolate(series, sectionAt(timeAt((float)i / (count - 1)))) / Math.Max(top, 1e-6));

            return values;
        }

        private static double interpolate(IReadOnlyList<double> series, double section)
        {
            section = Math.Clamp(section, 0, series.Count - 1);
            int below = (int)Math.Floor(section);
            int above = Math.Min(below + 1, series.Count - 1);
            return Interpolation.Lerp(series[below], series[above], section - below);
        }

        private static float valueAt(float[] values, float x)
        {
            float position = Math.Clamp(x, 0, 1) * (values.Length - 1);
            int below = (int)Math.Floor(position);
            int above = Math.Min(below + 1, values.Length - 1);
            return (float)Interpolation.Lerp(values[below], values[above], position - below);
        }

        private Drawable legendItem(LocalisableString name, double stars, Color4 colour) => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(4, 0),
            Children = new Drawable[]
            {
                new Circle
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(6),
                    Colour = colour,
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = name,
                    Font = OsuFont.GetFont(size: 11, weight: FontWeight.SemiBold),
                    Colour = colours.GrayC,
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = LocalisableString.Interpolate($@"★ {stars.ToLocalisableString(@"0.00")}"),
                    Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                    Colour = Color4.White,
                },
            }
        };

        #endregion

        #region Hover

        /// <summary>
        /// What the lanes show at <paramref name="time"/>: the scores (or combo), the nearest hit's timing, and the map's difficulty.
        /// </summary>
        private LocalisableString describe(double time, ComboBreakAnalysis.ScoreRace? race)
        {
            LocalisableString text = ComboBreakDisplay.FormatTime(time);

            if (race != null)
            {
                var (play, fullCombo) = race.At(time);
                text = LocalisableString.Interpolate(
                    $"{text}\n{ScosuOutcomeStrings.ThisPlayScore(ScosuPlayOutcomeFormatter.Compact((long)play))} · {ScosuOutcomeStrings.FullComboScore(ScosuPlayOutcomeFormatter.Compact((long)fullCombo))} · −{ScosuPlayOutcomeFormatter.Compact((long)Math.Max(0, fullCombo - play))}");
            }
            else
            {
                int combo = analysis.Points.LastOrDefault(p => p.Time <= time).Combo;
                text = LocalisableString.Interpolate($"{text}\n{ScosuOutcomeStrings.ComboAt(combo.ToLocalisableString(@"N0"))}");
            }

            if (nearestHit(time) is TimedHit hit)
                text = LocalisableString.Interpolate($"{text}\n{timingText(hit)}");

            if (strains != null)
                text = LocalisableString.Interpolate($"{text}\n{difficultyText(time)}");

            return text;
        }

        /// <summary>
        /// The hit closest to <paramref name="time"/>, if any is close enough to be what the pointer is on.
        /// </summary>
        private TimedHit? nearestHit(double time)
        {
            if (hits.Length == 0)
                return null;

            // Within a couple of pixels' worth of time, or half a second on short plays.
            double reach = Math.Max(500, length / 200);
            var nearest = hits.MinBy(h => Math.Abs(h.Time - time));
            return Math.Abs(nearest.Time - time) <= reach ? nearest : null;
        }

        /// <summary>
        /// How hard the map is at <paramref name="time"/>, each skill against its own hardest point.
        /// </summary>
        private LocalisableString difficultyText(double time)
        {
            double section = sectionAt(time);

            static LocalisableString share(List<double> series, double section)
                => (interpolate(series, section) / Math.Max(series.Max(), 1e-6)).ToLocalisableString(@"0%");

            return strains!.HasSkillSplit
                ? ScosuOutcomeStrings.MapDifficultyAt(share(strains.Precision, section), share(strains.Speed, section))
                : ScosuOutcomeStrings.MapDifficultyTotalAt(share(strains.Total, section));
        }

        private static LocalisableString timingText(TimedHit hit)
        {
            if (hit.Result.IsMiss())
                return ScosuOutcomeStrings.Miss;

            double offset = Math.Round(hit.Offset);

            if (offset == 0)
                return ScosuOutcomeStrings.OnTime;

            var amount = Math.Abs(offset).ToLocalisableString(@"N0");
            return offset < 0 ? ScosuOutcomeStrings.MillisecondsEarly(amount) : ScosuOutcomeStrings.MillisecondsLate(amount);
        }

        #endregion

        private Drawable breakDot(ComboBreakAnalysis.ComboBreak comboBreak, float x, float y) => new Container
        {
            Anchor = Anchor.BottomLeft,
            Origin = Anchor.Centre,
            RelativePositionAxes = Axes.Both,
            X = x,
            Y = -y,
            Size = new Vector2(7),
            Masking = true,
            CornerRadius = 3.5f,
            BorderThickness = 1.5f,
            BorderColour = colours.Gray2,
            Alpha = 0,
            Scale = new Vector2(0.4f),
            Child = new Box { RelativeSizeAxes = Axes.Both, Colour = ComboBreakDisplay.KindColour(comboBreak, rulesetId, colours) },
        };

        private void popIn(IReadOnlyList<Drawable> dots, double delay)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                using (BeginDelayedSequence(delay + (double)i * 300 / Math.Max(1, dots.Count)))
                {
                    dots[i].FadeIn(300, Easing.OutQuint);
                    dots[i].ScaleTo(1, 500, Easing.OutQuint);
                }
            }
        }

        private Drawable timeLabel(double time, Anchor anchor) => new OsuSpriteText
        {
            Anchor = anchor,
            Origin = anchor,
            Text = ComboBreakDisplay.FormatTime(time),
            Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 2, weight: FontWeight.SemiBold),
            Colour = Color4.White,
            Alpha = 0.5f,
        };

        /// <param name="Time">When the object was, in the beatmap.</param>
        /// <param name="Offset">How early (below 0) or late it was hit, in milliseconds.</param>
        /// <param name="Result">How it was judged.</param>
        private readonly record struct TimedHit(double Time, double Offset, HitResult Result);

        /// <summary>
        /// In the score lane's header: whether to show the full combo's score, or only the play's, up close.
        /// </summary>
        private partial class FullComboToggle : OsuClickableContainer, IHasCurrentValue<bool>
        {
            private readonly BindableWithCurrent<bool> current = new BindableWithCurrent<bool>();

            public Bindable<bool> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            public override LocalisableString TooltipText => current.Value ? ScosuOutcomeStrings.HideFullCombo : ScosuOutcomeStrings.ShowFullCombo;

            private Box background = null!;
            private SpriteIcon icon = null!;
            private OsuSpriteText text = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            public FullComboToggle()
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 8;
                Action = () => current.Value = !current.Value;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // A pill, as a button looks: shown or not, it reads as something to click.
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Padding = new MarginPadding { Horizontal = 8, Vertical = 3 },
                        Children = new Drawable[]
                        {
                            icon = new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(10),
                            },
                            text = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = ScosuOutcomeStrings.FullComboToggle,
                                Font = OsuFont.GetFont(size: 10, weight: FontWeight.Bold),
                            },
                        }
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                current.BindValueChanged(_ => updateState(), true);
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
                base.OnHoverLost(e);
            }

            private void updateState()
            {
                bool shown = current.Value;

                // Shown: filled, in the full combo line's grey. Hidden: an outline-ish pill, the eye struck through.
                icon.Icon = shown ? FontAwesome.Solid.Eye : FontAwesome.Solid.EyeSlash;
                background.FadeTo(shown ? (IsHovered ? 0.24f : 0.16f) : (IsHovered ? 0.12f : 0.06f), 150, Easing.OutQuint);

                var colour = shown || IsHovered ? colours.GrayE : colours.Gray9;
                icon.FadeColour(colour, 150, Easing.OutQuint);
                text.FadeColour(colour, 150, Easing.OutQuint);
            }
        }

        /// <summary>
        /// One lane of the card: a faint rounded box with its caption (and legend) at the top, and a graph over the play's length.
        /// </summary>
        private partial class Lane : CompositeDrawable
        {
            private const float caption_height = 22;
            private const float bottom_padding = 8;

            public Container Graph { get; }

            /// <summary>
            /// The height of the graph, in which the plots' values from 0 to 1 are drawn.
            /// </summary>
            public float GraphHeight => Height - Graph.Padding.Top - bottom_padding;

            private readonly OsuSpriteText caption;
            private readonly FillFlowContainer header;

            public Lane(float height)
            {
                RelativeSizeAxes = Axes.X;
                Height = height;
                Masking = true;
                CornerRadius = 10;
                Alpha = 0;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0.03f,
                    },
                    Graph = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = caption_height, Bottom = bottom_padding, Horizontal = graph_inset },
                    },
                    header = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Margin = new MarginPadding { Left = graph_inset, Top = 7 },
                        Child = caption = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Font = OsuFont.GetFont(size: 10, weight: FontWeight.Bold),
                            Spacing = new Vector2(1, 0),
                        },
                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                caption.Colour = colours.Gray9;
            }

            public void SetCaption(LocalisableString text) => caption.Text = text.ToUpper();

            /// <summary>
            /// A short dim line under the header, saying what the lane shows. The lane grows to make room for it.
            /// </summary>
            public void SetNote(LocalisableString text)
            {
                const float note_height = 14;

                AddInternal(new OsuSpriteText
                {
                    Margin = new MarginPadding { Left = graph_inset, Top = caption_height },
                    Text = text,
                    Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
                    Colour = OsuColour.Gray(0.45f),
                });

                Height += note_height;
                Graph.Padding = Graph.Padding with { Top = caption_height + note_height };
            }

            /// <summary>
            /// Whether one of the header's controls (not its caption or legend texts) is at <paramref name="screenSpacePos"/>.
            /// </summary>
            public bool HeaderReceivesInputAt(Vector2 screenSpacePos)
                => header.OfType<FullComboToggle>().Any(t => t.IsPresent && t.ReceivePositionalInputAt(screenSpacePos));

            public void AddLegend(Drawable item)
            {
                item.Anchor = Anchor.CentreLeft;
                item.Origin = Anchor.CentreLeft;
                item.Alpha = 0;
                header.Add(item);
                item.Delay(300).FadeIn(300, Easing.OutQuint);
            }

            public void Appear() => this.FadeIn(300, Easing.OutQuint);
        }

        /// <summary>
        /// The guides through all the lanes: one at each break, and one where the pointer is.
        /// Hovering tells what the lanes show there (or what the break was); clicking a break watches the replay from a little before it.
        /// </summary>
        private partial class BreakOverlay : CompositeDrawable, IHasTooltip
        {
            /// <summary>
            /// How close to a break the pointer must be (in pixels) to be on it.
            /// </summary>
            private const float break_reach = 6;

            public Action<ComboBreakAnalysis.ComboBreak>? Watch { get; init; }

            public ComboBreakAnalysis.ScoreRace? Race { get; set; }

            public LocalisableString TooltipText => hoveredGroup is ComboBreakAnalysis.BreakGroup group
                ? ComboBreakDisplay.Tooltip(group.Shown, rulesetId, costsKnown(), Watch != null, group.Count - 1)
                : describe(time(pointerX), Race);

            private readonly ComboBreakAnalysis analysis;
            private readonly IReadOnlyList<ComboBreakAnalysis.BreakGroup> groups;
            private readonly int rulesetId;
            private readonly Func<double, ComboBreakAnalysis.ScoreRace?, LocalisableString> describe;
            private readonly Func<bool> costsKnown;

            private Container<DashedLine> guides = null!;
            private Box pointer = null!;

            private float pointerX;
            private ComboBreakAnalysis.BreakGroup? hoveredGroup;

            /// <summary>
            /// Where the lanes under it take input themselves.
            /// </summary>
            public Func<Vector2, bool>? PassThrough;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
                => base.ReceivePositionalInputAt(screenSpacePos) && PassThrough?.Invoke(screenSpacePos) != true;

            public BreakOverlay(ComboBreakAnalysis analysis, IReadOnlyList<ComboBreakAnalysis.BreakGroup> groups, int rulesetId, Func<double, ComboBreakAnalysis.ScoreRace?, LocalisableString> describe, Func<bool> costsKnown)
            {
                this.analysis = analysis;
                this.groups = groups;
                this.rulesetId = rulesetId;
                this.describe = describe;
                this.costsKnown = costsKnown;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                InternalChild = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = graph_inset },
                    Children = new Drawable[]
                    {
                        guides = new Container<DashedLine> { RelativeSizeAxes = Axes.Both },
                        pointer = new Box
                        {
                            RelativePositionAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Origin = Anchor.TopCentre,
                            Width = 1,
                            Colour = Color4.White,
                            Alpha = 0,
                        },
                    }
                };

                double length = Math.Max(1, analysis.EndTime - analysis.StartTime);

                foreach (var comboBreak in groups.Select(g => g.Shown))
                {
                    guides.Add(new DashedLine(horizontal: false)
                    {
                        RelativePositionAxes = Axes.X,
                        Origin = Anchor.TopCentre,
                        X = (float)Math.Clamp((comboBreak.Time - analysis.StartTime) / length, 0, 1),
                        Colour = ComboBreakDisplay.KindColour(comboBreak, rulesetId, colours),
                        Alpha = 0,
                    });
                }
            }

            public void ShowGuides()
            {
                for (int i = 0; i < guides.Count; i++)
                    guides[i].Delay(800 + (double)i * 300 / Math.Max(1, guides.Count)).FadeTo(restingAlpha(groups[i].Shown), 300, Easing.OutQuint);
            }

            /// <summary>
            /// A break that reset no combo (a miss right after another) is fainter.
            /// </summary>
            private static float restingAlpha(ComboBreakAnalysis.ComboBreak comboBreak) => comboBreak.ComboLost > 0 ? 0.3f : 0.15f;

            private float graphWidth => Math.Max(1, DrawWidth - 2 * graph_inset);

            private double time(float x) => analysis.StartTime + Math.Clamp((x - graph_inset) / graphWidth, 0, 1) * (analysis.EndTime - analysis.StartTime);

            protected override bool OnHover(HoverEvent e)
            {
                updatePointer(e.MousePosition.X);
                pointer.FadeTo(0.35f, 100, Easing.OutQuint);
                return true;
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                updatePointer(e.MousePosition.X);
                return base.OnMouseMove(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                pointer.FadeOut(200, Easing.OutQuint);
                setHoveredGroup(null);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (hoveredGroup is not ComboBreakAnalysis.BreakGroup group || Watch == null)
                    return false;

                Watch(group.Shown);
                // Also keeps the statistics panel open (clicking it elsewhere closes it).
                return true;
            }

            private void updatePointer(float x)
            {
                pointerX = x;
                pointer.X = Math.Clamp((x - graph_inset) / graphWidth, 0, 1);

                // The nearest break, if the pointer is on it.
                ComboBreakAnalysis.BreakGroup? nearest = null;
                float nearestDistance = break_reach;

                for (int i = 0; i < guides.Count; i++)
                {
                    float distance = Math.Abs(guides[i].X * graphWidth + graph_inset - x);

                    if (distance <= nearestDistance)
                    {
                        nearest = groups[i];
                        nearestDistance = distance;
                    }
                }

                setHoveredGroup(nearest);
            }

            private void setHoveredGroup(ComboBreakAnalysis.BreakGroup? group)
            {
                if (group == hoveredGroup)
                    return;

                for (int i = 0; i < guides.Count; i++)
                {
                    bool hovered = groups[i] == group;
                    guides[i].FadeTo(hovered ? 0.9f : restingAlpha(groups[i].Shown), 150, Easing.OutQuint);
                    guides[i].ResizeWidthTo(hovered ? 2 : 1, 150, Easing.OutQuint);
                }

                hoveredGroup = group;
                // The pointer's own guide would hide the break's.
                pointer.FadeTo(group == null && IsHovered ? 0.35f : 0, 100, Easing.OutQuint);
            }
        }

        /// <summary>
        /// A dashed line across its parent: a break's guide down through the lanes, or a grid line.
        /// </summary>
        private partial class DashedLine : CompositeDrawable
        {
            private const float dash = 4;
            private const float gap = 3;

            private readonly bool horizontal;
            private float drawnLength = -1;

            public DashedLine(bool horizontal)
            {
                this.horizontal = horizontal;

                RelativeSizeAxes = horizontal ? Axes.X : Axes.Y;

                if (horizontal)
                    Height = 1;
                else
                    Width = 1;
            }

            protected override void Update()
            {
                base.Update();

                float lineLength = horizontal ? DrawWidth : DrawHeight;

                if (lineLength == drawnLength)
                    return;

                drawnLength = lineLength;
                ClearInternal();

                for (float position = 0; position < lineLength; position += dash + gap)
                {
                    float size = Math.Min(dash, lineLength - position);

                    AddInternal(horizontal
                        ? new Box { X = position, Width = size, RelativeSizeAxes = Axes.Y }
                        : new Box { Y = position, Height = size, RelativeSizeAxes = Axes.X });
                }
            }
        }

        /// <summary>
        /// Something drawn from evenly or unevenly spaced samples over a lane's graph (x and values from 0 to 1, values from the bottom),
        /// which can draw in from the left or rise from the bottom, left first.
        /// </summary>
        private abstract partial class Plot : CompositeDrawable
        {
            private float progress = 1;

            /// <summary>
            /// How far it has drawn in, or risen, from 0 to 1.
            /// </summary>
            public float Progress
            {
                get => progress;
                set
                {
                    progress = value;
                    dirty = true;
                }
            }

            /// <summary>
            /// Whether it rises from the bottom rather than draws in from the left.
            /// </summary>
            public bool Rises { get; init; }

            private bool dirty = true;
            private Vector2 drawnSize;

            protected Plot()
            {
                RelativeSizeAxes = Axes.Both;
            }

            public void Animate(double duration, Easing easing)
            {
                Progress = 0;
                this.TransformTo(nameof(Progress), 1f, duration, easing);
            }

            protected override void Update()
            {
                base.Update();

                if (!dirty && DrawSize == drawnSize)
                    return;

                dirty = false;
                drawnSize = DrawSize;
                Redraw();
            }

            protected abstract void Redraw();

            /// <summary>
            /// How far drawing in has got (all of it when rising).
            /// </summary>
            protected float DrawnTo => Rises ? 1 : progress;

            /// <summary>
            /// How much of its value the sample at <paramref name="x"/> shows (all of it when drawing in): the left rises first.
            /// </summary>
            protected float RiseAt(float x)
            {
                if (!Rises)
                    return 1;

                float rise = Math.Clamp((progress - 0.45f * x) / 0.55f, 0, 1);
                return (float)Interpolation.ApplyEasing(Easing.OutQuint, rise);
            }

            protected Vector2 ToScreen(float x, float y) => new Vector2(x * DrawWidth, (1 - y) * DrawHeight);
        }

        /// <summary>
        /// A line through samples, solid or dashed.
        /// </summary>
        private partial class PlotLine : Plot
        {
            private const float dash = 5;
            private const float gap = 4;

            private readonly float[] xs;
            private readonly float[] values;
            private readonly float thickness;
            private readonly bool dashed;

            private readonly SmoothPath? path;
            private readonly Container<Box>? dashes;

            /// <summary>
            /// The size the dashes were laid out for: drawing in only shows more of them, so they are laid out again only when the size changes.
            /// </summary>
            private Vector2 dashedSize;

            public PlotLine(float[] xs, float[] values, float thickness, bool dashed = false)
            {
                this.xs = xs;
                this.values = values;
                this.thickness = thickness;
                this.dashed = dashed;

                if (dashed)
                    InternalChild = dashes = new Container<Box> { RelativeSizeAxes = Axes.Both };
                else
                    InternalChild = path = new SmoothPath { PathRadius = thickness / 2 };
            }

            protected override void Redraw()
            {
                if (dashed && !Rises)
                {
                    revealDashes();
                    return;
                }

                var points = new List<Vector2>(xs.Length);

                for (int i = 0; i < xs.Length; i++)
                {
                    if (xs[i] > DrawnTo)
                    {
                        // Up to where it has drawn in, between this sample and the one before.
                        if (i > 0)
                        {
                            float share = (DrawnTo - xs[i - 1]) / Math.Max(xs[i] - xs[i - 1], 1e-6f);
                            points.Add(ToScreen(DrawnTo, (float)Interpolation.Lerp(values[i - 1], values[i], share)));
                        }

                        break;
                    }

                    points.Add(ToScreen(xs[i], values[i] * RiseAt(xs[i])));
                }

                if (dashed)
                    drawDashes(points);
                else
                    drawPath(points);
            }

            private void drawPath(List<Vector2> points)
            {
                path!.Alpha = points.Count < 2 ? 0 : 1;

                if (points.Count < 2)
                    return;

                path.Vertices = points;
                // Vertices are drawn from their own top left: put the graph's origin back where it is.
                path.OriginPosition = path.PositionInBoundingBox(Vector2.Zero);
            }

            /// <summary>
            /// The whole dashed line, laid out once for its size, showing the dashes up to where it has drawn in.
            /// </summary>
            private void revealDashes()
            {
                if (DrawSize != dashedSize)
                {
                    dashedSize = DrawSize;
                    drawDashes(xs.Select((x, i) => ToScreen(x, values[i])).ToList());
                }

                float drawnTo = DrawnTo * DrawWidth;

                foreach (var d in dashes!)
                    d.Alpha = d.X <= drawnTo ? 1 : 0;
            }

            private void drawDashes(List<Vector2> points)
            {
                dashes!.Clear();

                // Walks along the line, laying a dash, then a gap, and so on.
                float walked = 0;

                for (int i = 1; i < points.Count; i++)
                {
                    Vector2 from = points[i - 1], to = points[i];
                    float segment = Vector2.Distance(from, to);

                    if (segment <= 0)
                        continue;

                    Vector2 direction = (to - from) / segment;
                    float position = 0;

                    while (position < segment)
                    {
                        float inPattern = walked % (dash + gap);
                        float left = inPattern < dash ? dash - inPattern : dash + gap - inPattern;
                        float step = Math.Min(left, segment - position);

                        if (inPattern < dash)
                        {
                            dashes.Add(new Box
                            {
                                Origin = Anchor.CentreLeft,
                                Position = from + direction * position,
                                Width = step,
                                Height = thickness,
                                Rotation = MathHelper.RadiansToDegrees(MathF.Atan2(direction.Y, direction.X)),
                                EdgeSmoothness = new Vector2(1),
                            });
                        }

                        position += step;
                        walked += step;
                    }
                }
            }
        }

        /// <summary>
        /// The area between two series of samples, in thin columns.
        /// </summary>
        private partial class PlotArea : Plot
        {
            private readonly float[] xs;
            private readonly float[] lower;
            private readonly float[] upper;
            private readonly Box[] columns;

            public PlotArea(float[] xs, float[] lower, float[] upper, ColourInfo colour)
            {
                this.xs = xs;
                this.lower = lower;
                this.upper = upper;

                // One column between each two samples, as high as both on average.
                columns = new Box[Math.Max(0, xs.Length - 1)];

                for (int i = 0; i < columns.Length; i++)
                {
                    columns[i] = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Both,
                        X = xs[i],
                        Width = xs[i + 1] - xs[i],
                        Colour = colour,
                    };
                }

                InternalChildren = columns;
            }

            protected override void Redraw()
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    float middle = (xs[i] + xs[i + 1]) / 2;
                    float rise = RiseAt(middle);
                    float bottom = (lower[i] + lower[i + 1]) / 2 * rise;
                    float top = (upper[i] + upper[i + 1]) / 2 * rise;

                    columns[i].Alpha = xs[i + 1] <= DrawnTo + 1e-4f ? 1 : 0;
                    columns[i].Y = -bottom;
                    columns[i].Height = Math.Max(0, top - bottom);
                }
            }
        }
    }
}
