// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
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
    public partial class ScoreSummaryDisplay : CornerInfoCard
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
        private CornerAreaGraph todayGraph = null!;
        private OsuSpriteText week = null!;

        private long shownGain;
        private bool hasShown;

        /// <summary>
        /// The gain as drawn, which counts up to the real one when it changes.
        /// </summary>
        public double DisplayedGain
        {
            get => shownGain;
            set
            {
                shownGain = (long)Math.Round(value);
                todayScore.Text = formatGain(shownGain);
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Margined = content = new Container
            {
                Width = CARD_WIDTH,
                AutoSizeAxes = Axes.Y,
                Alpha = 0,
                Children = new[]
                {
                    CreateSurface(todayGraph = new CornerAreaGraph(ScosuMenuStyle.VIOLET)
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 80,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                    }),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
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
                                    todayScore = new OsuSpriteText
                                    {
                                        Font = ScosuMenuStyle.Display(36),
                                        Text = formatGain(0),
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
            // Count up from what is shown (from nothing when the card just appeared), and shimmer when it is a new value.
            long gain = summary.Today.RankedScore;

            if (!hasShown)
                DisplayedGain = 0;

            if (gain != shownGain)
            {
                this.TransformTo(nameof(DisplayedGain), (double)gain, 800, Easing.OutQuint);
                todayScore.FlashColour(ScosuMenuStyle.VIOLET_INK, 900, Easing.OutQuint);
            }

            hasShown = true;

            todayDetails.Text = formatDetails(summary.Today, summary.Rank);

            week.Text = formatWeek(summary.Week, summary.Rank);

            // Starting from nothing gained, so a single play still draws an area.
            todayGraph.SetValues(summary.Today.Points.Select(p => (float)p.RankedScore).Prepend(0).ToArray());

            // What it takes to pass the next player is in the menu's bar, on both of its screens.

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

        private void hideContent()
        {
            hasShown = false;
            content.FadeOut(transition_duration, Easing.OutQuint);
        }

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
    }
}
