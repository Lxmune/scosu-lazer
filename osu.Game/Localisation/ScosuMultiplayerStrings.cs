// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's multiplayer additions: the win condition of a playlist item.
    /// </summary>
    public static class ScosuMultiplayerStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuMultiplayer";

        /// <summary>
        /// "Win condition"
        /// </summary>
        public static LocalisableString WinCondition => new TranslatableString(getKey(@"win_condition"), @"Win condition");

        /// <summary>
        /// "What decides this map's results. Ties are broken by score."
        /// </summary>
        public static LocalisableString WinConditionTooltip => new TranslatableString(getKey(@"win_condition_tooltip"), @"What decides this map's results. Ties are broken by score.");

        /// <summary>
        /// "Score"
        /// </summary>
        public static LocalisableString WinConditionScore => new TranslatableString(getKey(@"win_condition_score"), @"Score");

        /// <summary>
        /// "Accuracy"
        /// </summary>
        public static LocalisableString WinConditionAccuracy => new TranslatableString(getKey(@"win_condition_accuracy"), @"Accuracy");

        /// <summary>
        /// "Combo"
        /// </summary>
        public static LocalisableString WinConditionCombo => new TranslatableString(getKey(@"win_condition_combo"), @"Combo");

        /// <summary>
        /// "Accuracy wins"
        /// </summary>
        public static LocalisableString AccuracyWins => new TranslatableString(getKey(@"accuracy_wins"), @"Accuracy wins");

        /// <summary>
        /// "Combo wins"
        /// </summary>
        public static LocalisableString ComboWins => new TranslatableString(getKey(@"combo_wins"), @"Combo wins");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
