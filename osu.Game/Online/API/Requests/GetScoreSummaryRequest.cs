// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets the ranked score the local user gained today and this week (a scosu server endpoint).
    /// </summary>
    public class GetScoreSummaryRequest : APIRequest<APIScoreSummary>
    {
        public readonly IRulesetInfo Ruleset;

        /// <summary>
        /// The start of the user's day: their local midnight, with its UTC offset.
        /// </summary>
        public readonly DateTimeOffset Today;

        public GetScoreSummaryRequest(IRulesetInfo ruleset, DateTimeOffset today)
        {
            Ruleset = ruleset;
            Today = today;
        }

        /// <summary>
        /// The start of the user's day, in their time zone.
        /// </summary>
        public static DateTimeOffset LocalMidnight()
        {
            var today = DateTime.Today;
            return new DateTimeOffset(today, TimeZoneInfo.Local.GetUtcOffset(today));
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter("mode", Ruleset.ShortName);
            // Sent in UTC: the server only needs the instant, and a "+hh:mm" offset could be read as a space in a query string.
            req.AddParameter("today", Today.UtcDateTime.ToString(@"yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

            return req;
        }

        protected override string Target => @"me/score-summary";
    }
}
