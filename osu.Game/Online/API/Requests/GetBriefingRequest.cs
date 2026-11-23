// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// scosu: what changed since the local user's previous game session on <see cref="Ruleset"/>'s leaderboard (a scosu server
    /// endpoint). Asking starts a new session on the server, so the game asks once per launch.
    /// </summary>
    public class GetBriefingRequest : APIRequest<APIBriefing>
    {
        /// <summary>
        /// The leaderboard (Relax and Autopilot have their own, see <see cref="RelaxLeaderboards"/>).
        /// </summary>
        public readonly IRulesetInfo Ruleset;

        public GetBriefingRequest(IRulesetInfo ruleset)
        {
            Ruleset = ruleset;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.AddParameter("mode", Ruleset.ShortName);
            return req;
        }

        protected override string Target => @"me/briefing";
    }
}
