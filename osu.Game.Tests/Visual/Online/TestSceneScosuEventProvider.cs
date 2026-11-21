// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Notifications.WebSocket;

namespace osu.Game.Tests.Visual.Online
{
    [HeadlessTest]
    public partial class TestSceneScosuEventProvider : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private ScosuEventProvider provider = null!;

        private int requests;
        private APIScosuEvent? response;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("set up responses", () =>
            {
                requests = 0;
                response = createEvent(DateTimeOffset.Now.AddDays(1));
                dummyAPI.HandleRequest = request =>
                {
                    if (request is not GetScosuEventRequest eventRequest)
                        return false;

                    requests++;
                    eventRequest.TriggerSuccess(new GetScosuEventRequest.CurrentEvent { Event = response });
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
        public void TestFetchedOnceAndOnRefresh()
        {
            createProvider();
            AddUntilStep("event fetched", () => provider.Event.Value?.Title, () => Is.EqualTo("Test Event"));
            AddAssert("one request", () => requests, () => Is.EqualTo(1));

            AddStep("refresh twice", () =>
            {
                provider.Refresh();
                provider.Refresh();
            });
            AddUntilStep("fetched again, once", () => requests, () => Is.EqualTo(2));
        }

        [Test]
        public void TestFetchedWhenTheServerSaysItChanged()
        {
            createProvider();
            AddUntilStep("event fetched", () => requests, () => Is.EqualTo(1));

            AddStep("event cancelled", () => response = null);
            AddStep("other message", () => dummyAPI.NotificationsClient.Receive(new SocketMessage { Event = "new" }));
            AddStep("event changed", () => dummyAPI.NotificationsClient.Receive(new SocketMessage { Event = ScosuEventProvider.UPDATED_MESSAGE }));
            AddUntilStep("no event any more", () => provider.Event.Value, () => Is.Null);
            AddAssert("fetched once more", () => requests, () => Is.EqualTo(2));
        }

        [Test]
        public void TestFetchedWhenTheCountdownEnds()
        {
            AddStep("event starts in a moment", () => response = createEvent(DateTimeOffset.Now.AddSeconds(1)));
            createProvider();
            AddUntilStep("event fetched", () => requests, () => Is.EqualTo(1));
            AddUntilStep("fetched again after the start", () => requests, () => Is.EqualTo(2));
        }

        [Test]
        public void TestNothingAskedOfOtherServers()
        {
            AddStep("other server", () => dummyAPI.Endpoints.RanksByStableScore = false);
            createProvider();
            AddWaitStep("wait", 3);
            AddAssert("no request", () => requests, () => Is.Zero);
            AddAssert("no event", () => provider.Event.Value, () => Is.Null);
        }

        private void createProvider() => AddStep("create provider", () => Child = provider = new ScosuEventProvider());

        private static APIScosuEvent createEvent(DateTimeOffset startsAt) => new APIScosuEvent
        {
            Id = 1,
            Slug = "test-event",
            Title = "Test Event",
            Status = ScosuEventStatus.Upcoming,
            StartsAt = startsAt,
            EndsAt = startsAt.AddDays(7),
        };
    }
}
