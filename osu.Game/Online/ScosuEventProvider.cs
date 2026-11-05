// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Threading;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Notifications.WebSocket;
using osu.Game.Overlays.ScosuEvent;

namespace osu.Game.Online
{
    /// <summary>
    /// The scosu! event, fetched once for the whole game: the main menu's card and the event's page both show it.
    /// </summary>
    /// <remarks>
    /// It is fetched again when the API connects, when <see cref="Refresh"/> is called (after a play, when the page opens),
    /// when its countdown ends, and when the server says the event changed (<c>scosu.event.updated</c> on the notification websocket).
    /// </remarks>
    public partial class ScosuEventProvider : Component
    {
        /// <summary>
        /// The server's message when the event started, ended or was changed by an admin.
        /// </summary>
        public const string UPDATED_MESSAGE = @"scosu.event.updated";

        /// <summary>
        /// Every game hears <see cref="UPDATED_MESSAGE"/> at once: each waits up to this long before fetching, to spread the requests.
        /// </summary>
        private const double max_push_delay = 3000;

        /// <summary>
        /// How long after its countdown ends to fetch the event, giving the server time to start or finish it.
        /// </summary>
        private const double countdown_end_delay = 5000;

        /// <summary>
        /// The event, or <c>null</c> when there is none (or the server isn't a scosu server, or the game is offline).
        /// A failed fetch keeps the previous value.
        /// </summary>
        public IBindable<APIScosuEvent?> Event => current;

        /// <summary>
        /// Whether a fetch is under way.
        /// </summary>
        public IBindable<bool> Fetching => fetching;

        private readonly Bindable<APIScosuEvent?> current = new Bindable<APIScosuEvent?>();
        private readonly BindableBool fetching = new BindableBool();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScosuEventRequest? request;
        private ScheduledDelegate? countdownEnd;

        [BackgroundDependencyLoader]
        private void load()
        {
            api.NotificationsClient.MessageReceived += onMessageReceived;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh), true);
        }

        /// <summary>
        /// Fetches the event again.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void onMessageReceived(SocketMessage message)
        {
            if (message.Event == UPDATED_MESSAGE)
                Schedule(() => Scheduler.AddDelayed(Refresh, Random.Shared.NextDouble() * max_push_delay));
        }

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have events.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online)
            {
                set(null);
                fetching.Value = false;
                return;
            }

            var req = request = new GetScosuEventRequest();

            req.Success += response =>
            {
                if (req != request)
                    return;

                // The event first, so that watchers of both see it once the fetch is over.
                set(response.Event);
                fetching.Value = false;
            };
            req.Failure += _ =>
            {
                if (req == request)
                    fetching.Value = false;
            };

            fetching.Value = true;
            api.Queue(req);
        }

        private void set(APIScosuEvent? ev)
        {
            current.Value = ev;

            countdownEnd?.Cancel();
            countdownEnd = null;

            // The event starts or ends then: the server knows what comes next.
            if (ev != null && EventDisplay.CountdownTarget(ev) is DateTimeOffset target)
            {
                double untilEnd = Math.Max(0, (target - DateTimeOffset.Now).TotalMilliseconds);
                countdownEnd = Scheduler.AddDelayed(Refresh, untilEnd + countdown_end_delay);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            request = null;

            if (api.IsNotNull())
                api.NotificationsClient.MessageReceived -= onMessageReceived;

            base.Dispose(isDisposing);
        }
    }
}
