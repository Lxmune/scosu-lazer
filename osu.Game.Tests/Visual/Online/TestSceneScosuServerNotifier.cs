// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.Notifications.WebSocket;
using osu.Game.Online.Notifications.WebSocket.Events;
using osu.Game.Overlays;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneScosuServerNotifier : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private NotificationOverlay notificationOverlay = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create notifier", () =>
            {
                notificationOverlay = new NotificationOverlay
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                };

                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = [(typeof(INotificationOverlay), notificationOverlay)],
                    Children = new Drawable[]
                    {
                        notificationOverlay,
                        new ScosuServerNotifier(),
                    },
                };
            });
        }

        [Test]
        public void TestMapRequestAccepted()
        {
            receive(@"map_request_decided", new { title = "Artist - Title", status = "loved", reason = "", request_id = 1 });
            AddUntilStep("notification posted", () => notifications().Single().Text.ToString(), () => Is.EqualTo("Your map request for Artist - Title was loved"));
        }

        [Test]
        public void TestMapRequestDeclined()
        {
            receive(@"map_request_decided", new { title = "Artist - Title", status = "declined", reason = "Too short.", request_id = 1 });
            AddUntilStep("notification posted", () => notifications().Single().Text.ToString(), () => Is.EqualTo("Your map request for Artist - Title was declined: Too short."));
        }

        [Test]
        public void TestWeeklyReport()
        {
            receive(@"weekly_report_ready", new { week_start = "2026-11-16" });
            AddUntilStep("notification posted", () => notifications().Single().Text.ToString(), () => Is.EqualTo("Your weekly report is ready"));
        }

        [Test]
        public void TestAppealDenied()
        {
            receive(@"appeal_decided", new { appeal_id = 1, status = "denied", reason = "Aim assist." });
            AddUntilStep("notification posted", () => notifications().Single().Text.ToString(), () => Is.EqualTo("Your restriction appeal was denied: Aim assist."));
        }

        [Test]
        public void TestOtherNotificationsIgnored()
        {
            receive(@"user_achievement_unlock", new { });
            AddWaitStep("wait", 5);
            AddAssert("nothing posted", () => !notifications().Any());
        }

        private ScosuServerNotifier.ScosuServerNotification[] notifications()
            => notificationOverlay.ChildrenOfType<ScosuServerNotifier.ScosuServerNotification>().ToArray();

        private void receive(string name, object details) => AddStep($"receive {name}", () => dummyAPI.NotificationsClient.Receive(new SocketMessage
        {
            Event = @"new",
            Data = JObject.FromObject(new NewPrivateNotificationEvent
            {
                Name = name,
                ObjectId = 5,
                Details = JObject.FromObject(details),
            }),
        }));
    }
}
