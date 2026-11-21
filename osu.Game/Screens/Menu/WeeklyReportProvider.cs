// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the local user's weekly reports, for the main menu's card and hub.
    /// Cached by <see cref="MainMenu"/> so that its displays share one request.
    /// <see cref="Reports"/> is null while logged out, offline, or on servers without reports.
    /// Which report counts as new is up to each display (see <see cref="ScosuMenuFormatting.FindUnseenReport"/>).
    /// </summary>
    public partial class WeeklyReportProvider : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public IBindable<IReadOnlyList<APIWeeklyReport>?> Reports => reports;

        private readonly Bindable<IReadOnlyList<APIWeeklyReport>?> reports = new Bindable<IReadOnlyList<APIWeeklyReport>?>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetWeeklyReportsRequest? request;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            Refresh();
        }

        /// <summary>
        /// Fetches the reports again, e.g. after a play.
        /// </summary>
        public void Refresh() => Scheduler.AddOnce(fetch);

        private void fetch()
        {
            request?.Cancel();
            request = null;

            // Only scosu servers have reports.
            if (!api.Endpoints.RanksByStableScore || apiState.Value != APIState.Online)
            {
                reports.Value = null;
                return;
            }

            var req = request = new GetWeeklyReportsRequest(api.LocalUser.Value.Id);

            req.Success += r =>
            {
                if (req == request)
                    reports.Value = r;
            };
            req.Failure += _ =>
            {
                if (req == request)
                    reports.Value = null;
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
