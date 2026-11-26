// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
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
using osu.Framework.Utils;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the login briefing, once per game session and user, on the main menu: what changed since the previous session
    /// (ranks, ranked score, first places lost, unread messages) and today's daily tasks. Nothing shows when nothing changed.
    /// While the user is restricted or silenced it says so, with the reason, until when, and a link to the website (also as a
    /// notification, which stays in the notification overlay).
    /// It opens with a greeting for the time of day (see <see cref="MenuGreeting"/>).
    /// Each line leads to what it is about: the ranks and score to the profile, a lost first place to its beatmap, unread messages
    /// to the chat; the daily tasks line lists today's tasks under it. Clicking the rest of the card shows the profile; the cross hides it.
    /// The server starts a new session when asked (see <see cref="GetBriefingRequest"/>), so it is asked once per game run.
    /// </summary>
    public partial class BriefingMenuCard : CornerInfoCard
    {
        private const float transition_duration = 500;

        private static readonly Color4 warning_colour = Color4Extensions.FromHex(@"ff6b7e");

        /// <summary>
        /// The users already briefed since the game started.
        /// </summary>
        private static readonly HashSet<int> briefed = new HashSet<int>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

        private GetBriefingRequest? request;
        private OsuSpriteText? eyebrowText;

        /// <summary>
        /// Where clicking the card goes while the account is restricted or silenced (the appeal or standing page); otherwise the profile.
        /// </summary>
        private string? standingUrl;

        private bool shown;

        private Container content = null!;
        private DismissButton? dismissButton;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

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

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(fetch));
            accentHue.BindValueChanged(_ =>
            {
                if (eyebrowText != null)
                    eyebrowText.Colour = ScosuMenuStyle.VIOLET;
            });
            ruleset.BindValueChanged(_ => fetch(), true);
        }

        private void fetch()
        {
            // Only scosu servers have the briefing, and only rulesets they rank.
            if (request != null || api.State.Value != APIState.Online || !api.Endpoints.RanksByStableScore || !ruleset.Value.IsLegacyRuleset())
                return;

            int userId = api.LocalUser.Value.Id;

            if (!briefed.Add(userId))
                return;

            // Relax and Autopilot have their own leaderboards (see RelaxLeaderboards).
            var leaderboard = ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym));
            var req = request = new GetBriefingRequest(leaderboard);

            req.Success += briefing => Schedule(() =>
            {
                if (req != request)
                    return;

                request = null;
                display(briefing);
                notifyStanding(briefing.Standing);
            });
            req.Failure += _ => Schedule(() =>
            {
                if (req != request)
                    return;

                // Not briefed after all: the next time the API comes back online may try again.
                request = null;
                briefed.Remove(userId);
            });

            api.Queue(req);
        }

        private void display(APIBriefing briefing)
        {
            var lines = DailyTaskFormatting.ChangeLines(briefing);
            var standing = standingText(briefing.Standing);

            if (lines.Count == 0 && standing == null)
            {
                hide();
                return;
            }

            standingUrl = standing != null ? standingPage(briefing.Standing!) : null;

            var rows = new List<BriefingRow>();

            if (standing != null)
            {
                rows.Add(new BriefingRow(FontAwesome.Solid.Ban, standing.Value, warning_colour, ScosuDailyStrings.OpenOnWebsite,
                    () => game?.OpenUrlExternally(standingUrl!), warning_colour));
            }

            foreach (var change in lines)
            {
                var (icon, tooltip, action) = target(change);
                Color4 iconColour = change.Improved switch
                {
                    true => colours.Lime1,
                    false => warning_colour,
                    null => ScosuMenuStyle.VIOLET,
                };

                rows.Add(new BriefingRow(icon, change.Text, iconColour, tooltip, action));
            }

            var bodyChildren = new List<Drawable>(rows);

            if (briefing.DailyTasks is APIBriefing.TasksSummary tasks && tasks.Total > 0)
            {
                // Open from the start: the tasks are what the line is about (the user's choice).
                var taskList = new TaskList { OpenWhenLoaded = true };
                var tasksRow = new BriefingRow(FontAwesome.Solid.Tasks, ScosuDailyStrings.TasksSummary(tasks.CompletedCount, tasks.Total, tasks.DailyStreak),
                    tasks.DayComplete ? colours.Lime1 : ScosuMenuStyle.VIOLET, ScosuDailyStrings.ShowTasks, taskList.Toggle, ScosuMenuStyle.TEXT_DIM, expandable: true);

                taskList.Expanded.BindValueChanged(e => tasksRow.SetExpanded(e.NewValue, e.NewValue ? ScosuDailyStrings.HideTasks : ScosuDailyStrings.ShowTasks));

                rows.Add(tasksRow);
                bodyChildren.Add(tasksRow);
                bodyChildren.Add(taskList);
            }

            var body = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 1),
                // The rows' hover highlight reaches past the text, which stays aligned with the title.
                Margin = new MarginPadding { Horizontal = -BriefingRow.INSET },
                Children = bodyChildren,
            };

            var header = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    eyebrowText = new OsuSpriteText
                    {
                        Text = ScosuDailyStrings.SinceLastTime.ToUpper(),
                        Font = ScosuMenuStyle.Eyebrow,
                        Spacing = new Vector2(1.5f, 0),
                        Colour = ScosuMenuStyle.VIOLET,
                    },
                    new OsuTextFlowContainer(t => t.Font = ScosuMenuStyle.Display(22))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        // Room for the cross, which shows on hover.
                        Padding = new MarginPadding { Right = 16 },
                        Text = MenuGreeting.Pick(DateTimeOffset.Now, api.LocalUser.Value.Username, new Random()),
                    },
                }
            };

            if (briefing.Since is DateTimeOffset since)
            {
                header.Add(new OsuSpriteText
                {
                    Text = ScosuDailyStrings.LastPlayed(HumanizerUtils.Humanize(since)),
                    Font = ScosuMenuStyle.Body(12),
                    Colour = ScosuMenuStyle.TEXT_DIM,
                });
            }

            content.Children = new Drawable[]
            {
                CreateSurface(),
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 8),
                    Padding = new MarginPadding(ScosuMenuStyle.CARD_PADDING),
                    Children = new Drawable[]
                    {
                        header,
                        body,
                    },
                },
                // Only there while the card is hovered, see Update.
                dismissButton = new DismissButton(hide)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding(8),
                    Alpha = 0,
                },
            };

            shown = true;
            content.FadeIn(transition_duration, Easing.OutQuint);

            // The lines come in one after the other, once the card has slid in.
            for (int i = 0; i < rows.Count; i++)
                rows[i].Delay(150 + i * 60).FadeIn(400, Easing.OutQuint);
        }

        /// <summary>
        /// Where a change leads: its icon, what clicking it does, and that in a tooltip.
        /// </summary>
        private (IconUsage Icon, LocalisableString Tooltip, Action Action) target(DailyTaskFormatting.BriefingChange change)
        {
            switch (change.Kind)
            {
                case DailyTaskFormatting.BriefingChangeKind.GlobalRank:
                    return (FontAwesome.Solid.GlobeAmericas, ScosuDailyStrings.ShowYourProfile, showProfile);

                case DailyTaskFormatting.BriefingChangeKind.CountryRank:
                    return (FontAwesome.Solid.Flag, ScosuDailyStrings.ShowYourProfile, showProfile);

                case DailyTaskFormatting.BriefingChangeKind.RankedScore:
                    return (FontAwesome.Solid.ChartLine, ScosuDailyStrings.ShowYourProfile, showProfile);

                case DailyTaskFormatting.BriefingChangeKind.LostFirst when change.BeatmapId > 0:
                    return (FontAwesome.Solid.Crown, Hub.MenuHubStrings.OpenBeatmap, () => game?.ShowBeatmap((int)change.BeatmapId));

                default:
                    // The other first places lost are on the profile.
                    return (FontAwesome.Solid.Crown, ScosuDailyStrings.ShowYourProfile, showProfile);
            }
        }

        private void showProfile() => game?.ShowUser(api.LocalUser.Value);

        /// <summary>
        /// What the card says about the account's standing, or null in good standing.
        /// </summary>
        private static LocalisableString? standingText(APIBriefing.StandingNotice? standing)
        {
            if (standing == null)
                return null;

            if (standing.Restricted)
            {
                return string.IsNullOrWhiteSpace(standing.RestrictionReason)
                    ? ScosuDailyStrings.RestrictedNoReason
                    : ScosuDailyStrings.Restricted(standing.RestrictionReason);
            }

            if (standing.SilencedUntil is DateTimeOffset until)
            {
                string when = until.ToLocalTime().ToString(@"g", CultureInfo.CurrentCulture);

                return string.IsNullOrWhiteSpace(standing.SilenceReason)
                    ? ScosuDailyStrings.SilencedNoReason(when)
                    : ScosuDailyStrings.Silenced(when, standing.SilenceReason);
            }

            return null;
        }

        /// <summary>
        /// A restriction is appealed on the website's appeal page; a silence's details are in the account settings.
        /// </summary>
        private string standingPage(APIBriefing.StandingNotice standing)
            => standing.Restricted && !string.IsNullOrEmpty(standing.AppealUrl) ? standing.AppealUrl : $@"{api.Endpoints.WebsiteUrl}/home/account/edit";

        /// <summary>
        /// Also posts the standing as a notification, so it can be read again after the card is gone.
        /// </summary>
        private void notifyStanding(APIBriefing.StandingNotice? standing)
        {
            if (notifications == null || standing == null || standingText(standing) is not LocalisableString text)
                return;

            string url = standingPage(standing);

            notifications.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.Ban,
                Text = text,
                Activated = () =>
                {
                    game?.OpenUrlExternally(url);
                    return true;
                },
            });
        }

        private void hide()
        {
            shown = false;
            content.FadeOut(transition_duration, Easing.OutQuint);
        }

        protected override void Update()
        {
            base.Update();

            // The button's own hover takes the card's away, so both count.
            if (dismissButton != null)
            {
                float target = IsHovered || dismissButton.IsHovered ? 1 : 0;

                if (dismissButton.Alpha != target && !dismissButton.Transforms.Any())
                    dismissButton.FadeTo(target, 150, Easing.OutQuint);
            }
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!shown)
                return false;

            if (standingUrl != null)
                game?.OpenUrlExternally(standingUrl);
            else
                showProfile();

            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }

        /// <summary>
        /// One line of the briefing: an icon for what it is about (coloured by whether it got better or worse), the line, and an arrow
        /// on hover. Clicking it goes to what it is about, or, when expandable, opens something under it (then its arrow points down and stays).
        /// </summary>
        private partial class BriefingRow : OsuClickableContainer
        {
            /// <summary>
            /// How far the row's highlight reaches past its text on either side.
            /// </summary>
            public const float INSET = 6;

            private const float icon_column = 20;

            /// <summary>
            /// The arrow's alpha when it shows without hovering: a row that opens something under it always shows it.
            /// </summary>
            private const float expandable_arrow_alpha = 0.45f;

            private readonly Box highlight;
            private readonly SpriteIcon arrow;
            private readonly bool expandable;

            public BriefingRow(IconUsage icon, LocalisableString text, Color4 iconColour, LocalisableString tooltip, Action action, Color4? textColour = null,
                               bool expandable = false)
            {
                this.expandable = expandable;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 6;
                Alpha = 0;
                Action = action;
                TooltipText = tooltip;

                Children = new Drawable[]
                {
                    highlight = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    new SpriteIcon
                    {
                        // On the first line of the text, which may wrap.
                        Position = new Vector2(INSET + 1, 7),
                        Icon = icon,
                        Size = new Vector2(11),
                        Colour = iconColour,
                    },
                    new OsuTextFlowContainer(t =>
                    {
                        t.Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold);
                        if (textColour is Color4 colour)
                            t.Colour = colour;
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Left = INSET + icon_column, Right = INSET + 14, Vertical = 4 },
                        Text = text,
                    },
                    arrow = new SpriteIcon
                    {
                        // On the first line of the text, like the icon.
                        Anchor = expandable ? Anchor.TopRight : Anchor.CentreRight,
                        // Turns over about its centre when what the row opens opens.
                        Origin = expandable ? Anchor.Centre : Anchor.CentreRight,
                        Position = expandable ? new Vector2(-INSET - 4, 11) : new Vector2(-INSET + 4, 0),
                        Icon = expandable ? FontAwesome.Solid.ChevronDown : FontAwesome.Solid.ChevronRight,
                        Size = new Vector2(8),
                        Alpha = expandable ? expandable_arrow_alpha : 0,
                    },
                };
            }

            /// <summary>
            /// Shows whether what the row opens is open: the arrow turns over, and the tooltip says what clicking does now.
            /// </summary>
            public void SetExpanded(bool expanded, LocalisableString tooltip)
            {
                arrow.RotateTo(expanded ? 180 : 0, 300, Easing.OutQuint);
                TooltipText = tooltip;
            }

            protected override bool OnHover(HoverEvent e)
            {
                highlight.FadeTo(0.07f, 150, Easing.OutQuint);

                if (expandable)
                    arrow.FadeTo(0.9f, 150, Easing.OutQuint);
                else
                    arrow.FadeTo(0.7f, 150, Easing.OutQuint).MoveToX(-INSET, 200, Easing.OutQuint);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                highlight.FadeOut(250, Easing.OutQuint);

                if (expandable)
                    arrow.FadeTo(expandable_arrow_alpha, 250, Easing.OutQuint);
                else
                    arrow.FadeOut(250, Easing.OutQuint).MoveToX(-INSET + 4, 250, Easing.OutQuint);

                base.OnHoverLost(e);
            }
        }

        /// <summary>
        /// Today's daily tasks, under the briefing's daily tasks line: opens and closes when the line is clicked, and asks the server
        /// for the tasks the first time it opens.
        /// </summary>
        private partial class TaskList : Container
        {
            private const float transition_duration = 300;

            /// <summary>
            /// The room the spinner takes while the tasks load.
            /// </summary>
            private const float spinner_height = 32;

            public readonly BindableBool Expanded = new BindableBool();

            /// <summary>
            /// Whether the list opens (and fetches the tasks) as soon as it is shown.
            /// </summary>
            public bool OpenWhenLoaded { get; init; }

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            private readonly FillFlowContainer flow;
            private readonly LoadingSpinner spinner;

            private GetDailyTasksRequest? request;
            private bool fetched;

            public TaskList()
            {
                RelativeSizeAxes = Axes.X;
                // Not auto-sized: its height follows what it holds in Update, so opening and closing can't fight a transform.
                Height = 0;
                Masking = true;

                Children = new Drawable[]
                {
                    flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6),
                        // Under the line's text, as far in as it.
                        Padding = new MarginPadding { Left = BriefingRow.INSET + 20, Right = BriefingRow.INSET, Top = 4, Bottom = 8 },
                        Alpha = 0,
                    },
                    spinner = new LoadingSpinner
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(16),
                        Margin = new MarginPadding { Vertical = 8 },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                if (OpenWhenLoaded)
                    Toggle();
            }

            public void Toggle()
            {
                Expanded.Toggle();

                if (Expanded.Value)
                {
                    fetch();
                    flow.FadeIn(transition_duration, Easing.OutQuint);
                }
                else
                    flow.FadeOut(transition_duration / 2, Easing.OutQuint);
            }

            protected override void Update()
            {
                base.Update();

                float target = 0;

                if (Expanded.Value)
                    target = spinner.State.Value == Visibility.Visible ? spinner_height : flow.DrawHeight;

                Height = (float)Interpolation.DampContinuously(Height, target, transition_duration / 4, Time.Elapsed);
            }

            private void fetch()
            {
                if (fetched || request != null)
                    return;

                spinner.Show();

                var req = request = new GetDailyTasksRequest();
                req.Success += tasks =>
                {
                    request = null;
                    fetched = true;
                    show(tasks);
                };
                req.Failure += _ =>
                {
                    // Asked again the next time it opens.
                    request = null;
                    showMessage(ScosuDailyStrings.TasksUnavailable);
                };

                api.Queue(req);
            }

            private void show(APIDailyTasks tasks)
            {
                spinner.Hide();

                if (tasks.Tasks.Count == 0)
                {
                    showMessage(ScosuDailyStrings.TasksUnavailable);
                    return;
                }

                flow.Clear();

                foreach (var task in tasks.Tasks)
                    flow.Add(new DailyTaskRow(task));

                TimeSpan untilReset = tasks.ResetsAt - DateTimeOffset.Now;

                if (untilReset > TimeSpan.Zero)
                {
                    flow.Add(new OsuSpriteText
                    {
                        Text = ScosuDailyStrings.NewTasksIn(untilReset.TotalHours >= 1
                            ? $@"{(int)untilReset.TotalHours}h {untilReset.Minutes:00}m"
                            : $@"{Math.Max(1, untilReset.Minutes)}m"),
                        Font = ScosuMenuStyle.Body(11),
                        Colour = ScosuMenuStyle.TEXT_DIM,
                    });
                }

                for (int i = 0; i < flow.Count; i++)
                    flow[i].FadeInFromZero(300, Easing.OutQuint);
            }

            private void showMessage(LocalisableString message)
            {
                spinner.Hide();

                flow.Child = new OsuSpriteText
                {
                    Text = message,
                    Font = ScosuMenuStyle.Body(12),
                    Colour = ScosuMenuStyle.TEXT_DIM,
                };
            }

            protected override void Dispose(bool isDisposing)
            {
                request?.Cancel();
                base.Dispose(isDisposing);
            }
        }

        private partial class DismissButton : OsuClickableContainer
        {
            public DismissButton(Action action)
            {
                Action = action;
                Size = new Vector2(20);
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
