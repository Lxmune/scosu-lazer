// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Overlays.Settings.Sections.Online
{
    /// <summary>
    /// scosu: downloads, in one click, the ranked maps of the selected mode the player hasn't played (or passed, full-combo'd, got S on)
    /// and doesn't have yet. The server lists them (see <see cref="GetMissingBeatmapSetsRequest"/>); the game leaves out the sets it has.
    /// Only on scosu servers.
    /// </summary>
    public partial class MissingBeatmapsSettings : SettingsSubsection
    {
        protected override LocalisableString Header => ScosuMissingBeatmapsStrings.Header;

        public override IEnumerable<LocalisableString> FilterTerms => base.FilterTerms.Concat(new LocalisableString[] { @"download", @"ranked", @"beatmaps" });

        public override bool IsPresent => base.IsPresent && api.Endpoints.RanksByStableScore;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private BeatmapModelDownloader downloader { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private readonly Bindable<MissingBeatmapsKind> kind = new Bindable<MissingBeatmapsKind>();

        private readonly BindableDouble minStars = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 10,
            Precision = 0.5,
        };

        private readonly BindableInt limit = new BindableInt(50)
        {
            MinValue = 10,
            MaxValue = 200,
            Precision = 10,
        };

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private SettingsButtonV2 downloadButton = null!;

        private GetMissingBeatmapSetsRequest? request;
        private List<APIBeatmapSet> toDownload = new List<APIBeatmapSet>();

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<MissingBeatmapsKind>
                {
                    Caption = ScosuMissingBeatmapsStrings.Kind,
                    HintText = ScosuMissingBeatmapsStrings.KindHint,
                    Current = kind,
                }),
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = ScosuMissingBeatmapsStrings.MinimumStars,
                    Current = minStars,
                    KeyboardStep = 0.5f,
                    TransferValueOnCommit = true,
                    LabelFormat = v => $@"{v:0.#}★",
                }),
                new SettingsItemV2(new FormSliderBar<int>
                {
                    Caption = ScosuMissingBeatmapsStrings.Limit,
                    Current = limit,
                    KeyboardStep = 10,
                    TransferValueOnCommit = true,
                }),
                downloadButton = new SettingsButtonV2
                {
                    Action = download,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (!api.Endpoints.RanksByStableScore)
                return;

            kind.BindValueChanged(_ => Scheduler.AddOnce(refresh));
            minStars.BindValueChanged(_ => Scheduler.AddOnce(refresh));
            limit.BindValueChanged(_ => Scheduler.AddOnce(showResult));
            ruleset.BindValueChanged(_ => Scheduler.AddOnce(refresh));

            // The API's state changes on its own thread.
            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(() => Scheduler.AddOnce(refresh)), true);
        }

        private List<APIBeatmapSet> missing = new List<APIBeatmapSet>();

        /// <summary>
        /// Asks the server for the sets again, e.g. after the filters changed.
        /// </summary>
        private void refresh()
        {
            request?.Cancel();
            request = null;
            missing = new List<APIBeatmapSet>();

            if (apiState.Value != APIState.Online || !ruleset.Value.IsLegacyRuleset())
            {
                setButton(ScosuMissingBeatmapsStrings.LogIn, false);
                return;
            }

            setButton(ScosuMissingBeatmapsStrings.Searching, false);

            var req = request = new GetMissingBeatmapSetsRequest(api.LocalUser.Value.Id, ruleset.Value, kind.Value, minStars.Value);

            // responses arrive on the API thread; the state and the button belong to the update thread.
            req.Success += response => Schedule(() =>
            {
                if (req != request)
                    return;

                missing = response.BeatmapSets;
                showResult();
            });
            req.Failure += _ => Schedule(() =>
            {
                if (req == request)
                    setButton(ScosuMissingBeatmapsStrings.Failed, false);
            });

            api.Queue(req);
        }

        /// <summary>
        /// Leaves out the sets the game has or is downloading, and offers the rest (up to <see cref="limit"/>).
        /// </summary>
        private void showResult()
        {
            if (request?.CompletionState != APIRequestCompletionState.Completed)
                return;

            var have = realm.Run(r => r.All<BeatmapSetInfo>()
                                       .Where(s => !s.DeletePending)
                                       .AsEnumerable()
                                       .Select(s => s.OnlineID)
                                       .ToHashSet());

            toDownload = missing.Where(s => !have.Contains(s.OnlineID) && downloader.GetExistingDownload(s) == null)
                                .Take(limit.Value)
                                .ToList();

            if (toDownload.Count == 0)
                setButton(ScosuMissingBeatmapsStrings.NothingMissing, false);
            else
                setButton(ScosuMissingBeatmapsStrings.Download(toDownload.Count), true);
        }

        private void download()
        {
            bool noVideo = config.Get<bool>(OsuSetting.PreferNoVideo);

            foreach (var set in toDownload)
                downloader.Download(set, noVideo);

            // The ones being downloaded are left out now.
            showResult();
        }

        private void setButton(LocalisableString text, bool enabled)
        {
            downloadButton.Text = text;
            downloadButton.Enabled.Value = enabled;
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
