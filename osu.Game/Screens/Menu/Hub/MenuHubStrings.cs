// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// Strings of the main menu hub's tiles and bar.
    /// </summary>
    public static class MenuHubStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuMenuHub";

        /// <summary>
        /// "Score gained"
        /// </summary>
        public static LocalisableString ScoreGained => new TranslatableString(getKey(@"score_gained"), @"Score gained");

        /// <summary>
        /// "global rank"
        /// </summary>
        public static LocalisableString GlobalRank => new TranslatableString(getKey(@"global_rank"), @"global rank");

        /// <summary>
        /// "Log in to see your score"
        /// </summary>
        public static LocalisableString LogInForScore => new TranslatableString(getKey(@"log_in_for_score"), @"Log in to see your score");

        /// <summary>
        /// "Your score isn't available right now"
        /// </summary>
        public static LocalisableString ScoreUnavailable => new TranslatableString(getKey(@"score_unavailable"), @"Your score isn't available right now");

        /// <summary>
        /// "Daily challenge"
        /// </summary>
        public static LocalisableString DailyChallenge => new TranslatableString(getKey(@"daily_challenge"), @"Daily challenge");

        /// <summary>
        /// "No daily challenge today"
        /// </summary>
        public static LocalisableString NoDailyChallenge => new TranslatableString(getKey(@"no_daily_challenge"), @"No daily challenge today");

        /// <summary>
        /// "Today's challenge"
        /// </summary>
        public static LocalisableString TodaysChallenge => new TranslatableString(getKey(@"todays_challenge"), @"Today's challenge");

        /// <summary>
        /// "Ends in {0}"
        /// </summary>
        public static LocalisableString EndsInTime(string time) => new TranslatableString(getKey(@"ends_in_time"), @"Ends in {0}", time);

        /// <summary>
        /// "Friends"
        /// </summary>
        public static LocalisableString Friends => new TranslatableString(getKey(@"friends"), @"Friends");

        /// <summary>
        /// "{0} of {1} online"
        /// </summary>
        public static LocalisableString FriendsOnline(string online, string total) => new TranslatableString(getKey(@"friends_online"), @"{0} of {1} online", online, total);

        /// <summary>
        /// "Log in to see your friends"
        /// </summary>
        public static LocalisableString LogInForFriends => new TranslatableString(getKey(@"log_in_for_friends"), @"Log in to see your friends");

        /// <summary>
        /// "No friends added yet"
        /// </summary>
        public static LocalisableString NoFriends => new TranslatableString(getKey(@"no_friends"), @"No friends added yet");

        /// <summary>
        /// "Open friends"
        /// </summary>
        public static LocalisableString OpenFriends => new TranslatableString(getKey(@"open_friends"), @"Open friends");

        /// <summary>
        /// "Weekly report"
        /// </summary>
        public static LocalisableString WeeklyReport => new TranslatableString(getKey(@"weekly_report"), @"Weekly report");

        /// <summary>
        /// "Your week is ready"
        /// </summary>
        public static LocalisableString WeekIsReady => new TranslatableString(getKey(@"week_is_ready"), @"Your week is ready");

        /// <summary>
        /// "Read it on the website"
        /// </summary>
        public static LocalisableString ReadOnWebsite => new TranslatableString(getKey(@"read_on_website"), @"Read it on the website");

        /// <summary>
        /// "Log in to see your weekly report"
        /// </summary>
        public static LocalisableString LogInForReport => new TranslatableString(getKey(@"log_in_for_report"), @"Log in to see your weekly report");

        /// <summary>
        /// "No weekly report yet"
        /// </summary>
        public static LocalisableString NoReport => new TranslatableString(getKey(@"no_report"), @"No weekly report yet");

        /// <summary>
        /// "News"
        /// </summary>
        public static LocalisableString News => new TranslatableString(getKey(@"news"), @"News");

        /// <summary>
        /// "No news right now"
        /// </summary>
        public static LocalisableString NoNews => new TranslatableString(getKey(@"no_news"), @"No news right now");

        /// <summary>
        /// "Read"
        /// </summary>
        public static LocalisableString Read => new TranslatableString(getKey(@"read"), @"Read");

        /// <summary>
        /// "First goal: {0}"
        /// </summary>
        public static LocalisableString FirstGoal(string goal) => new TranslatableString(getKey(@"first_goal"), @"First goal: {0}", goal);

        /// <summary>
        /// "{0} starts in {1}"
        /// </summary>
        public static LocalisableString EventStartsIn(string name, string time) => new TranslatableString(getKey(@"event_starts_in"), @"{0} starts in {1}", name, time);

        /// <summary>
        /// "{0} ends in {1}"
        /// </summary>
        public static LocalisableString EventEndsIn(string name, string time) => new TranslatableString(getKey(@"event_ends_in"), @"{0} ends in {1}", name, time);

        /// <summary>
        /// "On scosu now"
        /// </summary>
        public static LocalisableString OnScosuNow => new TranslatableString(getKey(@"on_scosu_now"), @"On scosu now");

        /// <summary>
        /// "Hide"
        /// </summary>
        public static LocalisableString Hide => new TranslatableString(getKey(@"hide"), @"Hide");

        /// <summary>
        /// "{0} online"
        /// </summary>
        public static LocalisableString Online(string count) => new TranslatableString(getKey(@"online"), @"{0} online", count);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
