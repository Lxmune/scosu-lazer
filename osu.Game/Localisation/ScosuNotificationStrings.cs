// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the notifications scosu's server sends that osu!lazer doesn't know (see <c>ScosuServerNotifier</c>).
    /// </summary>
    public static class ScosuNotificationStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuNotification";

        /// <summary>
        /// "Your map request for {0} was {1}"
        /// </summary>
        public static LocalisableString MapRequestAccepted(string title, string status) => new TranslatableString(getKey(@"map_request_accepted"), @"Your map request for {0} was {1}", title, status);

        /// <summary>
        /// "Your map request for {0} was declined: {1}"
        /// </summary>
        public static LocalisableString MapRequestDeclined(string title, string reason) => new TranslatableString(getKey(@"map_request_declined"), @"Your map request for {0} was declined: {1}", title, reason);

        /// <summary>
        /// "Your weekly report is ready"
        /// </summary>
        public static LocalisableString WeeklyReportReady => new TranslatableString(getKey(@"weekly_report_ready"), @"Your weekly report is ready");

        /// <summary>
        /// "Your restriction appeal was accepted"
        /// </summary>
        public static LocalisableString AppealAccepted => new TranslatableString(getKey(@"appeal_accepted"), @"Your restriction appeal was accepted");

        /// <summary>
        /// "Your restriction appeal was denied: {0}"
        /// </summary>
        public static LocalisableString AppealDenied(string reply) => new TranslatableString(getKey(@"appeal_denied"), @"Your restriction appeal was denied: {0}", reply);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
