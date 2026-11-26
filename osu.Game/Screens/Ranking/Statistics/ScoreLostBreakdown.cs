// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online;
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
        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private FillFlowContainer rows = null!;
        private Drawable? card;

        public ScoreLostBreakdown(ScoreInfo score)
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
                Spacing = new osuTK.Vector2(0, 8),
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
            // Not scheduled on this drawable: its card is hidden until now, and hidden drawables don't run their scheduler.
            // The API already calls back on the update thread, and a request cancelled on disposal never calls back.
            request.Success += show;
            api.Queue(request);
        }

        private void show(APIScoreBreakdown? breakdown)
        {
            if (breakdown == null || breakdown.MaxScore <= 0)
                return;

            rows.Clear();

            double share(long part) => (double)part / breakdown.MaxScore;

            var tiles = new ScosuTileGrid(2);
            tiles.AddTile(new ScosuStatTile(ScosuResultsStrings.ShareOfMaximum, breakdown.Fraction.ToLocalisableString(@"0.0%"),
                ScosuOutcomeStrings.ScoreOfMaximum(ScosuPlayOutcomeFormatter.Compact(breakdown.Score), ScosuPlayOutcomeFormatter.Compact(breakdown.MaxScore)))
            {
                ValueColour = colours.Lime1,
            });
            tiles.AddTile(new ScosuStatTile(ScosuOutcomeStrings.TileMissing, LocalisableString.Interpolate($@"−{ScosuPlayOutcomeFormatter.Compact(breakdown.Missing)}"),
                breakdown.Missing.ToLocalisableString(@"N0"))
            {
                Muted = breakdown.Missing <= 0,
            });
            rows.Add(tiles);

            // What the gap went to. The score earned and its bar are the panel's (the score, and the bar under it), so they aren't repeated here.
            // The gap can't be told apart by cause for osu!mania and for plays imported from osu!stable: the "missing" tile says it all then.
            if (breakdown.Split is APIScoreBreakdown.ScoreSplit split)
            {
                rows.Add(new ScosuStatisticRow(ScosuResultsStrings.LostToComboBreaks, formatLoss(split.ComboLoss, split.Exact))
                {
                    DotColour = colours.Red1,
                    Detail = share(split.ComboLoss).ToLocalisableString(@"0%"),
                });
                rows.Add(new ScosuStatisticRow(ScosuResultsStrings.LostToJudgements, split.JudgementLoss.ToLocalisableString(@"N0"))
                {
                    DotColour = colours.GrayA,
                    Detail = share(split.JudgementLoss).ToLocalisableString(@"0%"),
                });
            }

            card?.FadeIn(250, Easing.OutQuint);
            ScosuStatisticRow.Cascade(rows);
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
    }
}
