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
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.ScosuEvent;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The scosu event's card: its countdown, the local user's rank and objectives. Opens the <see cref="ScosuEventOverlay"/>.
    /// Hidden while logged out, while there is no event, or when the event's `look.menu_card` is off.
    /// </summary>
    public partial class EventMenuCard : CornerCard
    {
        private const float transition_duration = 500;
        private const float bar_width = 4;
        private const float inner_width = CARD_WIDTH - ScosuMenuStyle.CARD_PADDING * 2 - bar_width;

        /// <summary>
        /// Where the event wants its card (the main menu moves it there).
        /// </summary>
        public IBindable<ScosuEventMenuPosition> MenuPosition => menuPosition;

        private readonly Bindable<ScosuEventMenuPosition> menuPosition = new Bindable<ScosuEventMenuPosition>();

        [Resolved]
        private ScosuEventProvider? eventProvider { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private ScosuEventOverlay? eventOverlay { get; set; }

        private readonly IBindable<APIScosuEvent?> currentEvent = new Bindable<APIScosuEvent?>();

        private APIScosuEvent? shown;
        private ScheduledDelegate? ticker;

        private Container content = null!;
        private Box? background;
        private Drawable countBoxes = null!;
        private OsuSpriteText days = null!;
        private OsuSpriteText hours = null!;
        private OsuSpriteText minutes = null!;
        private OsuSpriteText endedOn = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            // The margin is on the content, so that a hidden card leaves no gap in the menu's column.
            InternalChild = Margined = content = new Container
            {
                Width = CARD_WIDTH,
                AutoSizeAxes = Axes.Y,
                Alpha = 0,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (eventProvider != null)
                currentEvent.BindTo(eventProvider.Event);
            currentEvent.BindValueChanged(e => display(e.NewValue), true);
        }

        /// <summary>
        /// Fetches the event again, e.g. after a play.
        /// </summary>
        public void Refresh() => eventProvider?.Refresh();

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
                ScosuMenuStyle.CreateGlassBackground(),
                // The picture, hover light and accent bar are clipped to the card's rounded corners.
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = ScosuMenuStyle.CORNER_RADIUS,
                    CornerExponent = 2.5f,
                    Children = new[]
                    {
                        ev.Appearance.MenuImageUrl is string image ? EventDisplay.Picture(image, ev.Appearance.MenuDim) : Empty(),
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ScosuMenuStyle.FILL,
                            Alpha = 0,
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = bar_width,
                            Colour = accent,
                        },
                    },
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 8),
                    Padding = new MarginPadding
                    {
                        Left = ScosuMenuStyle.CARD_PADDING + bar_width,
                        Vertical = ScosuMenuStyle.CARD_PADDING,
                        Right = ScosuMenuStyle.CARD_PADDING,
                    },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = LocalisableString.Interpolate($@"{ScosuEventStrings.Label.ToUpper()} · {EventDisplay.StatusText(ev.Status).ToUpper()}"),
                            Font = ScosuMenuStyle.Eyebrow,
                            Spacing = new Vector2(1.5f, 0),
                            Colour = accentText,
                        },
                        new OsuTextFlowContainer(t => t.Font = ScosuMenuStyle.Display(18))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = ev.Title,
                        },
                        createCountdown(),
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
                                    Font = ScosuMenuStyle.Body(12, FontWeight.SemiBold),
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
            };

            updateCountdown();
            ticker = Scheduler.AddDelayed(updateCountdown, 1000, true);

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

        /// <summary>
        /// The days, hours and minutes left in boxes, or the day the event ended on.
        /// </summary>
        private Drawable createCountdown() => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(6, 0),
            Children = new Drawable[]
            {
                countBoxes = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new[]
                    {
                        countBox(ScosuEventStrings.Days, out days),
                        countBox(ScosuEventStrings.Hours, out hours),
                        countBox(ScosuEventStrings.Minutes, out minutes),
                    },
                },
                endedOn = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Font = ScosuMenuStyle.Display(18),
                    Alpha = 0,
                },
            },
        };

        private static Drawable countBox(LocalisableString label, out OsuSpriteText number)
        {
            number = new OsuSpriteText
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Font = ScosuMenuStyle.Display(20),
            };

            return new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 54,
                Masking = true,
                CornerRadius = 6,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ScosuMenuStyle.FILL,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Vertical = 5 },
                        Children = new Drawable[]
                        {
                            number,
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = label.ToUpper(),
                                Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
                                Spacing = new Vector2(0.8f, 0),
                                Colour = ScosuMenuStyle.TEXT_DIM,
                            },
                        },
                    },
                },
            };
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
                            Font = ScosuMenuStyle.Display(16),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = ScosuEventStrings.Of(ev.PlayerCount.ToLocalisableString(@"N0")),
                            Font = OsuFont.GetFont(size: 12),
                            Colour = ScosuMenuStyle.TEXT_DIM,
                            Margin = new MarginPadding { Bottom = 2 },
                        },
                    },
                }
                : new OsuSpriteText
                {
                    Text = ScosuEventStrings.NotRanked,
                    Font = ScosuMenuStyle.Body(14),
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
                        Colour = ScosuMenuStyle.TEXT_DIM,
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
            Font = OsuFont.GetFont(size: 11, weight: FontWeight.SemiBold),
            Colour = ScosuMenuStyle.TEXT_DIM,
        };

        private void updateCountdown()
        {
            if (shown == null)
                return;

            if (EventDisplay.CountdownTarget(shown) is not DateTimeOffset target)
            {
                countBoxes.Alpha = 0;
                endedOn.Alpha = 1;
                endedOn.Text = ScosuEventStrings.EndedOn(EventDisplay.Day(shown.EndsAt));
                return;
            }

            var left = target - DateTimeOffset.Now;

            if (left < TimeSpan.Zero)
                left = TimeSpan.Zero;

            days.Text = ((int)left.TotalDays).ToString(@"0");
            hours.Text = left.Hours.ToString(@"00");
            minutes.Text = left.Minutes.ToString(@"00");

            // The event just started or ended: the provider fetches what comes next.
            if (left <= TimeSpan.Zero)
                ticker?.Cancel();
        }

        protected override bool OnHover(HoverEvent e)
        {
            background?.FadeTo(1f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background?.FadeTo(0f, 200, Easing.OutQuint);
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
    }
}
