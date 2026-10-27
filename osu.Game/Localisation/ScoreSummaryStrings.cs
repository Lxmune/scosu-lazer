// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the main menu's summary of the ranked score gained today and this week.
    /// </summary>
    public static class ScoreSummaryStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScoreSummary";

        /// <summary>
        /// "Today"
        /// </summary>
        public static LocalisableString Today => new TranslatableString(getKey(@"today"), @"Today");

        /// <summary>
        /// "This week"
        /// </summary>
        public static LocalisableString ThisWeek => new TranslatableString(getKey(@"this_week"), @"This week");

        /// <summary>
        /// "{0} plays"
        /// </summary>
        public static LocalisableString PlayCount(LocalisableString count) => new TranslatableString(getKey(@"play_count"), @"{0} plays", count);

        /// <summary>
        /// "{0} more to pass #{1} {2}"
        /// </summary>
        public static LocalisableString ScoreToNextRank(LocalisableString score, LocalisableString rank, string username)
            => new TranslatableString(getKey(@"score_to_next_rank"), @"{0} more to pass #{1} {2}", score, rank, username);

        /// <summary>
        /// "+{0} today"
        /// </summary>
        public static LocalisableString GainedToday(LocalisableString score) => new TranslatableString(getKey(@"gained_today"), @"+{0} today", score);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
