// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ScosuAccentHueTest
    {
        [TearDown]
        public void TearDown()
        {
            ScosuMenuStyle.AccentHue.SetDefault();
        }

        [Test]
        public void TestDefaultHueMatchesScosuViolet()
        {
            ScosuMenuStyle.AccentHue.SetDefault();

            Assert.That(ScosuMenuStyle.AccentHue.Value, Is.EqualTo(256));
            Assert.That(ScosuMenuStyle.VIOLET, Is.EqualTo(Color4Extensions.FromHex(@"a68bff")));

            var colours = new OsuColour();
            colours.ApplyAccentHue(ScosuMenuStyle.DEFAULT_HUE);
            Assert.That(colours.Pink, Is.EqualTo(Color4Extensions.FromHex(@"7856ce")));
            Assert.That(colours.PinkLight, Is.EqualTo(Color4Extensions.FromHex(@"a88bf5")));
        }

        [Test]
        public void TestCustomHueUpdatesMenuBrandAndOverlayProvider()
        {
            var defaultViolet = ScosuMenuStyle.VIOLET;
            var colours = new OsuColour();
            var defaultPink = colours.Pink;
            var defaultPink1 = colours.Pink1;
            var defaultGreyVioletDarker = colours.GreyVioletDarker;
            var provider = new OverlayColourProvider(OverlayColourScheme.Purple);
            var defaultHighlight = provider.Highlight1;
            var defaultBackground4 = provider.Background4;

            ScosuMenuStyle.AccentHue.Value = 190;
            colours.ApplyAccentHue(190);

            Assert.That(ScosuMenuStyle.VIOLET, Is.Not.EqualTo(defaultViolet));
            Assert.That(colours.Pink, Is.Not.EqualTo(defaultPink));
            Assert.That(colours.Pink1, Is.Not.EqualTo(defaultPink1));
            Assert.That(colours.GreyVioletDarker, Is.Not.EqualTo(defaultGreyVioletDarker));
            Assert.That(provider.Hue, Is.EqualTo(190));
            Assert.That(provider.Highlight1, Is.Not.EqualTo(defaultHighlight));
            Assert.That(provider.Background4, Is.Not.EqualTo(defaultBackground4));

            // Cyan hue (~190°) should be stronger in blue/green than red.
            Assert.That(ScosuMenuStyle.VIOLET.B, Is.GreaterThan(ScosuMenuStyle.VIOLET.R));
            Assert.That(colours.Pink.B, Is.GreaterThan(colours.Pink.R));
            Assert.That(provider.Highlight1.B, Is.GreaterThan(provider.Highlight1.R));
        }
    }
}
