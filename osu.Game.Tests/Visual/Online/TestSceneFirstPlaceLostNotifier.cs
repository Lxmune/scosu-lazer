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
    public partial class TestSceneFirstPlaceLostNotifier : OsuTestScene
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
                        new FirstPlaceLostNotifier(),
                    },
                };
            });
        }

        [Test]
        public void TestFirstPlaceLost()
        {
            receive(@"beatmap_first_place_lost");
            AddUntilStep("notification posted", () => notifications().Single().Text.ToString(),
                () => Is.EqualTo("peppy took your #1 on Artist - Title [Insane]"));
        }

        [Test]
        public void TestOtherNotificationsIgnored()
        {
            receive(@"user_achievement_unlock");
            AddWaitStep("wait", 5);
            AddAssert("nothing posted", () => !notifications().Any());
        }

        private FirstPlaceLostNotifier.FirstPlaceLostNotification[] notifications()
            => notificationOverlay.ChildrenOfType<FirstPlaceLostNotifier.FirstPlaceLostNotification>().ToArray();

        private void receive(string name) => AddStep($"receive {name}", () => dummyAPI.NotificationsClient.Receive(new SocketMessage
        {
            Event = @"new",
            Data = JObject.FromObject(new NewPrivateNotificationEvent
            {
                Name = name,
                SourceUserID = 2,
                Details = JObject.FromObject(new FirstPlaceLostNotifier.FirstPlaceLost
                {
                    Username = "peppy",
                    BeatmapId = 75,
                    Title = "Artist - Title [Insane]",
                }),
            }),
        }));
    }
}
