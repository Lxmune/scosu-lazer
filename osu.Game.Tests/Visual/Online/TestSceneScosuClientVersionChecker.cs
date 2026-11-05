// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;

namespace osu.Game.Tests.Visual.Online
{
    [HeadlessTest]
    public partial class TestSceneScosuClientVersionChecker : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private NotificationOverlay notificationOverlay = null!;

        private int requests;
        private bool current;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("rank by stable score", () => dummyAPI.Endpoints.RanksByStableScore = true);
            AddStep("set up responses", () =>
            {
                requests = 0;
                current = false;
                dummyAPI.HandleRequest = request =>
                {
                    if (request is not GetScosuClientVersionRequest versionRequest)
                        return false;

                    requests++;
                    versionRequest.TriggerSuccess(new GetScosuClientVersionRequest.ClientVersion { Current = current });
                    return true;
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset server kind", () => dummyAPI.Endpoints.RanksByStableScore = false);
            AddStep("back online", () => dummyAPI.SetState(APIState.Online));
        }

        [Test]
        public void TestOutdatedClientToldOnce()
        {
            createChecker();
            AddUntilStep("told to update", () => notifications().Length, () => Is.EqualTo(1));

            AddStep("disconnect", () => dummyAPI.SetState(APIState.Offline));
            AddStep("reconnect", () => dummyAPI.SetState(APIState.Online));
            AddWaitStep("wait", 3);
            AddAssert("still told once", () => notifications().Length, () => Is.EqualTo(1));
        }

        [Test]
        public void TestCurrentClientNotTold()
        {
            AddStep("current build", () => current = true);
            createChecker();
            AddUntilStep("asked", () => requests, () => Is.EqualTo(1));
            AddWaitStep("wait", 3);
            AddAssert("nothing posted", () => notifications(), () => Is.Empty);
        }

        [Test]
        public void TestNothingAskedOfOtherServers()
        {
            AddStep("other server", () => dummyAPI.Endpoints.RanksByStableScore = false);
            createChecker();
            AddWaitStep("wait", 3);
            AddAssert("no request", () => requests, () => Is.Zero);
            AddAssert("nothing posted", () => notifications(), () => Is.Empty);
        }

        private void createChecker() => AddStep("create checker", () =>
        {
            notificationOverlay = new NotificationOverlay
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
            };

            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies = [(typeof(INotificationOverlay), notificationOverlay)],
                Children = new Drawable[]
                {
                    notificationOverlay,
                    new ScosuClientVersionChecker(),
                },
            };
        });

        private ScosuClientVersionChecker.OutdatedClientNotification[] notifications()
            => notificationOverlay.ChildrenOfType<ScosuClientVersionChecker.OutdatedClientNotification>().ToArray();
    }
}
