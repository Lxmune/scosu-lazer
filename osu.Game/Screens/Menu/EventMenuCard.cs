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
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.ScosuEvent;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The scosu! event's card: its countdown, the local user's rank and objectives. Opens the <see cref="ScosuEventOverlay"/>.
    /// Hidden while logged out, while there is no event, or when the event's `look.menu_card` is off.
    /// </summary>
    public partial class EventMenuCard : VisibilityContainer
    {
        private const float transition_duration = 500;
        private const float card_width = 300;
        private const float inner_width = card_width - 30;

        /// <summary>
        /// Where the event wants its card (the main menu moves it there).
        /// </summary>
        public IBindable<ScosuEventMenuPosition> MenuPosition => menuPosition;

        private readonly Bindable<ScosuEventMenuPosition> menuPosition = new Bindable<ScosuEventMenuPosition>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private ScosuEventOverlay? eventOverlay { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScosuEventRequest? request;
        private APIScosuEvent? shown;
        private ScheduledDelegate? ticker;

        private Container content = null!;
        private Box? background;
        private OsuSpriteText countdown = null!;

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
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh), true);
        }

        /// <summary>
        /// Fetches the event again, e.g. after a play.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have events.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online)
            {
                display(null);
                return;
            }

            var req = request = new GetScosuEventRequest();

            req.Success += response =>
            {
                if (req == request)
                    display(response.Event);
            };
            req.Failure += _ =>
            {
                if (req == request)
                    display(null);
            };

            api.Queue(req);
        }

        private void display(APIScosuEvent? ev)
        {
            ticker?.Cancel();
            ticker = null;

            if (ev == null || !ev.Appearance.MenuCard)
            {
                shown = null;
                content.FadeOut(transition_duration, Easing.OutQuint);
                return;
            }

            shown = ev;
            menuPosition.Value = ev.Appearance.MenuPosition;

            var accent = EventDisplay.Accent(ev);
            var accentText = EventDisplay.Lifted(accent);

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
                    Children = new Drawable[]
                    {
                        createHeader(ev, accentText),
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                            Padding = new MarginPadding { Horizontal = 15, Top = 8, Bottom = 12 },
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(8, 0),
                                    Children = new Drawable[]
                                    {
                                        heading(EventDisplay.CountdownLabel(ev).ToUpper()).With(d =>
                                        {
                                            d.Anchor = Anchor.BottomLeft;
                                            d.Origin = Anchor.BottomLeft;
                                            d.Margin = new MarginPadding { Bottom = 5 };
                                        }),
                                        countdown = new OsuSpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Font = OsuFont.GetFont(size: 26, weight: FontWeight.Light, fixedWidth: true),
                                        },
                                    },
                                },
                                new Box
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 1,
                                    Colour = Color4.White,
                                    Alpha = 0.12f,
                                    Margin = new MarginPadding { Vertical = 2 },
                                },
                                createStanding(ev),
                                createObjectives(ev, accent),
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Colour = accentText,
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = ScosuEventStrings.Open,
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
                    },
                },
            };

            updateCountdown();
            ticker = Scheduler.AddDelayed(updateCountdown, 1000, true);

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

        private Drawable createHeader(APIScosuEvent ev, Color4 accentText)
        {
            var header = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            if (ev.Appearance.MenuImageUrl is string image)
                header.Add(EventDisplay.Picture(image, ev.Appearance.MenuDim));

            header.Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
                Padding = new MarginPadding { Horizontal = 15, Top = 12, Bottom = 10 },
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = ScosuEventStrings.Label.ToUpper(),
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                Colour = accentText,
                            },
                            EventDisplay.StatusPill(ev).With(d =>
                            {
                                d.Anchor = Anchor.CentreRight;
                                d.Origin = Anchor.CentreRight;
                            }),
                        },
                    },
                    new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = ev.Title,
                    },
                },
            });

            return header;
        }

        private Drawable createStanding(APIScosuEvent ev)
        {
            var me = ev.Me;

            Drawable rank = me?.Rank is int position
                ? new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = LocalisableString.Interpolate($@"#{position.ToLocalisableString(@"N0")}"),
                            Font = OsuFont.GetFont(size: 18),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = ScosuEventStrings.Of(ev.PlayerCount.ToLocalisableString(@"N0")),
                            Font = OsuFont.GetFont(size: 12),
                            Colour = colours.GrayC,
                            Margin = new MarginPadding { Bottom = 2 },
                        },
                    },
                }
                : new OsuSpriteText
                {
                    Text = ScosuEventStrings.NotRanked,
                    Font = OsuFont.GetFont(size: 14),
                    Margin = new MarginPadding { Top = 3 },
                };

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new[]
                    {
                        stat(ScosuEventStrings.YourRank.ToUpper(), rank),
                        stat(ScosuEventStrings.Gained.ToUpper(), new OsuSpriteText
                        {
                            Text = (me?.RankedScore ?? 0).ToLocalisableString(@"N0"),
                            Font = OsuFont.GetFont(size: 18, fixedWidth: true),
                        }),
                    },
                },
            };
        }

        private Drawable createObjectives(APIScosuEvent ev, Color4 accent)
        {
            var objectives = ev.Objectives;
            var progress = ev.Me?.Objectives;

            if (objectives.Count == 0 || progress == null)
                return Empty();

            int completed = progress.Count(p => p.Done);
            float segmentWidth = (inner_width - 4 * (objectives.Count - 1)) / objectives.Count;

            int next = progress.FindIndex(p => !p.Done);
            var nextText = next >= 0
                ? ScosuEventStrings.NextObjective(objectives[next].Label, $@"{progress[next].Text} / {objectives[next].TargetText}")
                : ScosuEventStrings.AllObjectivesDone;

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
                Margin = new MarginPadding { Top = 2 },
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            heading(ScosuEventStrings.Objectives.ToUpper()),
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Text = $@"{completed} / {objectives.Count}",
                                Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                            },
                        },
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(4, 0),
                        ChildrenEnumerable = progress.Take(objectives.Count).Select(p => new CircularContainer
                        {
                            Width = segmentWidth,
                            Height = 5,
                            Masking = true,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = p.Done ? accent : Color4.White.Opacity(0.14f),
                            },
                        }),
                    },
                    new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 12))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = nextText,
                        Colour = colours.GrayC,
                    },
                },
            };
        }

        private Drawable stat(LocalisableString title, Drawable value) => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Children = new Drawable[] { heading(title), value },
        };

        private OsuSpriteText heading(LocalisableString text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
            Colour = colours.Yellow,
        };

        private void updateCountdown()
        {
            if (shown == null)
                return;

            if (EventDisplay.CountdownTarget(shown) is not DateTimeOffset target)
            {
                countdown.Text = EventDisplay.Day(shown.EndsAt);
                return;
            }

            var left = target - DateTimeOffset.Now;
            countdown.Text = EventDisplay.ShortCountdown(left);

            // The event just started or ended: the server knows what comes next.
            if (left <= TimeSpan.Zero)
            {
                ticker?.Cancel();
                Scheduler.AddDelayed(Refresh, 5000);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            background?.FadeTo(0.55f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background?.FadeTo(0.4f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (shown == null || content.Alpha == 0)
                return false;

            eventOverlay?.Show();
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
