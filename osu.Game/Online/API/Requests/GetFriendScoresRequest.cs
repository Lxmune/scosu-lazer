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
    /// Gets the rank, ranked score and ranked score gained today of each of the local user's friends (a scosu server endpoint).
    /// </summary>
    public class GetFriendScoresRequest : APIRequest<APIFriendScores>
    {
        public readonly IRulesetInfo Ruleset;

        /// <summary>
        /// The start of the local user's day: their local midnight.
        /// </summary>
        public readonly DateTimeOffset Today;

        public GetFriendScoresRequest(IRulesetInfo ruleset, DateTimeOffset today)
        {
            Ruleset = ruleset;
            Today = today;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter("mode", Ruleset.ShortName);
            // Sent in UTC: the server only needs the instant, and a "+hh:mm" offset could be read as a space in a query string.
            req.AddParameter("today", Today.UtcDateTime.ToString(@"yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

            return req;
        }

        protected override string Target => @"me/friends/scores";
    }
}
