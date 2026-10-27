// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ProductionEndpointConfigurationTest
    {
        [TestCase(null, "https://lazer.scosu.net")]
        [TestCase("", "https://lazer.scosu.net")]
        [TestCase("https://example.com/", "https://example.com")]
        [TestCase(" https://example.com:8443 ", "https://example.com:8443")]
        public void TestAcceptedServerUrls(string? input, string expected)
        {
            Assert.That(ProductionEndpointConfiguration.TryNormaliseServerUrl(input, out string normalised), Is.True);
            Assert.That(normalised, Is.EqualTo(expected));
        }

        [TestCase("http://example.com")]
        [TestCase("example.com")]
        [TestCase("ftp://example.com")]
        [TestCase("https://example.com/?token=1")]
        [TestCase("https://example.com/osu")]
        [TestCase("https://user:password@example.com")]
        public void TestRejectedServerUrls(string input)
        {
            Assert.That(ProductionEndpointConfiguration.TryNormaliseServerUrl(input, out _), Is.False);
        }

        [Test]
        public void TestEndpointsOfServer()
        {
            var scosu = new ProductionEndpointConfiguration();

            Assert.That(scosu.APIUrl, Is.EqualTo("https://lazer.scosu.net"));
            Assert.That(scosu.MultiplayerUrl, Is.EqualTo("https://lazer.scosu.net/signalr/multiplayer"));
            Assert.That(scosu.RanksByStableScore, Is.True);

            var other = new ProductionEndpointConfiguration("https://osu.example.com");

            Assert.That(other.SpectatorUrl, Is.EqualTo("https://osu.example.com/signalr/spectator"));
            Assert.That(other.RanksByStableScore, Is.False);

            // an invalid address falls back to the scosu server rather than connecting anywhere else.
            Assert.That(new ProductionEndpointConfiguration("http://osu.example.com").APIUrl, Is.EqualTo("https://lazer.scosu.net"));
        }
    }
}
