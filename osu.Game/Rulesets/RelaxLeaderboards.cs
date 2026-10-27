// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Scoring;

namespace osu.Game.Rulesets
{
    /// <summary>
    /// Relax and Autopilot plays are ranked apart from normal plays, on leaderboards of their own.
    /// Servers present each of these leaderboards as an extra ruleset (such as "osurx", with online IDs 4 to 7),
    /// whose plays are played in the base ruleset with the matching mod.
    /// </summary>
    public static class RelaxLeaderboards
    {
        private record Leaderboard(string RulesetShortName, string ModAcronym, string ShortName, int OnlineID);

        /// <summary>
        /// In the order they are shown. A play with both mods (which lazer doesn't allow) would go to Autopilot.
        /// </summary>
        private static readonly Leaderboard[] leaderboards =
        {
            new Leaderboard(@"osu", @"RX", @"osurx", 4),
            new Leaderboard(@"osu", @"AP", @"osuap", 5),
            new Leaderboard(@"taiko", @"RX", @"taikorx", 6),
            new Leaderboard(@"fruits", @"RX", @"fruitsrx", 7),
        };

        /// <summary>
        /// This ruleset, followed by its Relax and Autopilot leaderboards.
        /// </summary>
        public static IEnumerable<RulesetInfo> WithRelaxLeaderboards(this RulesetInfo ruleset)
            => leaderboards.Where(l => l.RulesetShortName == ruleset.ShortName).Select(l => create(ruleset, l)).Prepend(ruleset);

        /// <summary>
        /// The leaderboard that plays in this ruleset with the given mods are ranked on:
        /// a Relax or Autopilot leaderboard, or the ruleset itself.
        /// </summary>
        public static RulesetInfo GetLeaderboardRuleset(this RulesetInfo ruleset, IEnumerable<string> modAcronyms)
        {
            var acronyms = modAcronyms.ToHashSet();
            var leaderboard = leaderboards.Where(l => l.RulesetShortName == ruleset.ShortName && acronyms.Contains(l.ModAcronym))
                                          .MaxBy(l => l.ModAcronym == @"AP");

            return leaderboard == null ? ruleset : create(ruleset, leaderboard);
        }

        /// <summary>
        /// The leaderboard this score is ranked on.
        /// </summary>
        public static RulesetInfo GetLeaderboardRuleset(this ScoreInfo score) => score.Ruleset.GetLeaderboardRuleset(score.Mods.Select(m => m.Acronym));

        /// <summary>
        /// The short name of the ruleset that the plays of this leaderboard are played in ("osu" for "osurx").
        /// </summary>
        public static string GetPlayedRulesetShortName(this IRulesetInfo ruleset) => leaderboards.FirstOrDefault(l => l.ShortName == ruleset.ShortName)?.RulesetShortName ?? ruleset.ShortName;

        /// <summary>
        /// The mod acronym ("RX" or "AP") of a Relax or Autopilot leaderboard, or <see langword="null"/> for a normal ruleset.
        /// </summary>
        public static string? GetRelaxLeaderboardMod(this IRulesetInfo ruleset) => leaderboards.FirstOrDefault(l => l.ShortName == ruleset.ShortName)?.ModAcronym;

        /// <summary>
        /// Looks up a ruleset or a Relax or Autopilot leaderboard by its short name.
        /// </summary>
        public static RulesetInfo? GetLeaderboardRuleset(this RulesetStore rulesets, string shortName)
        {
            var leaderboard = leaderboards.FirstOrDefault(l => l.ShortName == shortName);

            if (leaderboard == null)
                return rulesets.GetRuleset(shortName);

            var ruleset = rulesets.GetRuleset(leaderboard.RulesetShortName);
            return ruleset == null ? null : create(ruleset, leaderboard);
        }

        private static RulesetInfo create(RulesetInfo ruleset, Leaderboard leaderboard) =>
            new RulesetInfo(leaderboard.ShortName, $@"{ruleset.Name} ({leaderboard.ModAcronym})", ruleset.InstantiationInfo, leaderboard.OnlineID)
            {
                Available = ruleset.Available,
            };
    }
}
