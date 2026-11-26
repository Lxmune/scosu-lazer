// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// scosu: today's daily tasks, each with its progress, and how many are done. Opens the Daily tasks page on the website
    /// (where one task a day can be swapped). Plays complete the tasks on the server, so it asks again whenever the hub opens.
    /// </summary>
    public partial class HubTasksTile : HubTile
    {
        /// <summary>
        /// Opening the hub again within this doesn't ask again.
        /// </summary>
        private static readonly TimeSpan refresh_interval = TimeSpan.FromSeconds(30);

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetDailyTasksRequest? request;
        private DateTimeOffset? lastFetch;
        private APIDailyTasks? lastTasks;

        public HubTasksTile()
        {
            Eyebrow.Text = ScosuDailyStrings.DailyTasks.ToUpper();
            showPlaceholder(ScosuDailyStrings.LogInForTasks);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(state => Schedule(() =>
            {
                lastFetch = null;

                if (state.NewValue == APIState.Online)
                    RefreshIfStale();
                else
                {
                    request?.Cancel();
                    request = null;
                    showPlaceholder(api.IsLoggedIn ? ScosuDailyStrings.TasksUnavailable : ScosuDailyStrings.LogInForTasks);
                }
            }), true);
        }

        /// <summary>
        /// Fetches today's tasks, unless that was done a moment ago.
        /// </summary>
        public void RefreshIfStale()
        {
            // Only scosu servers have daily tasks.
            if (request != null || api.State.Value != APIState.Online || !api.Endpoints.RanksByStableScore)
                return;

            if (lastFetch != null && DateTimeOffset.Now - lastFetch < refresh_interval)
                return;

            var req = request = new GetDailyTasksRequest();

            req.Success += tasks => Schedule(() =>
            {
                if (req != request)
                    return;

                request = null;
                lastFetch = DateTimeOffset.Now;
                lastTasks = tasks;
                display(tasks);
            });
            req.Failure += _ => Schedule(() =>
            {
                if (req != request)
                    return;

                request = null;
                showPlaceholder(ScosuDailyStrings.TasksUnavailable);
            });

            api.Queue(req);
        }

        private void showPlaceholder(LocalisableString text)
        {
            lastTasks = null;
            TooltipText = default;
            ShowPlaceholder(FontAwesome.Solid.CheckSquare, text);
        }

        protected override void OnAccentHueChanged()
        {
            if (IsLoaded && lastTasks != null)
                display(lastTasks);
        }

        private void display(APIDailyTasks tasks)
        {
            if (tasks.Tasks.Count == 0)
            {
                showPlaceholder(ScosuDailyStrings.TasksUnavailable);
                return;
            }

            ShowContent();
            SetEyebrowDetail(ScosuDailyStrings.TasksDone(tasks.CompletedCount, tasks.Tasks.Count));

            foreach (var task in tasks.Tasks)
                Body.Add(new DailyTaskRow(task));

            SetFoot(null, default);
            SetAction(() => game?.OpenUrlExternally($@"{api.Endpoints.WebsiteUrl}/daily-tasks"));

            TooltipText = tasks.DayComplete
                ? LocalisableString.Interpolate($@"{ScosuDailyStrings.DayCounts} · {ScosuDailyStrings.DayStreak(tasks.Streaks.Daily)}")
                : ScosuDailyStrings.DayStreak(tasks.Streaks.Daily);
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
