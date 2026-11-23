// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.Metadata;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Toolbar
{
    /// <summary>
    /// scosu: compact server connection indicator in the toolbar showing online status, live player count, and API latency.
    /// </summary>
    public partial class ToolbarServerStatus : OsuClickableContainer
    {
        private static readonly Color4 online_colour = Color4Extensions.FromHex(@"6ee39a");
        private static readonly Color4 warning_colour = Color4Extensions.FromHex(@"f6b94c");
        private static readonly Color4 offline_colour = Color4Extensions.FromHex(@"7a7689");

        private const double ping_interval_ms = 60_000;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        [Resolved]
        private DashboardOverlay? dashboard { get; set; }

        [Resolved]
        private LoginOverlay? login { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly IBindableDictionary<int, UserPresence> presences = new BindableDictionary<int, UserPresence>();

        private Box hoverBackground = null!;
        private Box flashBackground = null!;
        private Circle statusDot = null!;
        private OsuSpriteText label = null!;

        private ScheduledDelegate? pingSchedule;
        private GetScosuClientVersionRequest? activePingRequest;
        private IDisposable? presenceWatch;
        private int? lastPingMs;

        public ToolbarServerStatus()
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Padding = new MarginPadding(ToolbarButton.PADDING),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 6,
                            CornerExponent = 3f,
                            Children = new Drawable[]
                            {
                                hoverBackground = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = OsuColour.Gray(80).Opacity(180),
                                    Blending = BlendingParameters.Additive,
                                    Alpha = 0,
                                },
                                flashBackground = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    Colour = Color4.White.Opacity(100),
                                    Blending = BlendingParameters.Additive,
                                },
                            }
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6, 0),
                            Padding = new MarginPadding { Horizontal = 10 },
                            Children = new Drawable[]
                            {
                                statusDot = new Circle
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(7),
                                    Colour = offline_colour,
                                },
                                label = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold, fixedWidth: true),
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

            apiState.BindTo(api.State);
            apiState.BindValueChanged(state => Schedule(() => onApiStateChanged(state.NewValue)), true);

            if (metadataClient != null)
            {
                presences.BindTo(metadataClient.UserPresences);
                presences.BindCollectionChanged((_, _) => Schedule(updateDisplay), true);
            }
        }

        private void onApiStateChanged(APIState state)
        {
            pingSchedule?.Cancel();
            pingSchedule = null;
            activePingRequest?.Cancel();
            activePingRequest = null;

            if (state == APIState.Online)
            {
                presenceWatch ??= metadataClient?.BeginWatchingUserPresence();
                measurePing();
                pingSchedule = Scheduler.AddDelayed(measurePing, ping_interval_ms, true);
            }
            else
            {
                presenceWatch?.Dispose();
                presenceWatch = null;
                lastPingMs = null;
            }

            updateDisplay();
        }

        private void measurePing()
        {
            if (api.State.Value != APIState.Online || !api.Endpoints.RanksByStableScore)
                return;

            activePingRequest?.Cancel();

            var stopwatch = Stopwatch.StartNew();
            var request = new GetScosuClientVersionRequest(string.Empty);
            activePingRequest = request;

            request.Success += _ =>
            {
                stopwatch.Stop();
                Schedule(() =>
                {
                    if (activePingRequest != request)
                        return;

                    activePingRequest = null;
                    lastPingMs = Math.Max(1, (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds));
                    updateDisplay();
                });
            };

            request.Failure += _ => Schedule(() =>
            {
                if (activePingRequest != request)
                    return;

                activePingRequest = null;
                lastPingMs = null;
                updateDisplay();
            });

            api.Queue(request);
        }

        private void updateDisplay()
        {
            string host = getHostName();

            switch (apiState.Value)
            {
                case APIState.Online:
                {
                    int onlineCount = getOnlineCount();
                    statusDot.FadeColour(online_colour, 200, Easing.OutQuint);
                    label.Colour = Color4.White;
                    label.Text = FormatOnlineLabel(onlineCount, lastPingMs);
                    TooltipText = lastPingMs is int ping
                        ? $@"{host} · {onlineCount} online · {ping} ms"
                        : $@"{host} · {onlineCount} online";
                    break;
                }

                case APIState.Connecting:
                case APIState.RequiresSecondFactorAuth:
                    statusDot.FadeColour(warning_colour, 200, Easing.OutQuint);
                    label.Colour = warning_colour;
                    label.Text = @"...";
                    TooltipText = $@"{host} · {ScosuMenuStrings.ServerStatusConnecting}";
                    break;

                case APIState.Failing:
                    statusDot.FadeColour(warning_colour, 200, Easing.OutQuint);
                    label.Colour = warning_colour;
                    label.Text = ScosuMenuStrings.ServerStatusUnreachable;
                    TooltipText = ScosuMenuStrings.ServerUnreachable;
                    break;

                default:
                    statusDot.FadeColour(offline_colour, 200, Easing.OutQuint);
                    label.Colour = OsuColour.Gray(0.65f);
                    label.Text = ScosuMenuStrings.ServerStatusOffline;
                    TooltipText = $@"{host} · {ScosuMenuStrings.ServerStatusOffline}";
                    break;
            }
        }

        private int getOnlineCount()
        {
            int count = presences.Count;
            int localId = api.LocalUser.Value.OnlineID;

            if (localId > 1 && !presences.ContainsKey(localId))
                count++;

            return Math.Max(count, 1);
        }

        private string getHostName()
        {
            return Uri.TryCreate(api.Endpoints.WebsiteUrl, UriKind.Absolute, out var uri)
                ? uri.Host
                : api.Endpoints.WebsiteUrl;
        }

        internal static string FormatOnlineLabel(int onlineCount, int? pingMs)
            => pingMs is int ping ? $@"{onlineCount} · {ping}ms" : onlineCount.ToString();

        protected override bool OnClick(ClickEvent e)
        {
            flashBackground.FadeOutFromOne(800, Easing.OutQuint);

            if (apiState.Value == APIState.Online)
                dashboard?.Show();
            else
                login?.Show();

            return base.OnClick(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverBackground.FadeIn(200);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverBackground.FadeOut(200);
            base.OnHoverLost(e);
        }

        protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverClickSounds(sampleSet);

        protected override void Dispose(bool isDisposing)
        {
            pingSchedule?.Cancel();
            activePingRequest?.Cancel();
            presenceWatch?.Dispose();
            presenceWatch = null;
            base.Dispose(isDisposing);
        }
    }
}
