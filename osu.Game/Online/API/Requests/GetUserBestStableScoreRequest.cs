// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// The local user's best score on a beatmap's global leaderboard, whose osu!stable score is what the beatmap adds to their ranked score
    /// on servers that rank plays by it (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public class GetUserBestStableScoreRequest : APIRequest<APIScoresCollection>
    {
        private readonly IBeatmapInfo beatmap;

        public readonly IRulesetInfo LeaderboardRuleset;

        /// <param name="beatmap">The beatmap.</param>
        /// <param name="leaderboardRuleset">The leaderboard: a ruleset, or one of its Relax or Autopilot leaderboards (see <see cref="RelaxLeaderboards"/>).</param>
        public GetUserBestStableScoreRequest(IBeatmapInfo beatmap, IRulesetInfo leaderboardRuleset)
        {
            this.beatmap = beatmap;
            LeaderboardRuleset = leaderboardRuleset;
        }

        /// <summary>
        /// The osu!stable score of the user's best, or <see langword="null"/> if they have none.
        /// </summary>
        public long? BestStableScore => Response?.UserScore?.Score.LegacyTotalScore;

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter(@"type", @"global");
            req.AddParameter(@"mode", LeaderboardRuleset.ShortName);
            // Only the user's own score is needed.
            req.AddParameter(@"limit", @"1");

            return req;
        }

        protected override string Target => $@"beatmaps/{beatmap.OnlineID}/scores";
    }
}
