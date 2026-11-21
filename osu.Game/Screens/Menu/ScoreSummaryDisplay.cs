// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
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
    /// The ranked score the local user gained today and this week, in the selected ruleset, and what it takes to pass the next player.
    /// Hidden while logged out, and when the server can't tell (see <see cref="GetScoreSummaryRequest"/>).
    /// </summary>
    public partial class ScoreSummaryDisplay : CornerCard
    {
        private const float transition_duration = 500;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private ScoreSummaryProvider provider { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();

        private Container content = null!;
        private OsuSpriteText todayScore = null!;
        private OsuSpriteText todayDetails = null!;
        private Container graphContainer = null!;
        private LineGraph todayGraph = null!;
        private OsuSpriteText week = null!;
        private FillFlowContainer nextRank = null!;
        private OsuSpriteText nextRankText = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Margined = content = new Container
            {
                Width = CARD_WIDTH,
                AutoSizeAxes = Axes.Y,
                Alpha = 0,
                Children = new Drawable[]
                {
                    ScosuMenuStyle.CreateGlassBackground(),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(ScosuMenuStyle.CARD_PADDING),
                        Spacing = new Vector2(0, 4),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = ScoreSummaryStrings.Today.ToUpper(),
                                Font = ScosuMenuStyle.Eyebrow,
                                Spacing = new Vector2(1.5f, 0),
                                Colour = ScosuMenuStyle.VIOLET,
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(12, 0),
                                Children = new Drawable[]
                                {
                                    todayScore = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = ScosuMenuStyle.Display(30),
                                    },
                                    graphContainer = new Container
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(64, 22),
                                        Child = todayGraph = new LineGraph
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            LineColour = ScosuMenuStyle.VIOLET,
                                            MinValue = 0,
                                        },
                                    },
                                },
                            },
                            todayDetails = new OsuSpriteText
                            {
                                Font = ScosuMenuStyle.Body(13),
                                Colour = ScosuMenuStyle.TEXT_DIM,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 1,
                                Colour = ScosuMenuStyle.OUTLINE,
                                Margin = new MarginPadding { Vertical = 4 },
                            },
                            week = new OsuSpriteText
                            {
                                Font = ScosuMenuStyle.Body(13),
                                Colour = ScosuMenuStyle.TEXT_DIM,
                            },
                            nextRank = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(6, 0),
                                Alpha = 0,
                                Children = new Drawable[]
                                {
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(9),
                                        Icon = FontAwesome.Solid.ArrowUp,
                                        Colour = ScosuMenuStyle.VIOLET_INK,
                                    },
                                    nextRankText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = ScosuMenuStyle.Body(13),
                                        Colour = ScosuMenuStyle.VIOLET_INK,
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            summary.BindTo(provider.Summary);
            summary.BindValueChanged(s =>
            {
                if (s.NewValue is APIScoreSummary value)
                    display(value);
                else
                    hideContent();
            }, true);
        }

        /// <summary>
        /// Fetches the summary again, e.g. after a play.
        /// </summary>
        public void Refresh() => provider.Refresh();

        private void display(APIScoreSummary summary)
        {
            todayScore.Text = formatGain(summary.Today.RankedScore);
            todayDetails.Text = formatDetails(summary.Today, summary.Rank);

            week.Text = formatWeek(summary.Week, summary.Rank);

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
                nextRankText.Text = ScoreSummaryStrings.ScoreToNextRank(ScosuMenuFormatting.CompactScore(next.RankedScoreNeeded), next.Rank.ToLocalisableString(@"N0"), next.Username);
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

        /// <summary>
        /// "This week +12,345 · 23 plays · #45 → #41": the rank is on the line above, so only a change of it is repeated here.
        /// </summary>
        private static LocalisableString formatWeek(APIScoreSummary.Period period, int? rank)
        {
            var summary = LocalisableString.Interpolate(
                $@"{ScoreSummaryStrings.ThisWeek} {formatGain(period.RankedScore)} · {ScoreSummaryStrings.PlayCount(period.PlayCount.ToLocalisableString(@"N0"))}");

            if (rank is int current && period.RankStart is int start && start != current)
                return LocalisableString.Interpolate($@"{summary} · #{start.ToLocalisableString(@"N0")} → #{current.ToLocalisableString(@"N0")}");

            return summary;
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
    }
}
