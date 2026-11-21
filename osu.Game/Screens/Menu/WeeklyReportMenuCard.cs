// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The local user's latest weekly report, when they haven't seen it: a few of its highlights. Opens the report on the website.
    /// Hidden while logged out, on servers without reports, when there is no recent report, and once the report is opened or dismissed
    /// (see <see cref="OsuSetting.ScosuSeenWeeklyReport"/>).
    /// </summary>
    public partial class WeeklyReportMenuCard : VisibilityContainer
    {
        private const float transition_duration = 500;
        private const float card_width = 300;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly Bindable<string> seenWeek = new Bindable<string>();

        private GetWeeklyReportsRequest? request;
        private APIWeeklyReport? shown;

        private Container content = null!;
        private Box background = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            // The margin is on the content, so that a hidden card leaves no gap in the menu's column.
            InternalChild = content = new Container
            {
                Width = card_width,
                AutoSizeAxes = Axes.Y,
                Alpha = 0,
                Margin = new MarginPadding { Horizontal = 10, Top = 10 },
                Masking = true,
                CornerExponent = 2.5f,
                CornerRadius = 15,
            };

            config.BindWith(OsuSetting.ScosuSeenWeeklyReport, seenWeek);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            seenWeek.BindValueChanged(_ => Scheduler.AddOnce(Refresh));
            Refresh();
        }

        /// <summary>
        /// Fetches the reports again, e.g. after a play.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have reports.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online)
            {
                display(null);
                return;
            }

            var req = request = new GetWeeklyReportsRequest(api.LocalUser.Value.Id);

            req.Success += reports =>
            {
                if (req == request)
                    display(ScosuMenuFormatting.FindUnseenReport(reports, seenWeek.Value, DateTimeOffset.UtcNow));
            };
            req.Failure += _ =>
            {
                if (req == request)
                    display(null);
            };

            api.Queue(req);
        }

        private void display(APIWeeklyReport? report)
        {
            if (report == null)
            {
                shown = null;
                content.FadeOut(transition_duration, Easing.OutQuint);
                return;
            }

            // Nothing to redraw: the same report, already on screen.
            if (shown?.WeekStart == report.WeekStart && content.Alpha > 0)
                return;

            shown = report;

            var highlights = ScosuMenuFormatting.PickHighlights(report);

            content.Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.4f,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 4),
                    Padding = new MarginPadding { Horizontal = 15, Vertical = 12 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = ScosuMenuStrings.WeeklyReportLabel.ToUpper(),
                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                            Colour = colours.Yellow,
                        },
                        new OsuSpriteText
                        {
                            Text = ScosuMenuStrings.WeeklyReportTitle,
                            Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold),
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Margin = new MarginPadding { Top = 2 },
                            ChildrenEnumerable = highlights.Select(h => new OsuSpriteText
                            {
                                Text = ScosuMenuFormatting.HighlightText(h),
                                Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                Colour = h.Change == @"worse" ? colours.Orange1 : Color4.White,
                            }),
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(4, 0),
                            Margin = new MarginPadding { Top = 4 },
                            Colour = colours.Yellow,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Text = ScosuMenuStrings.OpenReport,
                                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                },
                                new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = FontAwesome.Solid.ChevronRight,
                                    Size = new Vector2(9),
                                },
                            },
                        },
                    },
                },
                new DismissButton(dismiss)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding(6),
                },
            };

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

        /// <summary>
        /// Marks the shown report as seen, which hides the card.
        /// </summary>
        private void dismiss()
        {
            if (shown == null)
                return;

            if (string.CompareOrdinal(shown.WeekStart, seenWeek.Value) > 0)
                seenWeek.Value = shown.WeekStart;

            shown = null;
            content.FadeOut(transition_duration, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeTo(0.55f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeTo(0.4f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (shown == null || content.Alpha == 0)
                return false;

            string week = shown.WeekStart;

            game?.OpenUrlExternally($@"{api.Endpoints.WebsiteUrl}/weekly-reports/{week}");
            dismiss();
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

        private partial class DismissButton : OsuClickableContainer
        {
            public DismissButton(Action action)
            {
                Action = action;
                Size = new Vector2(16);
                TooltipText = ScosuMenuStrings.Dismiss;

                Child = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = FontAwesome.Solid.Times,
                    Size = new Vector2(10),
                };
            }
        }
    }
}
