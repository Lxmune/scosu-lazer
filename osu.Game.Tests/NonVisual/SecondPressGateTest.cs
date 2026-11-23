// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Play;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class SecondPressGateTest
    {
        private readonly object retry = new object();
        private readonly object quit = new object();

        [Test]
        public void TestSecondPressInTimeGoesAhead()
        {
            var gate = new SecondPressGate();

            Assert.That(gate.Press(retry, 1000), Is.False);
            Assert.That(gate.IsArmed(retry, 3000), Is.True);
            Assert.That(gate.Press(retry, 3000), Is.True);
            // Disarmed again after going ahead.
            Assert.That(gate.Press(retry, 3100), Is.False);
        }

        [Test]
        public void TestLatePressOrOtherActionStartsAgain()
        {
            var gate = new SecondPressGate();

            Assert.That(gate.Press(retry, 0), Is.False);
            Assert.That(gate.Press(retry, SecondPressGate.WINDOW + 1), Is.False);
            Assert.That(gate.Press(quit, SecondPressGate.WINDOW + 2), Is.False);
            Assert.That(gate.IsArmed(retry, SecondPressGate.WINDOW + 3), Is.False);
            Assert.That(gate.Press(quit, SecondPressGate.WINDOW + 3), Is.True);

            gate.Press(retry, 0);
            gate.Reset();
            Assert.That(gate.IsArmed(retry, 1), Is.False);
        }
    }
}
