// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of scosu's main menu rank-up line and weekly report card, and of the website links in the login panel.
    /// </summary>
    public static class ScosuMenuStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuMenu";

        /// <summary>
        /// "Your week"
        /// </summary>
        public static LocalisableString WeeklyReportTitle => new TranslatableString(getKey(@"weekly_report_title"), @"Your week");

        /// <summary>
        /// "Weekly report"
        /// </summary>
        public static LocalisableString WeeklyReportLabel => new TranslatableString(getKey(@"weekly_report_label"), @"Weekly report");

        /// <summary>
        /// "Open report"
        /// </summary>
        public static LocalisableString OpenReport => new TranslatableString(getKey(@"open_report"), @"Open report");

        /// <summary>
        /// "Dismiss"
        /// </summary>
        public static LocalisableString Dismiss => new TranslatableString(getKey(@"dismiss"), @"Dismiss");

        /// <summary>
        /// "{0} ranked score"
        /// </summary>
        public static LocalisableString HighlightRankedScore(string gain) => new TranslatableString(getKey(@"highlight_ranked_score"), @"{0} ranked score", gain);

        /// <summary>
        /// "{0} {1} → {2}"
        /// </summary>
        public static LocalisableString HighlightChange(LocalisableString measure, string before, string value) => new TranslatableString(getKey(@"highlight_change"), @"{0} {1} → {2}", measure, before, value);

        /// <summary>
        /// "Weekly reports"
        /// </summary>
        public static LocalisableString WeeklyReports => new TranslatableString(getKey(@"weekly_reports"), @"Weekly reports");

        /// <summary>
        /// "Account standing &amp; appeal"
        /// </summary>
        public static LocalisableString AccountStanding => new TranslatableString(getKey(@"account_standing"), @"Account standing & appeal");

        /// <summary>
        /// "Account settings"
        /// </summary>
        public static LocalisableString AccountSettings => new TranslatableString(getKey(@"account_settings"), @"Account settings");

        /// <summary>
        /// "scosu server unreachable"
        /// </summary>
        public static LocalisableString ServerUnreachable => new TranslatableString(getKey(@"server_unreachable"), @"scosu server unreachable");

        /// <summary>
        /// "scosu server unreachable. Click to check Discord for status updates."
        /// </summary>
        public static LocalisableString ServerUnreachableNotification => new TranslatableString(getKey(@"server_unreachable_notification"), @"scosu server unreachable. Click to check Discord for status updates.");

        /// <summary>
        /// "Discord"
        /// </summary>
        public static LocalisableString Discord => new TranslatableString(getKey(@"discord"), @"Discord");

        /// <summary>
        /// "Register"
        /// </summary>
        public static LocalisableString RegisterOnWebsite => new TranslatableString(getKey(@"register_on_website"), @"Register");

        /// <summary>
        /// "{0} online"
        /// </summary>
        public static LocalisableString PlayersOnlineCount(int count) => new TranslatableString(getKey(@"players_online_count"), @"{0} online", count);

        /// <summary>
        /// "Offline"
        /// </summary>
        public static LocalisableString ServerStatusOffline => new TranslatableString(getKey(@"server_status_offline"), @"Offline");

        /// <summary>
        /// "Connecting"
        /// </summary>
        public static LocalisableString ServerStatusConnecting => new TranslatableString(getKey(@"server_status_connecting"), @"Connecting");

        /// <summary>
        /// "Unreachable"
        /// </summary>
        public static LocalisableString ServerStatusUnreachable => new TranslatableString(getKey(@"server_status_unreachable"), @"Unreachable");

        /// <summary>
        /// The name of a weekly report highlight's measure (<c>ur</c>, <c>accuracy</c>, ...), or the measure itself if it isn't known.
        /// </summary>
        public static LocalisableString MeasureName(string measure) => measure switch
        {
            @"ranked_score" => new TranslatableString(getKey(@"measure_ranked_score"), @"Ranked score"),
            @"accuracy" => new TranslatableString(getKey(@"measure_accuracy"), @"Accuracy"),
            @"pass_rate" => new TranslatableString(getKey(@"measure_pass_rate"), @"Pass rate"),
            @"stars" => new TranslatableString(getKey(@"measure_stars"), @"Stars"),
            @"ur" => new TranslatableString(getKey(@"measure_ur"), @"UR"),
            @"ur_spread" => new TranslatableString(getKey(@"measure_ur_spread"), @"UR spread"),
            @"offset" => new TranslatableString(getKey(@"measure_offset"), @"Offset"),
            @"stream_rush" => new TranslatableString(getKey(@"measure_stream_rush"), @"Stream rush"),
            @"spinner_rpm" => new TranslatableString(getKey(@"measure_spinner_rpm"), @"Spinner RPM"),
            _ => measure,
        };

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
