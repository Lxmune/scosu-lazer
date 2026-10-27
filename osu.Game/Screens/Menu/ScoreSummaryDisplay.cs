// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The ranked score the local user gained today and this week, in the selected ruleset.
    /// Hidden while logged out, and when the server can't tell (see <see cref="GetScoreSummaryRequest"/>).
    /// </summary>
    public partial class ScoreSummaryDisplay : VisibilityContainer
    {
        private const float transition_duration = 500;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScoreSummaryRequest? request;

        private Container content = null!;
        private OsuSpriteText todayScore = null!;
        private OsuSpriteText todayDetails = null!;
        private Container graphContainer = null!;
        private LineGraph todayGraph = null!;
        private OsuSpriteText weekScore = null!;
        private OsuSpriteText weekDetails = null!;
        private OsuSpriteText nextRank = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = content = new Container
            {
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Masking = true,
                CornerExponent = 2.5f,
                CornerRadius = 15,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.4f,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Horizontal = 15, Vertical = 10 },
                        Spacing = new Vector2(0, 2),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = ScoreSummaryStrings.Today.ToUpper(),
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                Colour = colours.Yellow,
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    todayScore = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = OsuFont.GetFont(size: 26, weight: FontWeight.Light, fixedWidth: true),
                                    },
                                    graphContainer = new Container
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(100, 22),
                                        Child = todayGraph = new LineGraph
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            LineColour = colours.Yellow,
                                            MinValue = 0,
                                        },
                                    },
                                },
                            },
                            todayDetails = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5, 0),
                                Margin = new MarginPadding { Top = 6 },
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = ScoreSummaryStrings.ThisWeek.ToUpper(),
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                        Colour = colours.Yellow,
                                    },
                                    weekScore = new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                    },
                                    weekDetails = new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                    },
                                },
                            },
                            nextRank = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = colours.Yellow,
                                Margin = new MarginPadding { Top = 2 },
                            },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            ruleset.BindValueChanged(_ => Refresh(), true);
        }

        /// <summary>
        /// Fetches the summary again, e.g. after a play.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have the summary.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online || !ruleset.Value.IsLegacyRuleset())
            {
                hideContent();
                return;
            }

            var req = request = new GetScoreSummaryRequest(ruleset.Value, GetScoreSummaryRequest.LocalMidnight());

            req.Success += summary =>
            {
                if (req == request)
                    display(summary);
            };
            req.Failure += _ =>
            {
                if (req == request)
                    hideContent();
            };

            api.Queue(req);
        }

        private void display(APIScoreSummary summary)
        {
            todayScore.Text = formatGain(summary.Today.RankedScore);
            todayDetails.Text = formatDetails(summary.Today, summary.Rank);

            weekScore.Text = formatGain(summary.Week.RankedScore);
            weekDetails.Text = formatDetails(summary.Week, summary.Rank);

            // Starting from nothing gained, so a single play still draws a line.
            var points = summary.Today.Points;

            if (points.Count > 0)
            {
                todayGraph.Values = points.Select(p => (float)p.RankedScore).Prepend(0);
                graphContainer.Show();
            }
            else
                graphContainer.Hide();

            if (summary.Next is APIScoreSummary.NextRank next)
            {
                nextRank.Text = ScoreSummaryStrings.ScoreToNextRank(next.RankedScoreNeeded.ToLocalisableString(@"N0"), next.Rank.ToLocalisableString(@"N0"), next.Username);
                nextRank.Show();
            }
            else
                nextRank.Hide();

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

        private void hideContent() => content.FadeOut(transition_duration, Easing.OutQuint);

        private static LocalisableString formatGain(long rankedScore) => LocalisableString.Interpolate($@"+{rankedScore.ToLocalisableString(@"N0")}");

        /// <summary>
        /// "23 plays · #45 → #41", or less when a rank is unknown.
        /// </summary>
        private static LocalisableString formatDetails(APIScoreSummary.Period period, int? rank)
        {
            var plays = ScoreSummaryStrings.PlayCount(period.PlayCount.ToLocalisableString(@"N0"));

            if (rank is not int current)
                return plays;

            if (period.RankStart is int start && start != current)
                return LocalisableString.Interpolate($@"{plays} · #{start.ToLocalisableString(@"N0")} → #{current.ToLocalisableString(@"N0")}");

            return LocalisableString.Interpolate($@"{plays} · #{current.ToLocalisableString(@"N0")}");
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (content.Alpha == 0)
                return false;

            game?.ShowUser(api.LocalUser.Value);
            return true;
        }

        protected override void PopIn() => this.FadeIn(transition_duration, Easing.OutQuint);

        protected override void PopOut() => this.FadeOut(transition_duration, Easing.OutQuint);

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            request = null;

            base.Dispose(isDisposing);
        }
    }
}
