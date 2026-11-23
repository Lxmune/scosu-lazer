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
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the login briefing, once per game session and user, on the main menu: what changed since the previous session
    /// (ranks, ranked score, first places lost, unread messages) and today's daily tasks. Nothing shows when nothing changed.
    /// While the user is restricted or silenced it says so, with the reason, until when, and a link to the website (also as a
    /// notification, which stays in the notification overlay). Click or dismiss to hide it.
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

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetBriefingRequest? request;

        /// <summary>
        /// Where clicking the card goes (the appeal or standing page), or null to just hide it.
        /// </summary>
        private string? clickUrl;

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

            clickUrl = standing != null ? standingUrl(briefing.Standing!) : null;

            var body = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 3),
            };

            if (standing != null)
            {
                body.Add(new OsuTextFlowContainer(t =>
                {
                    t.Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold);
                    t.Colour = warning_colour;
                })
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = standing.Value,
                });
            }

            foreach (var line in lines)
            {
                body.Add(new OsuTextFlowContainer(t => t.Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = line,
                });
            }

            if (briefing.DailyTasks is APIBriefing.TasksSummary tasks && tasks.Total > 0)
            {
                body.Add(new OsuSpriteText
                {
                    Text = ScosuDailyStrings.TasksSummary(tasks.CompletedCount, tasks.Total, tasks.DailyStreak),
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
                    Spacing = new Vector2(0, 6),
                    Padding = new MarginPadding(ScosuMenuStyle.CARD_PADDING),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = ScosuDailyStrings.SinceLastTime.ToUpper(),
                            Font = ScosuMenuStyle.Eyebrow,
                            Spacing = new Vector2(1.5f, 0),
                            Colour = ScosuMenuStyle.VIOLET,
                        },
                        new OsuSpriteText
                        {
                            Text = ScosuDailyStrings.WelcomeBack,
                            Font = ScosuMenuStyle.Display(22),
                        },
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

            content.FadeIn(transition_duration, Easing.OutQuint);
        }

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
        private string standingUrl(APIBriefing.StandingNotice standing)
            => standing.Restricted && !string.IsNullOrEmpty(standing.AppealUrl) ? standing.AppealUrl : $@"{api.Endpoints.WebsiteUrl}/home/account/edit";

        /// <summary>
        /// Also posts the standing as a notification, so it can be read again after the card is gone.
        /// </summary>
        private void notifyStanding(APIBriefing.StandingNotice? standing)
        {
            if (notifications == null || standing == null || standingText(standing) is not LocalisableString text)
                return;

            string url = standingUrl(standing);

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
            clickUrl = null;
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
            if (content.Alpha == 0)
                return false;

            if (clickUrl != null)
                game?.OpenUrlExternally(clickUrl);

            hide();
            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
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
