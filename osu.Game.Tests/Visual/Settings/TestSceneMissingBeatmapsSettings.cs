// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.Online;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Tests.Online;

namespace osu.Game.Tests.Visual.Settings
{
    public partial class TestSceneMissingBeatmapsSettings : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Cached(typeof(IBindable<RulesetInfo>))]
        private readonly Bindable<RulesetInfo> selectedRuleset = new Bindable<RulesetInfo>(new OsuRuleset().RulesetInfo);

        private TestScenePlaylistsBeatmapAvailabilityTracker.TestBeatmapModelDownloader downloader = null!;

        private MissingBeatmapsSettings settings = null!;

        private GetMissingBeatmapSetsRequest? lastRequest;
        private readonly List<int> started = new List<int>();

        private SettingsButtonV2 button => settings.ChildrenOfType<SettingsButtonV2>().Single();

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.CacheAs<BeatmapModelDownloader>(downloader = new TestScenePlaylistsBeatmapAvailabilityTracker.TestBeatmapModelDownloader(null!, API));
            downloader.DownloadBegan += r => started.Add(r.Model.OnlineID);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("reset", () =>
            {
                lastRequest = null;
                started.Clear();
                Realm.Write(r => r.RemoveAll<BeatmapSetInfo>());
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestButtonShowsCountWithoutLocalSets()
        {
            respondWith(1, 2, 3);
            createSettings();

            AddUntilStep("button offers 3", () => button.Text.ToString(), () => Is.EqualTo("Download 3 maps you don't have"));
            AddAssert("button enabled", () => button.Enabled.Value);
            AddAssert("asked for missing sets", () => lastRequest, () => Is.Not.Null);
        }

        [Test]
        public void TestLocalSetsLeftOut()
        {
            AddStep("add local sets", () => Realm.Write(r =>
            {
                foreach (int id in new[] { 1, 3 })
                    r.Add(new BeatmapSetInfo { OnlineID = id });
            }));

            respondWith(1, 2, 3, 4);
            createSettings();

            AddUntilStep("button offers 2", () => button.Text.ToString(), () => Is.EqualTo("Download 2 maps you don't have"));
        }

        [Test]
        public void TestNothingMissing()
        {
            AddStep("add local set", () => Realm.Write(r =>
            {
                r.Add(new BeatmapSetInfo { OnlineID = 1 });
            }));

            respondWith(1);
            createSettings();

            AddUntilStep("nothing to download", () => button.Text.ToString(), () => Is.EqualTo("You have every map that matches"));
            AddAssert("button disabled", () => !button.Enabled.Value);
        }

        [Test]
        public void TestNothingOnOtherServers()
        {
            AddStep("other server", () => dummyAPI.Endpoints.RanksByStableScore = false);
            respondWith(1, 2);
            createSettings();

            AddWaitStep("wait", 3);
            AddAssert("no request", () => lastRequest, () => Is.Null);
            AddAssert("subsection hidden", () => settings.IsPresent, () => Is.False);
            AddAssert("button not offering anything", () => button.Text.ToString(), () => Does.Not.Contain("Download"));
        }

        [Test]
        public void TestFailure()
        {
            AddStep("fail requests", () => dummyAPI.HandleRequest = request =>
            {
                if (request is not GetMissingBeatmapSetsRequest missingRequest)
                    return false;

                lastRequest = missingRequest;
                missingRequest.TriggerFailure(new WebException());
                return true;
            });
            createSettings();

            AddUntilStep("button disabled", () => button.Enabled.Value, () => Is.False);
            AddAssert("not offering a download", () => button.Text.ToString(), () => Does.Not.Contain("Download"));
        }

        [Test]
        public void TestClickQueuesDownloads()
        {
            AddStep("add local set", () => Realm.Write(r =>
            {
                r.Add(new BeatmapSetInfo { OnlineID = 2 });
            }));

            respondWith(1, 2, 3);
            createSettings();

            AddUntilStep("button offers 2", () => button.Text.ToString(), () => Is.EqualTo("Download 2 maps you don't have"));
            AddStep("click button", () => button.TriggerClick());
            AddAssert("downloads of the missing sets began", () => started.OrderBy(i => i).ToArray(), () => Is.EqualTo(new[] { 1, 3 }));
            AddUntilStep("nothing left to offer", () => button.Text.ToString(), () => Is.EqualTo("You have every map that matches"));
        }

        private void respondWith(params int[] setIds) => AddStep("set up response", () => dummyAPI.HandleRequest = request =>
        {
            if (request is not GetMissingBeatmapSetsRequest missingRequest)
                return false;

            lastRequest = missingRequest;
            missingRequest.TriggerSuccess(new GetMissingBeatmapSetsRequest.Response
            {
                BeatmapSets = setIds.Select(id => new APIBeatmapSet { OnlineID = id, Title = $"Set {id}", Artist = "Artist" }).ToList(),
            });
            return true;
        });

        private void createSettings() => AddStep("create settings", () => Child = new OsuScrollContainer
        {
            RelativeSizeAxes = Axes.Both,
            Child = settings = new MissingBeatmapsSettings
            {
                RelativeSizeAxes = Axes.X,
            },
        });
    }
}
