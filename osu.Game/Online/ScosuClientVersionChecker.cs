// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Updater;

namespace osu.Game.Online
{
    /// <summary>
    /// Tells the player when the scosu server no longer accepts plays from this build, as soon as the game connects
    /// rather than when their first play is refused.
    /// </summary>
    public partial class ScosuClientVersionChecker : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private UpdateManager? updateManager { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private bool notified;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(state =>
            {
                if (state.NewValue == APIState.Online)
                    Schedule(check);
            }, true);
        }

        private void check()
        {
            // Only scosu servers refuse old builds; once is enough for a session.
            if (notified || !api.Endpoints.RanksByStableScore)
                return;

            var req = new GetScosuClientVersionRequest(game.VersionHash);
            req.Success += response =>
            {
                if (response.Current || notified)
                    return;

                notified = true;
                notifications?.Post(new OutdatedClientNotification
                {
                    Activated = () =>
                    {
                        if (updateManager?.CanCheckForUpdate == true)
                            updateManager.CheckForUpdate();
                        return true;
                    },
                });
            };
            api.Queue(req);
        }

        public partial class OutdatedClientNotification : SimpleNotification
        {
            public OutdatedClientNotification()
            {
                Icon = FontAwesome.Solid.Download;
                Text = "This version of scosu! is outdated: your plays won't be submitted. Update the game to play online again.";
            }
        }
    }
}
