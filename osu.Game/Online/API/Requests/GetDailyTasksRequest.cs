// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// scosu: gets the local user's daily tasks today, with their streaks (a scosu server endpoint).
    /// </summary>
    public class GetDailyTasksRequest : APIRequest<APIDailyTasks>
    {
        protected override string Target => @"me/daily-tasks";
    }
}
