// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Notifications.WebSocket;
using osu.Game.Online.Notifications.WebSocket.Events;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Online
{
    /// <summary>
    /// Tells the player when someone takes their first place on a beatmap's leaderboard.
    /// This is scosu's <c>beatmap_first_place_lost</c> notification, which osu-web doesn't have.
    /// </summary>
    public partial class FirstPlaceLostNotifier : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            api.NotificationsClient.MessageReceived += onMessageReceived;
        }

        private void onMessageReceived(SocketMessage message)
        {
            if (message.Event != @"new")
                return;

            var data = message.Data?.ToObject<NewPrivateNotificationEvent>();
            if (data == null || data.Name != @"beatmap_first_place_lost")
                return;

            var details = data.Details?.ToObject<FirstPlaceLost>();
            if (details == null)
                return;

            var sniper = new APIUser
            {
                Id = (int)data.SourceUserID,
                Username = details.Username,
                AvatarUrl = details.AvatarUrl,
            };

            Schedule(() => notifications?.Post(new FirstPlaceLostNotification(sniper, details)));
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (api.IsNotNull())
                api.NotificationsClient.MessageReceived -= onMessageReceived;
        }

        public class FirstPlaceLost
        {
            [JsonProperty("username")]
            public string Username { get; set; } = string.Empty;

            [JsonProperty("avatar_url")]
            public string AvatarUrl { get; set; } = string.Empty;

            [JsonProperty("beatmap_id")]
            public int BeatmapId { get; set; }

            /// <summary>
            /// The beatmap as "Artist - Title [Difficulty]".
            /// </summary>
            [JsonProperty("title")]
            public string Title { get; set; } = string.Empty;
        }

        public partial class FirstPlaceLostNotification : UserAvatarNotification
        {
            private readonly int beatmapId;

            public FirstPlaceLostNotification(APIUser sniper, FirstPlaceLost details)
                : base(sniper, $"{sniper.Username} took your #1 on {details.Title}")
            {
                beatmapId = details.BeatmapId;
            }

            [BackgroundDependencyLoader]
            private void load(OsuGame? game, INotificationOverlay notificationOverlay)
            {
                Icon = FontAwesome.Solid.Crown;

                Activated = () =>
                {
                    notificationOverlay.Hide();
                    game?.ShowBeatmap(beatmapId);
                    return true;
                };
            }
        }
    }
}
