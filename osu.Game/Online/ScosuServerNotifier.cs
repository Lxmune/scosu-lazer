// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.Notifications.WebSocket;
using osu.Game.Online.Notifications.WebSocket.Events;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Online
{
    /// <summary>
    /// Tells the player about the decisions scosu's server sends them: their map request (<c>map_request_decided</c>),
    /// their weekly report (<c>weekly_report_ready</c>) and their restriction appeal (<c>appeal_decided</c>).
    /// None of these exist in osu-web. Clicking one opens the matching page of the website.
    /// </summary>
    public partial class ScosuServerNotifier : Component
    {
        public const string MAP_REQUEST_DECIDED = @"map_request_decided";
        public const string WEEKLY_REPORT_READY = @"weekly_report_ready";
        public const string APPEAL_DECIDED = @"appeal_decided";

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
            if (data == null)
                return;

            var parsed = Parse(data);
            if (parsed == null)
                return;

            Schedule(() => notifications?.Post(new ScosuServerNotification(parsed, api.Endpoints.WebsiteUrl)));
        }

        /// <summary>
        /// Reads a server notification, or returns <c>null</c> if it isn't one of the kinds above.
        /// </summary>
        public static ParsedNotification? Parse(NewPrivateNotificationEvent data)
        {
            switch (data.Name)
            {
                case MAP_REQUEST_DECIDED:
                    var request = data.Details?.ToObject<MapRequestDecided>();
                    if (request == null)
                        return null;

                    if (request.Status == @"declined")
                        return new ParsedNotification(ScosuNotificationStrings.MapRequestDeclined(request.Title, request.Reason), FontAwesome.Solid.Times, @"/map-requests?tab=mine");

                    // An accepted request is a ranked or loved set, which the game can show itself.
                    return new ParsedNotification(ScosuNotificationStrings.MapRequestAccepted(request.Title, request.Status), FontAwesome.Solid.CheckCircle, @"/map-requests?tab=mine", (int)data.ObjectId);

                case WEEKLY_REPORT_READY:
                    var report = data.Details?.ToObject<WeeklyReportReady>();
                    if (report == null)
                        return null;

                    return new ParsedNotification(ScosuNotificationStrings.WeeklyReportReady, FontAwesome.Solid.ChartBar, $@"/weekly-reports/{report.WeekStart}");

                case APPEAL_DECIDED:
                    var appeal = data.Details?.ToObject<AppealDecided>();
                    if (appeal == null)
                        return null;

                    return new ParsedNotification(appeal.Status == @"accepted" ? ScosuNotificationStrings.AppealAccepted : ScosuNotificationStrings.AppealDenied(appeal.Reason), FontAwesome.Solid.Gavel, @"/appeal");

                default:
                    return null;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (api.IsNotNull())
                api.NotificationsClient.MessageReceived -= onMessageReceived;
        }

        /// <param name="Text">What the notification says.</param>
        /// <param name="Icon">Its icon.</param>
        /// <param name="WebsitePath">The page of the website a click opens, relative to its address.</param>
        /// <param name="BeatmapSetId">If set, a click shows this beatmap set in the game instead.</param>
        public record ParsedNotification(LocalisableString Text, IconUsage Icon, string WebsitePath, int? BeatmapSetId = null);

        public class MapRequestDecided
        {
            [JsonProperty("title")]
            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// <c>ranked</c>, <c>loved</c> or <c>declined</c>.
            /// </summary>
            [JsonProperty("status")]
            public string Status { get; set; } = string.Empty;

            [JsonProperty("reason")]
            public string Reason { get; set; } = string.Empty;

            [JsonProperty("request_id")]
            public int RequestId { get; set; }
        }

        public class WeeklyReportReady
        {
            /// <summary>
            /// The Monday of the report's week, as <c>yyyy-MM-dd</c>.
            /// </summary>
            [JsonProperty("week_start")]
            public string WeekStart { get; set; } = string.Empty;
        }

        public class AppealDecided
        {
            [JsonProperty("appeal_id")]
            public int AppealId { get; set; }

            /// <summary>
            /// <c>accepted</c> or <c>denied</c>.
            /// </summary>
            [JsonProperty("status")]
            public string Status { get; set; } = string.Empty;

            /// <summary>
            /// The staff's reply.
            /// </summary>
            [JsonProperty("reason")]
            public string Reason { get; set; } = string.Empty;
        }

        public partial class ScosuServerNotification : SimpleNotification
        {
            private readonly ParsedNotification parsed;
            private readonly string websiteUrl;

            public ScosuServerNotification(ParsedNotification parsed, string websiteUrl)
            {
                this.parsed = parsed;
                this.websiteUrl = websiteUrl;

                Text = parsed.Text;
            }

            [BackgroundDependencyLoader]
            private void load(OsuGame? game, INotificationOverlay notificationOverlay)
            {
                Icon = parsed.Icon;

                Activated = () =>
                {
                    notificationOverlay.Hide();

                    if (parsed.BeatmapSetId is int setId)
                        game?.ShowBeatmapSet(setId);
                    else
                        game?.OpenUrlExternally($@"{websiteUrl}{parsed.WebsitePath}");

                    return true;
                };
            }
        }
    }
}
