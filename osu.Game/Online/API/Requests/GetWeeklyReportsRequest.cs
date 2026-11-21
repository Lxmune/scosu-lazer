// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets a user's weekly reports with their highlights, newest first. Only the user can read their own (a scosu server endpoint).
    /// </summary>
    public class GetWeeklyReportsRequest : ScosuWebAPIRequest<List<APIWeeklyReport>>
    {
        private readonly long userId;

        public GetWeeklyReportsRequest(long userId)
        {
            this.userId = userId;
        }

        protected override string Target => $@"users/{userId}/weekly-reports";
    }
}
