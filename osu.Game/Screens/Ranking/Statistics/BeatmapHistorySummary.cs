// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: the local user's history on the score's beatmap and leaderboard, the play just made included:
    /// how many plays and how many passed, and the play closest to a full combo.
    /// Stays hidden until the server answers, and if it can't.
    /// </summary>
    public partial class BeatmapHistorySummary : CompositeDrawable
    {
        private readonly ScoreInfo score;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private GetBeatmapAttemptsRequest? request;
        private FillFlowContainer rows = null!;
        private Drawable? card;

        public BeatmapHistorySummary(ScoreInfo score)
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
                Spacing = new Vector2(0, 6),
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            card = this.FindClosestParent<StatisticItemContainer>();
            card?.Hide();

            if (!api.Endpoints.RanksByStableScore || !api.IsLoggedIn || score.BeatmapInfo?.OnlineID > 0 != true)
                return;

            var req = request = new GetBeatmapAttemptsRequest(score.BeatmapInfo, score.GetLeaderboardRuleset());
            // Not scheduled on this drawable: its card is hidden until now, and hidden drawables don't run their scheduler.
            // The API already calls back on the update thread, and a request cancelled on disposal never calls back.
            req.Success += show;
            api.Queue(req);
        }

        private void show(APIBeatmapAttempts? attempts)
        {
            if (attempts == null || attempts.Plays <= 0)
                return;

            rows.Clear();

            var tiles = new ScosuTileGrid(3);
            bool firstPlay = attempts.Plays == 1 && attempts.Unsubmitted <= 0;

            tiles.AddTile(new ScosuStatTile(ScosuOutcomeStrings.TilePlays, attempts.Plays.ToLocalisableString(@"N0"), firstPlay ? ScosuOutcomeStrings.FirstPlay : default)
            {
                TooltipText = ScosuResultsStrings.HistoryTooltip,
            });

            tiles.AddTile(new ScosuStatTile(ScosuOutcomeStrings.TilePassRate, (attempts.PassRate ?? 0).ToLocalisableString(@"0%"),
                ScosuOutcomeStrings.PassesOf(attempts.Passes.ToLocalisableString(@"N0"), attempts.Plays.ToLocalisableString(@"N0"))));

            if (attempts.Closest is APIBeatmapAttempts.ClosestAttempt closest)
            {
                bool fullCombo = closest.FullCombo || closest.Misses + closest.SliderBreaks <= 0 && closest.BeatmapMaxCombo > 0 && closest.MaxCombo >= closest.BeatmapMaxCombo;
                var outOf = ScosuOutcomeStrings.OfCount(LocalisableString.Interpolate($@"{closest.BeatmapMaxCombo.ToLocalisableString(@"N0")}x"));
                var misses = closest.Misses == 1 ? ScosuOutcomeStrings.OneMiss : ScosuOutcomeStrings.MissCount(closest.Misses.ToLocalisableString(@"N0"));

                // Slider breaks apart from the misses, as the panel counts them.
                var breaks = closest.SliderBreaks <= 0 ? misses
                    : LocalisableString.Interpolate($@"{misses} · {(closest.SliderBreaks == 1 ? ScosuOutcomeStrings.OneSliderBreak : ScosuOutcomeStrings.SliderBreakCount(closest.SliderBreaks.ToLocalisableString(@"N0")))}");

                // This play, when it is the closest one yet, says so where it shows.
                tiles.AddTile(new ScosuStatTile(closest.ScoreId == score.OnlineID ? ScosuOutcomeStrings.TileClosestThisPlay : ScosuOutcomeStrings.TileClosest,
                    fullCombo ? ScosuOutcomeStrings.FullComboShort : LocalisableString.Interpolate($@"{closest.MaxCombo.ToLocalisableString(@"N0")}x"),
                    fullCombo ? ScosuOutcomeStrings.FullCombo : LocalisableString.Interpolate($@"{outOf} · {breaks}"))
                {
                    ValueColour = fullCombo ? colours.Lime1 : null,
                });
            }

            rows.Add(tiles);

            card?.FadeIn(250, Easing.OutQuint);
            ScosuStatisticRow.Cascade(tiles);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            request?.Cancel();
        }
    }
}
