// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Ranking
{
    /// <summary>
    /// scosu: a replay downloaded for a score is found as that score's, even when its file carries another ID or none
    /// (the server serves older replays as they were stored).
    /// </summary>
    public partial class TestSceneDownloadedReplayMatching : OsuTestScene
    {
        private const long online_id = 424242;

        private BeatmapManager beatmapManager = null!;
        private ScoreManager scoreManager = null!;
        private TestScoreModelDownloader downloader = null!;

        private readonly Bindable<DownloadState> state = new Bindable<DownloadState>();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            RulesetStore rulesetStore;
            dependencies.Cache(rulesetStore = new RealmRulesetStore(Realm));
            dependencies.Cache(beatmapManager = new BeatmapManager(LocalStorage, Realm, null, dependencies.Get<AudioManager>(), Resources, dependencies.Get<GameHost>(), Beatmap.Default));
            dependencies.Cache(scoreManager = new ScoreManager(rulesetStore, () => beatmapManager, LocalStorage, Realm, API));
            dependencies.CacheAs<ScoreModelDownloader>(downloader = new TestScoreModelDownloader(scoreManager, API));
            dependencies.Cache(Realm);

            return dependencies;
        }

        [Test]
        public void TestReplayWithoutTheScoresIdIsMatched()
        {
            Live<ScoreInfo>? imported = null;

            AddStep("clear scores", () => scoreManager.Delete());
            AddStep("import beatmap", () => beatmapManager.Import(TestResources.GetQuickTestBeatmapForImport()).WaitSafely());

            AddStep("track the online score", () => Child = new ScoreDownloadTracker(new ScoreInfo { OnlineID = online_id })
            {
                State = { BindTarget = state },
            });
            AddUntilStep("not downloaded", () => state.Value, () => Is.EqualTo(DownloadState.NotDownloaded));

            AddStep("import its replay, which carries no ID", () =>
            {
                var beatmapInfo = beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps.First();

                imported = scoreManager.Import(new ScoreInfo
                {
                    OnlineID = -1,
                    LegacyOnlineID = -1,
                    Hash = Guid.NewGuid().ToString(),
                    Ruleset = new OsuRuleset().RulesetInfo,
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 10 },
                    User = API.LocalUser.Value,
                });
            });
            AddAssert("still not found", () => state.Value, () => Is.EqualTo(DownloadState.NotDownloaded));

            AddStep("finish the download", () => downloader.FinishImport(new ScoreInfo { OnlineID = online_id }, imported!));

            AddUntilStep("found", () => state.Value, () => Is.EqualTo(DownloadState.LocallyAvailable));
            AddAssert("the score has its online ID", () => scoreManager.Query(s => s.OnlineID == online_id), () => Is.Not.Null);
        }

        private class TestScoreModelDownloader : ScoreModelDownloader
        {
            public TestScoreModelDownloader(IModelImporter<ScoreInfo> scoreManager, IAPIProvider api)
                : base(scoreManager, api)
            {
            }

            public void FinishImport(IScoreInfo model, Live<ScoreInfo> imported) => PostImport(model, new[] { imported });
        }
    }
}
