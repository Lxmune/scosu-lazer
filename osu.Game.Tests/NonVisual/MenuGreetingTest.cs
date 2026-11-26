// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Screens.Menu;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class MenuGreetingTest
    {
        [TestCase(5, MenuGreeting.TimeOfDay.Morning)]
        [TestCase(11, MenuGreeting.TimeOfDay.Morning)]
        [TestCase(12, MenuGreeting.TimeOfDay.Afternoon)]
        [TestCase(17, MenuGreeting.TimeOfDay.Afternoon)]
        [TestCase(18, MenuGreeting.TimeOfDay.Evening)]
        [TestCase(21, MenuGreeting.TimeOfDay.Evening)]
        [TestCase(22, MenuGreeting.TimeOfDay.Night)]
        [TestCase(0, MenuGreeting.TimeOfDay.Night)]
        [TestCase(4, MenuGreeting.TimeOfDay.Night)]
        public void TestTimeOfDay(int hour, MenuGreeting.TimeOfDay expected)
            => Assert.That(MenuGreeting.At(new DateTimeOffset(2026, 11, 25, hour, 30, 0, TimeSpan.Zero)), Is.EqualTo(expected));

        [Test]
        public void TestEveryGreetingFormats()
        {
            var all = Enum.GetValues<MenuGreeting.TimeOfDay>().SelectMany(MenuGreeting.For).ToArray();

            Assert.That(all, Has.Length.GreaterThanOrEqualTo(50));
            Assert.That(all, Is.Unique);

            foreach (string greeting in all)
                Assert.That(string.Format(greeting, "peppy"), Does.Not.Contain("{"));
        }

        [Test]
        public void TestPickedGreetingIsForTheTimeOfDay()
        {
            var evening = new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero);
            var random = new Random(1);

            for (int i = 0; i < 20; i++)
                Assert.That(MenuGreeting.EVENING.Select(g => string.Format(g, "peppy")), Does.Contain(MenuGreeting.Pick(evening, "peppy", random)));
        }
    }
}
