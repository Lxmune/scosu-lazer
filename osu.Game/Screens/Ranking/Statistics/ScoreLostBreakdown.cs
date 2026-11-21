// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// On servers that rank plays by osu!stable score (see <see cref="Online.EndpointConfiguration.RanksByStableScore"/>):
    /// how much of the beatmap's best score a play earned, and the score it missed, split by the server into what the combo breaks and what the judgements cost.
    /// Stays hidden until the server answers, and if it can't.
    /// </summary>
    public partial class ScoreLostBreakdown : CompositeDrawable, IHasTooltip
    {
        public LocalisableString TooltipText => ScosuResultsStrings.ScoreLostTooltip;

        private readonly ScoreInfo score;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private GetScoreBreakdownRequest? request;
        private FillFlowContainer rows = null!;
        private Box bar = null!;
        private Drawable? card;

        public ScoreLostBreakdown(ScoreInfo score)
        {
            this.score = score;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new osuTK.Vector2(0, 8),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 6,
                            Masking = true,
                            CornerRadius = 3,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colours.Gray3,
                                },
                                bar = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Width = 0,
                                    Colour = colours.Lime1,
                                },
                            }
                        },
                        rows = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new osuTK.Vector2(0, 4),
                        },
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The statistic's card is shown once there is something to show: not without an online score, nor when the request fails.
            card = this.FindClosestParent<StatisticItemContainer>();
            card?.Hide();

            // A score without an online ID has not been submitted (yet), so the server doesn't know it.
            if (!api.Endpoints.RanksByStableScore || !api.IsLoggedIn || score.OnlineID <= 0)
                return;

            request = new GetScoreBreakdownRequest(score.OnlineID);
            request.Success += breakdown => Schedule(() => show(breakdown));
            api.Queue(request);
        }

        private void show(APIScoreBreakdown? breakdown)
        {
            if (breakdown == null || breakdown.MaxScore <= 0)
                return;

            rows.Clear();

            rows.Add(new Row(ScosuResultsStrings.ShareOfMaximum, ScosuResultsStrings.PercentOfMaximum(breakdown.Fraction.ToLocalisableString(@"0.0%"))));
            rows.Add(new Row(ScosuResultsStrings.ScoreMissing, breakdown.Missing.ToLocalisableString(@"N0")));

            // The gap can't be told apart by cause for osu!mania and for plays imported from osu!stable.
            if (breakdown.Split is APIScoreBreakdown.ScoreSplit split)
            {
                rows.Add(new Row(ScosuResultsStrings.LostToComboBreaks, formatLoss(split.ComboLoss, split.Exact)));
                rows.Add(new Row(ScosuResultsStrings.LostToJudgements, split.JudgementLoss.ToLocalisableString(@"N0")));
            }

            bar.ResizeWidthTo((float)breakdown.Fraction, 300, Easing.OutQuint);
            card?.FadeIn(250, Easing.OutQuint);
        }

        private static LocalisableString formatLoss(long loss, bool exact)
        {
            var text = loss.ToLocalisableString(@"N0");
            return exact ? text : ScosuResultsStrings.Estimate(text);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            request?.Cancel();
        }

        private partial class Row : SimpleStatisticItem
        {
            public Row(LocalisableString name, LocalisableString value)
                : base(name)
            {
                Value = value;
                FontSize = StatisticItem.FONT_SIZE;
            }
        }
    }
}
