// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Database;
using osu.Game.Graphics.Cursor;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Screens.Menu;
using osu.Game.Tests.Visual.Metadata;
using osu.Game.Users;

namespace osu.Game.Tests.Visual.Menus
{
    public partial class TestSceneOnlinePlayersDisplay : OsuTestScene
    {
        private TestMetadataClient metadataClient = null!;
        private OnlinePlayersDisplay display = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set up components", () =>
            {
                metadataClient = new TestMetadataClient();

                ((DummyAPIAccess)API).HandleRequest = req =>
                {
                    if (req is not LookupUsersRequest lookup)
                        return false;

                    lookup.TriggerSuccess(new GetUsersResponse
                    {
                        Users = lookup.UserIds.Select(id => new APIUser { Id = id, Username = $"Player {id}" }).ToList()
                    });
                    return true;
                };

                var userLookupCache = new UserLookupCache();

                Children = new Drawable[]
                {
                    metadataClient,
                    userLookupCache,
                    new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies = new (Type, object)[]
                        {
                            (typeof(MetadataClient), metadataClient),
                            (typeof(UserLookupCache), userLookupCache),
                        },
                        Child = new OsuContextMenuContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = display = new OnlinePlayersDisplay
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                State = { Value = Visibility.Visible },
                            },
                        },
                    },
                };
            });
        }

        [Test]
        public void TestPlayersComeAndGo()
        {
            AddAssert("nobody listed", () => !display.ChildrenOfType<UserPanel>().Any());

            AddStep("local user online", () => metadataClient.UserPresenceUpdated(API.LocalUser.Value.Id, online()));
            AddWaitStep("wait for lookups", 3);
            AddAssert("local user not listed", () => !display.ChildrenOfType<UserPanel>().Any());

            AddStep("two players online", () =>
            {
                metadataClient.UserPresenceUpdated(3001, online());
                metadataClient.UserPresenceUpdated(3002, new UserPresence { Status = UserStatus.Online, Activity = new UserActivity.InSoloGame() });
            });
            AddUntilStep("both listed", () => display.ChildrenOfType<UserPanel>().Select(p => p.User.Id).OrderBy(id => id), () => Is.EqualTo(new[] { 3001, 3002 }));

            AddStep("many players online", () =>
            {
                for (int id = 2000; id < 2010; id++)
                    metadataClient.UserPresenceUpdated(id, online());
            });
            AddUntilStep("six listed", () => display.ChildrenOfType<UserPanel>().Count(), () => Is.EqualTo(6));

            AddStep("one goes offline", () => metadataClient.UserPresenceUpdated(3001, null));
            AddUntilStep("no longer listed", () => display.ChildrenOfType<UserPanel>().All(p => p.User.Id != 3001));
        }

        [Test]
        public void TestWatchesPresenceOnlyWhileShown()
        {
            int ended = 0;
            int began = 0;

            AddStep("count watches", () =>
            {
                metadataClient.OnBeginWatchingUserPresence += () => began++;
                metadataClient.OnEndWatchingUserPresence += () => ended++;
            });
            AddStep("hide", () => display.Hide());
            AddAssert("stopped watching", () => ended, () => Is.EqualTo(1));
            AddStep("show", () => display.Show());
            AddAssert("watching again", () => began, () => Is.EqualTo(1));
        }

        private static UserPresence online() => new UserPresence { Status = UserStatus.Online, Activity = new UserActivity.ChoosingBeatmap() };
    }
}
