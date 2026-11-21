// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Configuration;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class SessionStaticsTest
    {
        /// <summary>
        /// scosu: the hub's daily challenge tile reads this before anything may have set it.
        /// </summary>
        [Test]
        public void TestDailyChallengeIntroNotPlayedByDefault()
        {
            var statics = new SessionStatics();
            Assert.That(statics.Get<bool>(Static.DailyChallengeIntroPlayed), Is.False);
        }
    }
}
