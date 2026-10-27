// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the rankings of ranked score gained this week or month, on servers that rank by osu!stable score.
    /// </summary>
    public static class GainedRankingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GainedRankings";

        /// <summary>
        /// "Gained"
        /// </summary>
        public static LocalisableString Gained => new TranslatableString(getKey(@"gained"), @"Gained");

        /// <summary>
        /// "Period"
        /// </summary>
        public static LocalisableString Period => new TranslatableString(getKey(@"period"), @"Period");

        /// <summary>
        /// "This week"
        /// </summary>
        public static LocalisableString ThisWeek => new TranslatableString(getKey(@"this_week"), @"This week");

        /// <summary>
        /// "This month"
        /// </summary>
        public static LocalisableString ThisMonth => new TranslatableString(getKey(@"this_month"), @"This month");

        /// <summary>
        /// "Ranked score gained since Monday. Resets every Monday at 00:00 UTC."
        /// </summary>
        public static LocalisableString WeekDescription => new TranslatableString(getKey(@"week_description"),
            @"Ranked score gained since Monday. Resets every Monday at 00:00 UTC.");

        /// <summary>
        /// "Ranked score gained since the 1st. Resets on the 1st of every month at 00:00 UTC."
        /// </summary>
        public static LocalisableString MonthDescription => new TranslatableString(getKey(@"month_description"),
            @"Ranked score gained since the 1st. Resets on the 1st of every month at 00:00 UTC.");

        /// <summary>
        /// "Score gained"
        /// </summary>
        public static LocalisableString ScoreGained => new TranslatableString(getKey(@"score_gained"), @"Score gained");

        /// <summary>
        /// "Nobody has gained any ranked score yet."
        /// </summary>
        public static LocalisableString NoneYet => new TranslatableString(getKey(@"none_yet"), @"Nobody has gained any ranked score yet.");

        /// <summary>
        /// "{0} places in the last 14 days"
        /// </summary>
        public static LocalisableString RankChange(LocalisableString places) => new TranslatableString(getKey(@"rank_change"), @"{0} places in the last 14 days", places);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
