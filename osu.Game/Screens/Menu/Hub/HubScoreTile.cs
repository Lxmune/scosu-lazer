// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using System;
using System.Linq;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The ranked score gained today and this week, the global rank and what it takes to pass the next player. Opens the local user's profile.
    /// </summary>
    public partial class HubScoreTile : HubTile
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private ScoreSummaryProvider? provider { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        /// <summary>
        /// The rank history changes once a day, so reopening the hub soon doesn't ask again.
        /// </summary>
        private static readonly TimeSpan history_interval = TimeSpan.FromMinutes(5);

        private GetUserRequest? historyRequest;
        private DateTimeOffset? lastHistoryFetch;
        private float[]? rankHistory;

        private readonly IBindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        public HubScoreTile()
        {
            Eyebrow.Text = MenuHubStrings.ScoreGained.ToUpper();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (provider != null)
                summary.BindTo(provider.Summary);

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(refresh));
            summary.BindValueChanged(_ => refresh(), true);
        }

        private void refresh()
        {
            if (summary.Value is not APIScoreSummary s)
            {
                ShowPlaceholder(FontAwesome.Solid.ChartLine, api.IsLoggedIn ? MenuHubStrings.ScoreUnavailable : MenuHubStrings.LogInForScore);
                return;
            }

            ShowContent();
            showGraph();
            Body.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(36, 0),
                Children = new Drawable[]
                {
                    stat($@"+{s.Today.RankedScore:N0}", LocalisableString.Interpolate($@"{ScoreSummaryStrings.Today} · {ScoreSummaryStrings.PlayCount(s.Today.PlayCount.ToLocalisableString(@"N0"))}"), Color4.White),
                    stat(s.Rank is int rank ? $@"#{rank:N0}" : @"-", MenuHubStrings.GlobalRank, Color4.White),
                    stat($@"+{ScosuMenuFormatting.CompactScore(s.Week.RankedScore)}", LocalisableString.Interpolate($@"{ScoreSummaryStrings.ThisWeek} · {ScoreSummaryStrings.PlayCount(s.Week.PlayCount.ToLocalisableString(@"N0"))}"), ScosuMenuStyle.TEXT_DIM),
                },
            });

            if (s.Next is APIScoreSummary.NextRank next)
                SetFoot(FontAwesome.Solid.ArrowUp, ScoreSummaryStrings.ScoreToNextRank(ScosuMenuFormatting.CompactScore(next.RankedScoreNeeded), next.Rank.ToLocalisableString(@"N0"), next.Username), ScosuMenuStyle.VIOLET);
            else
                SetFoot(null, default);

            SetAction(() => game?.ShowUser(api.LocalUser.Value));
        }

        /// <summary>
        /// Fetches the user's rank history for the graph behind the numbers, unless that was done recently.
        /// </summary>
        public void RefreshIfStale()
        {
            if (historyRequest != null || !api.IsLoggedIn || api.State.Value != APIState.Online)
                return;

            if (lastHistoryFetch != null && DateTimeOffset.Now - lastHistoryFetch < history_interval)
                return;

            var req = historyRequest = new GetUserRequest(api.LocalUser.Value.Id, ruleset.Value);

            req.Success += user => Schedule(() =>
            {
                if (req != historyRequest)
                    return;

                historyRequest = null;
                lastHistoryFetch = DateTimeOffset.Now;

                // Days without a rank are zeros.
                rankHistory = user.Statistics?.RankHistory?.Data?.Where(r => r > 0).Select(r => (float)r).ToArray();
                refresh();
            });
            req.Failure += _ => Schedule(() =>
            {
                if (req == historyRequest)
                    historyRequest = null;
            });

            api.Queue(req);
        }

        /// <summary>
        /// The rank history as a faint area at the bottom of the tile, a better rank higher up.
        /// </summary>
        private void showGraph()
        {
            if (rankHistory == null || rankHistory.Length < 2)
                return;

            SetBackground(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Height = 0.7f,
                        Colour = ColourInfo.GradientVertical(ScosuMenuStyle.VIOLET.Opacity(0), ScosuMenuStyle.VIOLET.Opacity(0.16f)),
                    },
                    new LineGraph
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Height = 0.7f,
                        LineColour = ScosuMenuStyle.VIOLET.Opacity(0.7f),
                        Values = rankHistory.Select(r => -r),
                    },
                },
            }, 0);
        }

        private static Drawable stat(string value, LocalisableString caption, Color4 colour) => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 2),
            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = value,
                    Font = ScosuMenuStyle.Display(36),
                    Colour = colour,
                },
                new OsuSpriteText
                {
                    Text = caption,
                    Font = ScosuMenuStyle.Body(12),
                    Colour = ScosuMenuStyle.TEXT_DIM,
                },
            },
        };
    }
}
