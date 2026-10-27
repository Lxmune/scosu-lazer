// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Rankings;
using osu.Game.Overlays.Rankings.Tables;
using osu.Game.Rulesets.Osu;
using osu.Game.Users;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneGainedRankings : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Green);

        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private readonly List<GainedRankingsPeriod> requestedPeriods = new List<GainedRankingsPeriod>();
        private readonly List<RankingsSortCriteria> requestedFilters = new List<RankingsSortCriteria>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set up API", () =>
            {
                requestedPeriods.Clear();
                requestedFilters.Clear();

                dummyAPI.HandleRequest = req =>
                {
                    if (req is not GetGainedRankingsRequest gainedRequest)
                        return false;

                    requestedPeriods.Add(gainedRequest.Period);
                    requestedFilters.Add(gainedRequest.Filter);

                    // This week has players, this month nobody yet.
                    gainedRequest.TriggerSuccess(new GetGainedRankingsResponse
                    {
                        Ranking = gainedRequest.Period == GainedRankingsPeriod.Week ? createRanking(3) : new List<APIGainedRanking>(),
                    });
                    return true;
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
        }

        [Test]
        public void TestPeriods()
        {
            AddStep("create layout", () => Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new GainedRankingsLayout
                {
                    Ruleset = { Value = new OsuRuleset().RulesetInfo },
                },
            });

            AddUntilStep("this week requested", () => requestedPeriods.SequenceEqual(new[] { GainedRankingsPeriod.Week }));
            AddUntilStep("table shown", () => this.ChildrenOfType<GainedTable>().Any());

            AddStep("select this month", () => this.ChildrenOfType<OverlaySortTabControl<GainedRankingsPeriod>>().Single().Current.Value = GainedRankingsPeriod.Month);
            AddUntilStep("this month requested", () => requestedPeriods.LastOrDefault() == GainedRankingsPeriod.Month);
            AddUntilStep("table hidden", () => !this.ChildrenOfType<GainedTable>().Any());

            AddAssert("everyone so far", () => requestedFilters.All(f => f == RankingsSortCriteria.All));
            AddStep("select friends", () => this.ChildrenOfType<RankingsSortTabControl>().Single().Current.Value = RankingsSortCriteria.Friends);
            AddUntilStep("friends requested", () => requestedFilters.LastOrDefault() == RankingsSortCriteria.Friends);
            AddAssert("same period", () => requestedPeriods.LastOrDefault() == GainedRankingsPeriod.Month);
        }

        [Test]
        public void TestTabOnlyOnScoreRankedServers()
        {
            AddStep("create header", () => Child = new RankingsOverlayHeader());
            AddAssert("no gained tab", () => !scopes().Contains(RankingsScope.Gained));

            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("create header", () => Child = new RankingsOverlayHeader());
            AddAssert("gained tab", () => scopes().Contains(RankingsScope.Gained));
            AddAssert("no performance tab", () => !scopes().Contains(RankingsScope.Performance));
        }

        private IEnumerable<RankingsScope> scopes() => this.ChildrenOfType<OsuTabControl<RankingsScope>>().Single().Items;

        private static List<APIGainedRanking> createRanking(int count) => Enumerable.Range(1, count).Select(i => new APIGainedRanking
        {
            User = new APIUser
            {
                Id = i,
                Username = $"player {i}",
                CountryCode = CountryCode.US,
                Active = true,
            },
            RankedScore = 10_000_000 / i,
            PlayCount = 20 / i,
        }).ToList();
    }
}
