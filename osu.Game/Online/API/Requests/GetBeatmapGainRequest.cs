// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets the ranked score that beating a user's best on one beatmap would add, as the server estimates it (a scosu server endpoint).
    /// </summary>
    public class GetBeatmapGainRequest : ScosuWebAPIRequest<APIBeatmapGain>
    {
        private readonly long userId;
        private readonly IBeatmapInfo beatmap;

        /// <summary>
        /// A ruleset or one of its Relax or Autopilot leaderboards (see <see cref="RelaxLeaderboards"/>), whose short names the server knows.
        /// </summary>
        public readonly IRulesetInfo LeaderboardRuleset;

        public GetBeatmapGainRequest(long userId, IBeatmapInfo beatmap, IRulesetInfo leaderboardRuleset)
        {
            this.userId = userId;
            this.beatmap = beatmap;
            LeaderboardRuleset = leaderboardRuleset;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.AddParameter(@"mode", LeaderboardRuleset.ShortName);
            return req;
        }

        protected override string Target => $@"users/{userId}/progress/beatmap/{beatmap.OnlineID}";
    }
}
