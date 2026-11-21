// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using NUnit.Framework.Legacy;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// Ensures the visual assets scosu replaces are shipped in osu.Game's own embedded resources (which take priority over osu!'s)
    /// and have the same dimensions as the osu! files they override, so layouts do not change.
    /// </summary>
    [TestFixture]
    public class ScosuVisualResourcesTest
    {
        [TestCase("Icons/Logo.png", 90, 90)]
        [TestCase("Icons/RulesetOsu.png", 90, 90)]
        [TestCase("Icons/RulesetTaiko.png", 90, 90)]
        [TestCase("Icons/RulesetCatch.png", 90, 90)]
        [TestCase("Icons/RulesetMania.png", 90, 90)]
        [TestCase("Icons/play-circle.png", 100, 100)]
        [TestCase("Intro/Triangles/logo-background.png", 1295, 1295)]
        [TestCase("Intro/Triangles/logo-highlight.png", 1295, 1295)]
        [TestCase("Intro/Welcome/menu-background.jpg", 2732, 1536)]
        [TestCase("Menu/menu-background-1.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-2.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-3.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-4.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-5.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-6.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-7.jpg", 2732, 1537)]
        [TestCase("Menu/menu-background-8.jpg", 2732, 1537)]
        [TestCase("Menu/logo.png", 1024, 1024)]
        [TestCase("Menu/hat.png", 922, 563)]
        [TestCase("Menu/dev-build-footer.png", 1920, 8)]
        [TestCase("Online/avatar-guest.png", 256, 256)]
        [TestCase("Online/not-found.png", 520, 603)]
        [TestCase("Online/supporter-heart.png", 150, 149)]
        [TestCase("Online/supporter-pippi.png", 656, 932)]
        [TestCase("Online/supporter-required.png", 520, 449)]
        [TestCase("Backgrounds/bg1.jpg", 1366, 768)]
        [TestCase("Backgrounds/bg2.jpg", 1366, 768)]
        [TestCase("Backgrounds/bg3.jpg", 1366, 768)]
        [TestCase("Backgrounds/bg4.jpg", 1366, 768)]
        [TestCase("Backgrounds/registration.jpg", 1661, 1036)]
        [TestCase("Headers/news.png", 1400, 310)]
        [TestCase("Headers/changelog.jpg", 1400, 310)]
        [TestCase("Headers/rankings.jpg", 1800, 600)]
        [TestCase("Headers/search.jpg", 900, 200)]
        [TestCase("Headers/wiki.jpg", 901, 170)]
        public void TestTextureReplaced(string path, int width, int height)
        {
            var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(OsuGameBase).Assembly), "Resources");

            using var stream = store.GetStream($"Textures/{path}");
            ClassicAssert.IsNotNull(stream, $"{path} is not embedded in osu.Game");

            var info = Image.Identify(stream);
            ClassicAssert.IsNotNull(info, $"{path} is not a valid image");
            ClassicAssert.AreEqual(width, info.Width);
            ClassicAssert.AreEqual(height, info.Height);
        }
    }
}
