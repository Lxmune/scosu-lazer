// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Select;
using osu.Game.Utils;

namespace osu.Game.Rulesets
{
    /// <summary>
    /// scosu: remembers the Relax or Autopilot leaderboard (see <see cref="RelaxLeaderboards"/>) the player last chose in song select,
    /// one per ruleset, and chooses it again when the game starts and when switching back to that ruleset.
    /// </summary>
    /// <remarks>
    /// The leaderboard follows the selected mods (Relax or Autopilot), so remembering it means remembering that mod.
    /// Only the choices made in solo song select are remembered, and the mod is only added back on the main menu or in solo song select,
    /// so multiplayer's mods are never touched. Nothing is added when the mod doesn't fit with the other selected mods.
    /// </remarks>
    public partial class RelaxLeaderboardMemory : Component
    {
        private readonly Func<IScreen?> currentScreen;

        private readonly Bindable<RulesetInfo> ruleset = new Bindable<RulesetInfo>();
        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        private Bindable<string> remembered = null!;

        /// <summary>
        /// The ruleset the selected mods belong to. While the ruleset changes, the mods are converted to the new one (and Autopilot may be dropped):
        /// that isn't a choice of the player, so nothing is remembered until the change has settled.
        /// </summary>
        private string? settledRuleset;

        public RelaxLeaderboardMemory(Func<IScreen?> currentScreen)
        {
            this.currentScreen = currentScreen;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Bindable<RulesetInfo> gameRuleset, Bindable<IReadOnlyList<Mod>> selectedMods)
        {
            remembered = config.GetBindable<string>(OsuSetting.ScosuRelaxLeaderboards);
            ruleset.BindTo(gameRuleset);
            mods.BindTo(selectedMods);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ruleset.BindValueChanged(_ =>
            {
                settledRuleset = null;
                Scheduler.AddOnce(settle, false);
            });
            mods.BindValueChanged(_ => remember());

            // The game's ruleset is read from the settings during startup: restore once it is there.
            Scheduler.AddOnce(settle, true);
        }

        private void settle(bool startup)
        {
            settledRuleset = ruleset.Value?.ShortName;

            if (startup || isScreenWhereModsMayChange(currentScreen()))
                restore();
        }

        private void remember()
        {
            if (mods.Disabled || settledRuleset == null || ruleset.Value?.ShortName != settledRuleset)
                return;

            if (currentScreen() is not SoloSongSelect)
                return;

            string? acronym = ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym)).GetRelaxLeaderboardMod();
            remembered.Value = Remember(remembered.Value, settledRuleset, acronym);
        }

        private void restore()
        {
            if (mods.Disabled || ruleset.Value == null || !ruleset.Value.Available)
                return;

            string? acronym = GetRemembered(remembered.Value, ruleset.Value.ShortName);

            if (acronym == null)
                return;

            // Already on a Relax or Autopilot leaderboard.
            if (ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym)).GetRelaxLeaderboardMod() != null)
                return;

            Mod? mod;

            try
            {
                mod = ruleset.Value.CreateInstance().CreateModFromAcronym(acronym);
            }
            catch
            {
                return;
            }

            if (mod == null)
                return;

            var newMods = mods.Value.Append(mod).ToArray();

            if (!ModUtils.CheckCompatibleSet(newMods) || !ModUtils.CheckValidForGameplay(newMods, out _))
                return;

            mods.Value = newMods;
        }

        private static bool isScreenWhereModsMayChange(IScreen? screen) => screen is SoloSongSelect or MainMenu;

        private const char entry_separator = ';';
        private const char value_separator = ':';

        /// <summary>
        /// The mod acronym ("RX" or "AP") of the leaderboard remembered for <paramref name="rulesetShortName"/> in <paramref name="stored"/>
        /// (the setting's value, such as "osu:AP;taiko:RX"), or <see langword="null"/> for its normal leaderboard.
        /// </summary>
        public static string? GetRemembered(string? stored, string rulesetShortName)
            => parse(stored).GetValueOrDefault(rulesetShortName);

        /// <summary>
        /// <paramref name="stored"/> with <paramref name="acronym"/> ("RX", "AP", or <see langword="null"/> for the normal leaderboard)
        /// remembered for <paramref name="rulesetShortName"/>.
        /// </summary>
        public static string Remember(string? stored, string rulesetShortName, string? acronym)
        {
            var entries = parse(stored);

            if (string.IsNullOrEmpty(acronym))
                entries.Remove(rulesetShortName);
            else
                entries[rulesetShortName] = acronym;

            return string.Join(entry_separator, entries.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => $@"{e.Key}{value_separator}{e.Value}"));
        }

        private static Dictionary<string, string> parse(string? stored)
        {
            var entries = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(stored))
                return entries;

            foreach (string entry in stored.Split(entry_separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = entry.Split(value_separator, StringSplitOptions.TrimEntries);

                if (parts.Length == 2 && parts[0].Length > 0 && parts[1] is @"RX" or @"AP")
                    entries[parts[0]] = parts[1];
            }

            return entries;
        }
    }
}
