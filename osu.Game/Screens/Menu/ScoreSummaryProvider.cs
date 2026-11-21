// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the ranked score gained today and this week (and the rank to pass next), for the main menu's cards and hub.
    /// Cached by <see cref="MainMenu"/> so that its displays share one request.
    /// <see cref="Summary"/> is null while logged out, offline, on a ruleset the server doesn't rank, or on servers without it.
    /// </summary>
    public partial class ScoreSummaryProvider : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        public IBindable<APIScoreSummary?> Summary => summary;

        private readonly Bindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScoreSummaryRequest? request;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            ruleset.BindValueChanged(_ => Refresh(), true);
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
                summary.Value = null;
                return;
            }

            var req = request = new GetScoreSummaryRequest(ruleset.Value, GetScoreSummaryRequest.LocalMidnight());

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
