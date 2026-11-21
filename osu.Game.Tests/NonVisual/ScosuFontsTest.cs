// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Game.Graphics;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// scosu: the fonts that replace osu!'s Torus and Venera (see Resources/Fonts/README.md) load from the game's resources,
    /// through osu-framework's own font reader, and are sized like the fonts they replace.
    /// </summary>
    [TestFixture]
    public class ScosuFontsTest
    {
        private static ResourceStore<byte[]> gameResources()
        {
            var store = new ResourceStore<byte[]>();
            // As OsuGameBase adds them: the game's own resources first, then osu!'s.
            store.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(OsuGameBase).Assembly), @"Resources"));
            store.AddStore(new DllResourceStore(osu.Game.Resources.OsuResources.ResourceAssembly));
            return store;
        }

        [TestCase(@"Fonts/Outfit/Outfit-Light", 84)]
        [TestCase(@"Fonts/Outfit/Outfit-Regular", 84)]
        [TestCase(@"Fonts/Outfit/Outfit-SemiBold", 84)]
        [TestCase(@"Fonts/Outfit/Outfit-Bold", 84)]
        [TestCase(@"Fonts/Unbounded/Unbounded-Light", 75)]
        [TestCase(@"Fonts/Unbounded/Unbounded-Bold", 75)]
        [TestCase(@"Fonts/Unbounded/Unbounded-Black", 75)]
        public void TestFontLoads(string asset, int baseline)
        {
            using var glyphs = new GlyphStore(gameResources(), asset);
            glyphs.LoadFontAsync().WaitSafely();

            Assert.That(glyphs.Baseline, Is.EqualTo(baseline));

            foreach (char c in @"Aa0%!é")
            {
                Assert.That(glyphs.HasGlyph(c), Is.True, $"{asset} has no '{c}'");
                Assert.That(glyphs.Get(c)!.XAdvance, Is.GreaterThan(0), $"{asset}: '{c}' has no width");
            }

            Assert.That(glyphs.Get('A')!.XAdvance, Is.Not.EqualTo(glyphs.Get('i')!.XAdvance), "proportional");
        }

        [Test]
        public void TestControlWithOsuFont()
        {
            using var glyphs = new GlyphStore(gameResources(), @"Fonts/Inter/Inter-Regular");
            glyphs.LoadFontAsync().WaitSafely();
            Assert.That(glyphs.HasGlyph('A'), Is.True);
        }

        [Test]
        public void TestTypefacesUseThem()
        {
            Assert.That(OsuFont.GetFont(Typeface.Torus, weight: FontWeight.Bold).FontName, Is.EqualTo(@"Outfit-Bold"));
            Assert.That(OsuFont.GetFont(Typeface.Torus, weight: FontWeight.Medium).FontName, Is.EqualTo(@"Outfit-Regular"));
            Assert.That(OsuFont.GetFont(Typeface.TorusAlternate, weight: FontWeight.SemiBold).FontName, Is.EqualTo(@"Outfit-SemiBold"));
            Assert.That(OsuFont.GetFont(Typeface.Venera, weight: FontWeight.Black).FontName, Is.EqualTo(@"Unbounded-Black"));
        }
    }
}
