// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The latest weekly report: "your week is ready" while the user hasn't seen it, else its headline. Opens it on the website.
    /// </summary>
    public partial class HubReportTile : HubTile
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private WeeklyReportProvider? provider { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<IReadOnlyList<APIWeeklyReport>?> reports = new Bindable<IReadOnlyList<APIWeeklyReport>?>();
        private readonly Bindable<string> seenWeek = new Bindable<string>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        public HubReportTile()
        {
            Eyebrow.Text = MenuHubStrings.WeeklyReport.ToUpper();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (provider != null)
                reports.BindTo(provider.Reports);

            config.BindWith(OsuSetting.ScosuSeenWeeklyReport, seenWeek);

            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(refresh));
            seenWeek.BindValueChanged(_ => refresh());
            reports.BindValueChanged(_ => refresh(), true);
        }

        private void refresh()
        {
            if (reports.Value is not IReadOnlyList<APIWeeklyReport> list)
            {
                ShowPlaceholder(api.IsLoggedIn ? MenuHubStrings.NoReport : MenuHubStrings.LogInForReport);
                return;
            }

            var unseen = ScosuMenuFormatting.FindUnseenReport(list, seenWeek.Value, DateTimeOffset.Now);

            // Only a report the user has already seen falls back to the latest one.
            var report = unseen ?? list.OrderByDescending(r => r.WeekStart, StringComparer.Ordinal).FirstOrDefault();

            if (report == null)
            {
                ShowPlaceholder(MenuHubStrings.NoReport);
                return;
            }

            Body.Clear();

            var highlight = ScosuMenuFormatting.PickHighlights(report).FirstOrDefault();

            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = unseen != null || highlight == null ? MenuHubStrings.WeekIsReady : ScosuMenuFormatting.HighlightText(highlight),
                Font = ScosuMenuStyle.Display(16),
            });

            SetFoot(FontAwesome.Solid.FileAlt, MenuHubStrings.ReadOnWebsite);
            SetAction(() => open(report, unseen != null));
        }

        private void open(APIWeeklyReport report, bool wasUnseen)
        {
            game?.OpenUrlExternally($@"{api.Endpoints.WebsiteUrl}/weekly-reports/{report.WeekStart}");

            // Same as the corner card: opening the report counts as seeing it.
            if (wasUnseen && string.CompareOrdinal(report.WeekStart, seenWeek.Value) > 0)
                seenWeek.Value = report.WeekStart;
        }
    }
}
