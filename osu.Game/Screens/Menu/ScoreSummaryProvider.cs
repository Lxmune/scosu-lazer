// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the ranked score gained today and this week (and the rank to pass next), for the main menu's cards and hub.
    /// On the leaderboard the selected ruleset and mods play on: Relax and Autopilot have their own (see <see cref="RelaxLeaderboards"/>).
    /// Cached by <see cref="MainMenu"/> so that its displays share one request.
    /// <see cref="Summary"/> is null while logged out, offline, on a ruleset the server doesn't rank, or on servers without it.
    /// </summary>
    public partial class ScoreSummaryProvider : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        public IBindable<APIScoreSummary?> Summary => summary;

        private readonly Bindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScoreSummaryRequest? request;

        /// <summary>
        /// The leaderboard of the last fetch, so that a change of mods only fetches again when it moves to another one.
        /// </summary>
        private string? fetchedLeaderboard;

        /// <summary>
        /// The leaderboard the selected ruleset and mods play on ("osurx" with Relax, ...).
        /// </summary>
        private RulesetInfo leaderboard => ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym));

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            ruleset.BindValueChanged(_ => Refresh(), true);
            mods.BindValueChanged(_ =>
            {
                if (leaderboard.ShortName != fetchedLeaderboard)
                    Refresh();
            });
        }

        /// <summary>
        /// Fetches the summary again, e.g. after a play.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have the summary.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online || !ruleset.Value.IsLegacyRuleset())
            {
                fetchedLeaderboard = null;
                summary.Value = null;
                return;
            }

            var board = leaderboard;
            fetchedLeaderboard = board.ShortName;

            var req = request = new GetScoreSummaryRequest(board, GetScoreSummaryRequest.LocalMidnight());

            req.Success += s =>
            {
                if (req == request)
                    summary.Value = s;
            };
            req.Failure += _ =>
            {
                if (req == request)
                    summary.Value = null;
            };

            api.Queue(req);
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
