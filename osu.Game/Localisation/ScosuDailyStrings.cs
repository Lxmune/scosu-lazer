// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// scosu: strings of the daily tasks, the login briefing, the account standing notice and the gameplay confirmations.
    /// </summary>
    public static class ScosuDailyStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuDaily";

        /// <summary>
        /// "Daily tasks"
        /// </summary>
        public static LocalisableString DailyTasks => new TranslatableString(getKey(@"daily_tasks"), @"Daily tasks");

        /// <summary>
        /// "Log in to get daily tasks"
        /// </summary>
        public static LocalisableString LogInForTasks => new TranslatableString(getKey(@"log_in_for_tasks"), @"Log in to get daily tasks");

        /// <summary>
        /// "Daily tasks aren't available right now"
        /// </summary>
        public static LocalisableString TasksUnavailable => new TranslatableString(getKey(@"tasks_unavailable"), @"Daily tasks aren't available right now");

        /// <summary>
        /// "{0}/{1} done"
        /// </summary>
        public static LocalisableString TasksDone(int done, int total) => new TranslatableString(getKey(@"tasks_done"), @"{0}/{1} done", done, total);

        /// <summary>
        /// "Day streak: {0}"
        /// </summary>
        public static LocalisableString DayStreak(int days) => new TranslatableString(getKey(@"day_streak"), @"Day streak: {0}", days);

        /// <summary>
        /// "Today counts toward your streak"
        /// </summary>
        public static LocalisableString DayCounts => new TranslatableString(getKey(@"day_counts"), @"Today counts toward your streak");

        /// <summary>
        /// "Open on the website"
        /// </summary>
        public static LocalisableString OpenOnWebsite => new TranslatableString(getKey(@"open_on_website"), @"Open on the website");

        /// <summary>
        /// "Gain {0} ranked score"
        /// </summary>
        public static LocalisableString GainRankedScore(string score) => new TranslatableString(getKey(@"gain_ranked_score"), @"Gain {0} ranked score", score);

        /// <summary>
        /// "Beat a best score by {0}%+"
        /// </summary>
        public static LocalisableString ImproveBest(long percent) => new TranslatableString(getKey(@"improve_best"), @"Beat a best score by {0}%+", percent);

        /// <summary>
        /// "Fail a map, then pass it"
        /// </summary>
        public static LocalisableString FailThenPass => new TranslatableString(getKey(@"fail_then_pass"), @"Fail a map, then pass it");

        /// <summary>
        /// "Pass a map unplayed for {0}+ days"
        /// </summary>
        public static LocalisableString ReturnToMap(long days) => new TranslatableString(getKey(@"return_to_map"), @"Pass a map unplayed for {0}+ days", days);

        /// <summary>
        /// "Pass a map with exactly one miss"
        /// </summary>
        public static LocalisableString OneMiss => new TranslatableString(getKey(@"one_miss"), @"Pass a map with exactly one miss");

        /// <summary>
        /// "Top {0}% of today's daily challenge"
        /// </summary>
        public static LocalisableString DailyChallengeTop(long percent) => new TranslatableString(getKey(@"daily_challenge_top"), @"Top {0}% of today's daily challenge", percent);

        /// <summary>
        /// "Take a #1"
        /// </summary>
        public static LocalisableString TakeFirst => new TranslatableString(getKey(@"take_first"), @"Take a #1");

        /// <summary>
        /// "Pass {0} maps in a row, each harder"
        /// </summary>
        public static LocalisableString RisingStars(long count) => new TranslatableString(getKey(@"rising_stars"), @"Pass {0} maps in a row, each harder", count);

        /// <summary>
        /// "Full combo a {0}★+ map"
        /// </summary>
        public static LocalisableString FullComboAbove(string stars) => new TranslatableString(getKey(@"full_combo_above"), @"Full combo a {0}★+ map", stars);

        /// <summary>
        /// "Pass a map with {0}%+ accuracy"
        /// </summary>
        public static LocalisableString HighAccuracy(long percent) => new TranslatableString(getKey(@"high_accuracy"), @"Pass a map with {0}%+ accuracy", percent);

        /// <summary>
        /// "Get an S rank or better"
        /// </summary>
        public static LocalisableString RankS => new TranslatableString(getKey(@"rank_s"), @"Get an S rank or better");

        /// <summary>
        /// "Pass a map with HD, HR, DT or FL"
        /// </summary>
        public static LocalisableString ModdedPass => new TranslatableString(getKey(@"modded_pass"), @"Pass a map with HD, HR, DT or FL");

        /// <summary>
        /// "Pass a map you've never played"
        /// </summary>
        public static LocalisableString NewMap => new TranslatableString(getKey(@"new_map"), @"Pass a map you've never played");

        /// <summary>
        /// "Reach a {0}x combo"
        /// </summary>
        public static LocalisableString ComboAbove(string combo) => new TranslatableString(getKey(@"combo_above"), @"Reach a {0}x combo", combo);

        /// <summary>
        /// "Pass {0} different maps"
        /// </summary>
        public static LocalisableString DifferentMaps(long count) => new TranslatableString(getKey(@"different_maps"), @"Pass {0} different maps", count);

        /// <summary>
        /// "Pass a map of {0}+ minutes"
        /// </summary>
        public static LocalisableString LongMap(string minutes) => new TranslatableString(getKey(@"long_map"), @"Pass a map of {0}+ minutes", minutes);

        /// <summary>
        /// "Since last time"
        /// </summary>
        public static LocalisableString SinceLastTime => new TranslatableString(getKey(@"since_last_time"), @"Since last time");

        /// <summary>
        /// "Welcome back"
        /// </summary>
        public static LocalisableString WelcomeBack => new TranslatableString(getKey(@"welcome_back"), @"Welcome back");

        /// <summary>
        /// "Global rank {0} ({1})"
        /// </summary>
        public static LocalisableString GlobalRankChange(string rank, string change) => new TranslatableString(getKey(@"global_rank_change"), @"Global rank {0} ({1})", rank, change);

        /// <summary>
        /// "Country rank {0} ({1})"
        /// </summary>
        public static LocalisableString CountryRankChange(string rank, string change) => new TranslatableString(getKey(@"country_rank_change"), @"Country rank {0} ({1})", rank, change);

        /// <summary>
        /// "{0} ranked score"
        /// </summary>
        public static LocalisableString RankedScoreChange(string change) => new TranslatableString(getKey(@"ranked_score_change"), @"{0} ranked score", change);

        /// <summary>
        /// "{0} sniped your #1 on {1}"
        /// </summary>
        public static LocalisableString Sniped(string username, string title) => new TranslatableString(getKey(@"sniped"), @"{0} sniped your #1 on {1}", username, title);

        /// <summary>
        /// "Lost your #1 on {0}"
        /// </summary>
        public static LocalisableString LostFirst(string title) => new TranslatableString(getKey(@"lost_first"), @"Lost your #1 on {0}", title);

        /// <summary>
        /// "and {0} more #1s lost"
        /// </summary>
        public static LocalisableString MoreSnipes(int count) => new TranslatableString(getKey(@"more_snipes"), @"and {0} more #1s lost", count);

        /// <summary>
        /// "{0} unread messages"
        /// </summary>
        public static LocalisableString UnreadMessages(int count)
            => new TranslatableString(getKey(@"unread_messages"), count == 1 ? @"{0} unread message" : @"{0} unread messages", count);

        /// <summary>
        /// "Daily tasks: {0}/{1} done, day streak {2}"
        /// </summary>
        public static LocalisableString TasksSummary(int done, int total, int streak)
            => new TranslatableString(getKey(@"tasks_summary"), @"Daily tasks: {0}/{1} done, day streak {2}", done, total, streak);

        /// <summary>
        /// "Your account is restricted: {0}. Click to appeal on the website."
        /// </summary>
        public static LocalisableString Restricted(string reason)
            => new TranslatableString(getKey(@"restricted"), @"Your account is restricted: {0}. Click to appeal on the website.", reason);

        /// <summary>
        /// "Your account is restricted. Click to appeal on the website."
        /// </summary>
        public static LocalisableString RestrictedNoReason => new TranslatableString(getKey(@"restricted_no_reason"), @"Your account is restricted. Click to appeal on the website.");

        /// <summary>
        /// "You are silenced until {0}: {1}. Click to see your account standing."
        /// </summary>
        public static LocalisableString Silenced(string until, string reason)
            => new TranslatableString(getKey(@"silenced"), @"You are silenced until {0}: {1}. Click to see your account standing.", until, reason);

        /// <summary>
        /// "You are silenced until {0}. Click to see your account standing."
        /// </summary>
        public static LocalisableString SilencedNoReason(string until)
            => new TranslatableString(getKey(@"silenced_no_reason"), @"You are silenced until {0}. Click to see your account standing.", until);

        /// <summary>
        /// "Press again to confirm"
        /// </summary>
        public static LocalisableString PressAgainToConfirm => new TranslatableString(getKey(@"press_again_to_confirm"), @"Press again to confirm");

        /// <summary>
        /// "Confirm retry and quit"
        /// </summary>
        public static LocalisableString ConfirmRetryQuit => new TranslatableString(getKey(@"confirm_retry_quit"), @"Confirm retry and quit");

        /// <summary>
        /// "After a minute of play, retrying or quitting asks for a second press."
        /// </summary>
        public static LocalisableString ConfirmRetryQuitTooltip
            => new TranslatableString(getKey(@"confirm_retry_quit_tooltip"), @"After a minute of play, retrying or quitting asks for a second press.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
