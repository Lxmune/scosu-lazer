// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Framework.Localisation;
using osu.Game.Online;
using osu.Game.Online.Notifications.WebSocket.Events;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class TestScosuServerNotifierParsing
    {
        private static NewPrivateNotificationEvent create(string name, object details, ulong objectId = 0)
            => new NewPrivateNotificationEvent { Name = name, ObjectId = objectId, Details = JObject.FromObject(details) };

        private static string text(ScosuServerNotifier.ParsedNotification parsed) => parsed.Text.ToString();

        [Test]
        public void TestMapRequestAccepted()
        {
            var parsed = ScosuServerNotifier.Parse(create(@"map_request_decided", new { title = "Artist - Title", status = "ranked", reason = "", request_id = 4 }, 1234));

            Assert.That(parsed, Is.Not.Null);
            Assert.That(text(parsed!), Is.EqualTo("Your map request for Artist - Title was ranked"));
            Assert.That(parsed!.BeatmapSetId, Is.EqualTo(1234));
        }

        [Test]
        public void TestMapRequestDeclined()
        {
            var parsed = ScosuServerNotifier.Parse(create(@"map_request_decided", new { title = "Artist - Title", status = "declined", reason = "Too short.", request_id = 4 }, 1234));

            Assert.That(text(parsed!), Is.EqualTo("Your map request for Artist - Title was declined: Too short."));
            Assert.That(parsed!.BeatmapSetId, Is.Null);
            Assert.That(parsed.WebsitePath, Is.EqualTo("/map-requests?tab=mine"));
        }

        [Test]
        public void TestWeeklyReport()
        {
            var parsed = ScosuServerNotifier.Parse(create(@"weekly_report_ready", new { week_start = "2026-11-16" }, 9));

            Assert.That(text(parsed!), Is.EqualTo("Your weekly report is ready"));
            Assert.That(parsed!.WebsitePath, Is.EqualTo("/weekly-reports/2026-11-16"));
        }

        [Test]
        public void TestAppeal()
        {
            var accepted = ScosuServerNotifier.Parse(create(@"appeal_decided", new { appeal_id = 1, status = "accepted", reason = "Sorry." }));
            var denied = ScosuServerNotifier.Parse(create(@"appeal_decided", new { appeal_id = 1, status = "denied", reason = "Aim assist." }));

            Assert.That(text(accepted!), Is.EqualTo("Your restriction appeal was accepted"));
            Assert.That(text(denied!), Is.EqualTo("Your restriction appeal was denied: Aim assist."));
            Assert.That(denied!.WebsitePath, Is.EqualTo("/appeal"));
        }

        [Test]
        public void TestUnknownKindIgnored()
        {
            Assert.That(ScosuServerNotifier.Parse(create(@"user_achievement_unlock", new { })), Is.Null);
        }
    }
}
