// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Extensions;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Expanded.Accuracy;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Expanded
{
    /// <summary>
    /// scosu: under the score, how much of the beatmap's maximum (with the play's mods) it earned, then what combo breaks and judgements cost,
    /// as the server splits it (see <see cref="APIScoreBreakdown"/>). Fills once the score has counted up; stays empty if the server can't tell.
    /// </summary>
    public partial class ScoreShareBar : CompositeDrawable, IHasTooltip
    {
        private const float bar_height = 4;

        /// <summary>
        /// The legend's height, kept from the start: the panel is a fixed size, and nothing in it should move when the server answers.
        /// </summary>
        private const float legend_height = 15;

        public LocalisableString TooltipText { get; private set; }

        private readonly ScoreInfo score;
        private readonly bool withFlair;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private GetScoreBreakdownRequest? request;

        private FillFlowContainer bar = null!;
        private FillFlowContainer legend = null!;

        /// <summary>
        /// When the score has finished counting up: the bar fills then, not before.
        /// </summary>
        private double fillTime;

        /// <summary>
        /// Whether the server can split <paramref name="score"/>: only plays it has, on servers that rank by osu!stable score.
        /// </summary>
        public static bool IsAvailable(ScoreInfo score, IAPIProvider api)
            => api.Endpoints.RanksByStableScore && api.IsLoggedIn && score.OnlineID > 0 && score.Ruleset.IsLegacyRuleset();

        public ScoreShareBar(ScoreInfo score, bool withFlair)
        {
            this.score = score;
            this.withFlair = withFlair;

            RelativeSizeAxes = Axes.X;
            Width = 0.8f;
            AutoSizeAxes = Axes.Y;
            // The room stays, so nothing below moves when the server answers.
            Alpha = 0;
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new CircularContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = bar_height,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                // As the statistic headers below.
                                Colour = Color4Extensions.FromHex(@"222"),
                            },
                            bar = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                            },
                        }
                    },
                    legend = new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        AutoSizeAxes = Axes.X,
                        Height = legend_height,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            fillTime = Time.Current + (withFlair ? AccuracyCircle.ACCURACY_TRANSFORM_DELAY + AccuracyCircle.ACCURACY_TRANSFORM_DURATION : 0);

            if (!IsAvailable(score, api))
                return;

            request = new GetScoreBreakdownRequest(score.OnlineID);
            request.Success += breakdown => Schedule(() => show(breakdown));
            api.Queue(request);
        }

        private void show(APIScoreBreakdown? breakdown)
        {
            if (breakdown == null || breakdown.MaxScore <= 0)
                return;

            float fraction(long part) => (float)System.Math.Clamp((double)part / breakdown.MaxScore, 0, 1);

            var parts = new List<(float Width, Color4 Colour)> { (fraction(breakdown.Score), colours.Lime1) };

            legend.Add(new OsuSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Text = ScosuResultsStrings.PercentOfMaximum(breakdown.Fraction.ToLocalisableString(@"0.0%")),
                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
            });

            if (breakdown.Split is APIScoreBreakdown.ScoreSplit s)
            {
                parts.Add((fraction(s.ComboLoss), colours.Red1));
                // Grey, not a hue: orange is slider breaks in this panel, and judgements are the quieter loss.
                parts.Add((fraction(s.JudgementLoss), colours.GrayA));

                if (s.ComboLoss > 0)
                    legend.Add(new LegendItem(ScosuOutcomeStrings.ScoreBarBreaks(ScosuPlayOutcomeFormatter.Compact(s.ComboLoss)), colours.Red1));
                if (s.JudgementLoss > 0)
                    legend.Add(new LegendItem(ScosuOutcomeStrings.ScoreBarJudgements(ScosuPlayOutcomeFormatter.Compact(s.JudgementLoss)), colours.GrayA));

                TooltipText = LocalisableString.Interpolate(
                    $"{ScosuResultsStrings.ScoreMissing}: {breakdown.Missing.ToLocalisableString(@"N0")}\n{ScosuResultsStrings.LostToComboBreaks}: {formatLoss(s.ComboLoss, s.Exact)}\n{ScosuResultsStrings.LostToJudgements}: {s.JudgementLoss.ToLocalisableString(@"N0")}");
            }
            else
            {
                // Without a split (osu!mania, plays imported from osu!stable), the missing score is told as one.
                parts.Add((fraction(breakdown.Missing), colours.Gray7));

                if (breakdown.Missing > 0)
                    legend.Add(new LegendItem(ScosuOutcomeStrings.ScoreBarMissing(ScosuPlayOutcomeFormatter.Compact(breakdown.Missing)), colours.Gray7));

                TooltipText = LocalisableString.Interpolate($"{ScosuResultsStrings.ScoreMissing}: {breakdown.Missing.ToLocalisableString(@"N0")}");
            }

            using (BeginDelayedSequence(System.Math.Max(0, fillTime - Time.Current)))
            {
                this.FadeIn(200, Easing.OutQuint);

                double delay = 0;

                foreach (var (width, colour) in parts)
                {
                    var segment = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0,
                        Colour = colour,
                    };

                    bar.Add(segment);

                    // The earned part first, then what was lost, one after the other.
                    using (BeginDelayedSequence(delay))
                        segment.ResizeWidthTo(width, 500, Easing.OutQuint);

                    delay += 250;
                }

                // Each legend entry with its part of the bar. They start hidden: faded in from zero only once their turn comes,
                // they would show with the bar first and blink out and in again.
                double legendDelay = 0;

                foreach (var item in legend)
                {
                    item.Alpha = 0;

                    using (BeginDelayedSequence(legendDelay))
                        item.FadeIn(400, Easing.OutQuint);

                    legendDelay += 250;
                }
            }
        }

        private static LocalisableString formatLoss(long loss, bool exact)
        {
            var text = loss.ToLocalisableString(@"N0");
            return exact ? text : ScosuResultsStrings.Estimate(text);
        }

        /// <summary>
        /// A part of the bar: its colour as a dot, then what it is.
        /// </summary>
        private partial class LegendItem : FillFlowContainer
        {
            public LegendItem(LocalisableString text, Color4 colour)
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(4, 0);

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
                        Text = text,
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                        Colour = Color4.White,
                        Alpha = 0.6f,
                    },
                };
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            request?.Cancel();
        }
    }
}
