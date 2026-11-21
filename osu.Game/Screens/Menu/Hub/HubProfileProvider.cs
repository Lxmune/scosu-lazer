// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The local user's profile, best play and recent plays for the hub's tiles: one request each, shared by every tile that shows part of them.
    /// Asked for again when the hub opens, but not more often than <see cref="refresh_interval"/> (or when the leaderboard or the user changed).
    /// On the leaderboard the selected ruleset and mods play on: Relax and Autopilot have their own (see <see cref="RelaxLeaderboards"/>).
    /// </summary>
    public partial class HubProfileProvider : Component
    {
        private static readonly TimeSpan refresh_interval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How many recent plays are fetched.
        /// </summary>
        public const int RECENT_COUNT = 25;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        /// <summary>
        /// Null while logged out, offline or until the first answer.
        /// </summary>
        public IBindable<APIUser?> User => user;

        public IBindable<IReadOnlyList<SoloScoreInfo>?> Best => best;

        public IBindable<IReadOnlyList<SoloScoreInfo>?> Recent => recent;

        private readonly Bindable<APIUser?> user = new Bindable<APIUser?>();
        private readonly Bindable<IReadOnlyList<SoloScoreInfo>?> best = new Bindable<IReadOnlyList<SoloScoreInfo>?>();
        private readonly Bindable<IReadOnlyList<SoloScoreInfo>?> recent = new Bindable<IReadOnlyList<SoloScoreInfo>?>();

        private APIRequest? userRequest;
        private APIRequest? bestRequest;
        private APIRequest? recentRequest;

        private DateTimeOffset? lastFetch;
        private int lastRuleset = -1;
        private long lastUser = -1;

        public void RefreshIfStale()
        {
            if (!api.IsLoggedIn || api.State.Value != APIState.Online)
            {
                user.Value = null;
                best.Value = null;
                recent.Value = null;
                lastFetch = null;
                return;
            }

            long userId = api.LocalUser.Value.Id;
            var rulesetInfo = ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym));
            bool same = lastRuleset == rulesetInfo.OnlineID && lastUser == userId;

            if (same && lastFetch != null && DateTimeOffset.Now - lastFetch < refresh_interval)
                return;

            if (!same)
            {
                user.Value = null;
                best.Value = null;
                recent.Value = null;
            }

            lastFetch = DateTimeOffset.Now;
            lastRuleset = rulesetInfo.OnlineID;
            lastUser = userId;

            cancel();

            var userReq = new GetUserRequest(userId, rulesetInfo);
            userReq.Success += u => Schedule(() =>
            {
                if (userReq == userRequest)
                    user.Value = u;
            });
            userRequest = userReq;
            api.Queue(userReq);

            var bestReq = new GetUserScoresRequest(userId, ScoreType.Best, new PaginationParameters(1), rulesetInfo);
            bestReq.Success += s => Schedule(() =>
            {
                if (bestReq == bestRequest)
                    best.Value = s;
            });
            bestRequest = bestReq;
            api.Queue(bestReq);

            var recentReq = new GetUserScoresRequest(userId, ScoreType.Recent, new PaginationParameters(RECENT_COUNT), rulesetInfo);
            recentReq.Success += s => Schedule(() =>
            {
                if (recentReq == recentRequest)
                    recent.Value = s;
            });
            recentRequest = recentReq;
            api.Queue(recentReq);
        }

        private void cancel()
        {
            userRequest?.Cancel();
            bestRequest?.Cancel();
            recentRequest?.Cancel();
        }

        protected override void Dispose(bool isDisposing)
        {
            cancel();
            base.Dispose(isDisposing);
        }
    }
}
