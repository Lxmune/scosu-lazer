// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Ranking;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class ShareScoreButtonTest
    {
        [Test]
        public void TestScoreUrl()
        {
            Assert.That(ShareScoreButton.ScoreUrl("https://scosu.net", 123), Is.EqualTo("https://scosu.net/scores/123"));
            Assert.That(ShareScoreButton.ScoreUrl("https://scosu.net/", 123), Is.EqualTo("https://scosu.net/scores/123"));
        }

        [Test]
        public void TestNoUrlWithoutAnOnlineScore()
        {
            Assert.That(ShareScoreButton.ScoreUrl("https://scosu.net", -1), Is.Null);
            Assert.That(ShareScoreButton.ScoreUrl("https://scosu.net", 0), Is.Null);
            Assert.That(ShareScoreButton.ScoreUrl(string.Empty, 123), Is.Null);
        }
    }
}
