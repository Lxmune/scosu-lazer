// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class RelaxLeaderboardsTest
    {
        [Test]
        public void TestLeaderboardsOfRulesets()
        {
            Assert.That(new OsuRuleset().RulesetInfo.WithRelaxLeaderboards().Select(r => (r.ShortName, r.OnlineID)),
                Is.EqualTo(new[] { ("osu", 0), ("osurx", 4), ("osuap", 5) }));
            Assert.That(new TaikoRuleset().RulesetInfo.WithRelaxLeaderboards().Select(r => r.ShortName), Is.EqualTo(new[] { "taiko", "taikorx" }));
            Assert.That(new CatchRuleset().RulesetInfo.WithRelaxLeaderboards().Select(r => r.ShortName), Is.EqualTo(new[] { "fruits", "fruitsrx" }));
            Assert.That(new ManiaRuleset().RulesetInfo.WithRelaxLeaderboards().Select(r => r.ShortName), Is.EqualTo(new[] { "mania" }));
        }

        [Test]
        public void TestLeaderboardOfMods()
        {
            var osu = new OsuRuleset().RulesetInfo;

            Assert.That(osu.GetLeaderboardRuleset(new[] { "HD", "DT" }).ShortName, Is.EqualTo("osu"));
            Assert.That(osu.GetLeaderboardRuleset(new[] { "HD", "RX" }).ShortName, Is.EqualTo("osurx"));
            Assert.That(osu.GetLeaderboardRuleset(new[] { "AP" }).ShortName, Is.EqualTo("osuap"));
            // like the server: Autopilot wins over Relax.
            Assert.That(osu.GetLeaderboardRuleset(new[] { "RX", "AP" }).ShortName, Is.EqualTo("osuap"));
            Assert.That(new ManiaRuleset().RulesetInfo.GetLeaderboardRuleset(new[] { "RX" }).ShortName, Is.EqualTo("mania"));
        }

        [Test]
        public void TestLeaderboardRulesetsArePlayedInTheirRuleset()
        {
            var relax = new OsuRuleset().RulesetInfo.GetLeaderboardRuleset(new[] { "RX" });

            Assert.That(relax.GetRelaxLeaderboardMod(), Is.EqualTo("RX"));
            Assert.That(relax.GetPlayedRulesetShortName(), Is.EqualTo("osu"));
            Assert.That(relax.CreateInstance(), Is.InstanceOf<OsuRuleset>());
            Assert.That(new OsuRuleset().RulesetInfo.GetRelaxLeaderboardMod(), Is.Null);
        }
    }
}
