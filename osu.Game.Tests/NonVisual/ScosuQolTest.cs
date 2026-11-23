// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Online.Chat;
using osu.Game.Overlays.Login;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ScosuQolTest
    {
        [Test]
        public void TestConfigDefaults()
        {
            using var storage = new TemporaryNativeStorage(nameof(ScosuQolTest));
            using var config = new OsuConfigManager(storage);

            Assert.That(config.Get<BeatmapDetailTab>(OsuSetting.BeatmapDetailTab), Is.EqualTo(BeatmapDetailTab.Global));
            Assert.That(config.Get<SortMode>(OsuSetting.SongSelectSortingMode), Is.EqualTo(SortMode.DateAdded));
            Assert.That(config.Get<bool>(OsuSetting.KeyOverlay), Is.True);
            Assert.That(config.Get<bool>(OsuSetting.SaveUsername), Is.True);
            Assert.That(config.Get<bool>(OsuSetting.SavePassword), Is.True);
        }

        [Test]
        public void TestRelaxLeaderboardMemory()
        {
            string stored = string.Empty;

            stored = RelaxLeaderboardMemory.Remember(stored, @"osu", @"RX");
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"osu"), Is.EqualTo(@"RX"));
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"taiko"), Is.Null);

            stored = RelaxLeaderboardMemory.Remember(stored, @"taiko", @"RX");
            stored = RelaxLeaderboardMemory.Remember(stored, @"osu", @"AP");
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"osu"), Is.EqualTo(@"AP"));
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"taiko"), Is.EqualTo(@"RX"));

            stored = RelaxLeaderboardMemory.Remember(stored, @"osu", null);
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"osu"), Is.Null);
            Assert.That(RelaxLeaderboardMemory.GetRemembered(stored, @"taiko"), Is.EqualTo(@"RX"));
        }

        [Test]
        public void TestScosuChatLinks()
        {
            string previousRoot = MessageFormatter.WebsiteRootUrl;

            try
            {
                // Even if another website root is configured, scosu.net links should resolve in-game.
                MessageFormatter.WebsiteRootUrl = @"https://example.test";

                Assert.That(MessageFormatter.IsWebsiteHost(@"scosu.net"), Is.True);
                Assert.That(MessageFormatter.IsWebsiteHost(@"sub.scosu.net"), Is.True);
                Assert.That(MessageFormatter.IsWebsiteHost(@"example.test"), Is.True);
                Assert.That(MessageFormatter.IsWebsiteHost(@"notscosu.net"), Is.False);

                var beatmap = MessageFormatter.GetLinkDetails(@"https://scosu.net/b/12345");
                Assert.That(beatmap.Action, Is.EqualTo(LinkAction.OpenBeatmap));
                Assert.That(beatmap.Argument, Is.EqualTo(@"12345"));

                var beatmapSet = MessageFormatter.GetLinkDetails(@"https://scosu.net/beatmapsets/999#osu/12345");
                Assert.That(beatmapSet.Action, Is.EqualTo(LinkAction.OpenBeatmap));
                Assert.That(beatmapSet.Argument, Is.EqualTo(@"12345"));

                var userShort = MessageFormatter.GetLinkDetails(@"https://scosu.net/u/42");
                Assert.That(userShort.Action, Is.EqualTo(LinkAction.OpenUserProfile));

                var userFull = MessageFormatter.GetLinkDetails(@"https://scosu.net/users/42/osurx");
                Assert.That(userFull.Action, Is.EqualTo(LinkAction.OpenUserProfile));
            }
            finally
            {
                MessageFormatter.WebsiteRootUrl = previousRoot;
            }
        }

        [Test]
        public void TestLoginUnreachableErrorClassification()
        {
            Assert.That(LoginForm.IsUnreachableError(new HttpRequestException(@"Connection refused")), Is.True);
            Assert.That(LoginForm.IsUnreachableError(new TaskCanceledException(@"Timed out")), Is.True);
            Assert.That(LoginForm.IsUnreachableError(new APIException(@"Bad Gateway", null, HttpStatusCode.BadGateway)), Is.True);
            Assert.That(LoginForm.IsUnreachableError(new InvalidOperationException(@"Cloudflare error 1033")), Is.True);

            Assert.That(LoginForm.IsUnreachableError(new APIException(@"Invalid credentials", null, HttpStatusCode.Unauthorized)), Is.False);
            Assert.That(LoginForm.IsUnreachableError(new InvalidOperationException(@"Wrong username or password")), Is.False);
            Assert.That(LoginForm.IsUnreachableError(null), Is.False);
        }

        [Test]
        public void TestToolbarServerStatusFormatting()
        {
            Assert.That(ToolbarServerStatus.FormatOnlineLabel(12, 24), Is.EqualTo(@"12 · 24ms"));
            Assert.That(ToolbarServerStatus.FormatOnlineLabel(1, null), Is.EqualTo(@"1"));
        }

        [Test]
        public void TestMainMenuVolumeScrollRouting()
        {
            // Base state (band out, hub closed, volume overlay hidden):
            // down opens the hub, up enters volume mode.
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: false, volumeOverlayVisible: false, altPressed: false, scrollDeltaY: -1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.OpenHub));
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: false, volumeOverlayVisible: false, altPressed: false, scrollDeltaY: 1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.PassToVolume));

            // Volume mode (volume overlay visible): both directions adjust volume and never open the hub.
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: false, volumeOverlayVisible: true, altPressed: false, scrollDeltaY: 1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.PassToVolume));
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: false, volumeOverlayVisible: true, altPressed: false, scrollDeltaY: -1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.PassToVolume));

            // Hub open (volume overlay hidden): up closes the hub, down is consumed.
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: true, volumeOverlayVisible: false, altPressed: false, scrollDeltaY: 1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.CloseHub));
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: true, volumeOverlayVisible: false, altPressed: false, scrollDeltaY: -1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.Consume));

            // Alt+wheel or band hidden always passes to volume.
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: true, hubOpen: false, volumeOverlayVisible: false, altPressed: true, scrollDeltaY: -1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.PassToVolume));
            Assert.That(MainMenu.ResolveHubScroll(bandIsOut: false, hubOpen: false, volumeOverlayVisible: false, altPressed: false, scrollDeltaY: -1),
                Is.EqualTo(MainMenu.MainMenuScrollAction.PassToVolume));
        }
    }
}
