// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// The score signature and the install id, which scosu-rust's <c>src/score_signature.rs</c> and <c>services/account_links.rs</c> check the other end of.
    /// </summary>
    [TestFixture]
    public class ScoreSignatureTest
    {
        [Test]
        public void TestSignatureMatchesServerVector()
        {
            // Dummy key, the same vector as the server's test.
            string signature = ScoreSignature.Sign("test-signing-key-not-secret", "beatmaps/123/solo/scores/456", @"{""ruleset_id"":0,""passed"":true,""total_score"":1234567}");

            Assert.That(signature, Is.EqualTo(ScoreSignature.TEST_VECTOR_SIGNATURE));
        }

        [Test]
        public void TestDifferentBodyOrTargetChangesSignature()
        {
            string signature = ScoreSignature.Sign("k", "beatmaps/1/solo/scores/2", "{}");

            Assert.That(ScoreSignature.Sign("k", "beatmaps/1/solo/scores/2", "{ }"), Is.Not.EqualTo(signature));
            Assert.That(ScoreSignature.Sign("k", "beatmaps/1/solo/scores/3", "{}"), Is.Not.EqualTo(signature));
            Assert.That(ScoreSignature.Sign("other", "beatmaps/1/solo/scores/2", "{}"), Is.Not.EqualTo(signature));
        }

        [Test]
        public void TestDevelopmentBuildDoesNotSign()
        {
            // Test builds are made without the release key.
            Assert.That(ScoreSignature.Sign("beatmaps/1/solo/scores/2", "{}"), Is.Null);
        }

        [Test]
        public void TestGeneratedDeviceIdIsValidAndRandom()
        {
            string id = OAuth.GenerateDeviceId();

            Assert.That(OAuth.IsValidDeviceId(id), Is.True);
            Assert.That(OAuth.GenerateDeviceId(), Is.Not.EqualTo(id));
        }

        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("0123456789abcdef0123456789abcdef", true)]
        [TestCase("0123456789ABCDEF0123456789ABCDEF", false)]
        [TestCase("0123456789abcdef0123456789abcde", false)]
        [TestCase("0123456789abcdef0123456789abcdeg", false)]
        public void TestDeviceIdValidation(string? id, bool valid)
        {
            Assert.That(OAuth.IsValidDeviceId(id!), Is.EqualTo(valid));
        }
    }
}
