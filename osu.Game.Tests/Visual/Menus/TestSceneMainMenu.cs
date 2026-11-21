// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Menu.Hub;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Menus
{
    public partial class TestSceneMainMenu : OsuGameTestScene
    {
        private OnlineMenuBanner onlineMenuBanner => Game.ChildrenOfType<OnlineMenuBanner>().Single();

        [Test]
        public void TestVolumeControlViaScroll()
        {
            AddUntilStep("wait for volume overlay loaded", () => Game.ChildrenOfType<VolumeOverlay>().Single().IsLoaded);
            AddStep("reset volume", () => Audio.Volume.Value = 1);

            AddStep("mouse in centre", () => InputManager.MoveMouseTo(Game.ScreenSpaceDrawQuad.Centre));

            AddRepeatStep("scroll down", () => InputManager.ScrollVerticalBy(-10), 5);
            AddAssert("volume lowered down", () => Audio.Volume.Value < 1);
            AddRepeatStep("scroll up", () => InputManager.ScrollVerticalBy(10), 5);
            AddAssert("volume raised up", () => Audio.Volume.Value == 1);
        }

        [Test]
        public void TestOnlineMenuBannerTrusted()
        {
            AddStep("don't fetch online content", () => onlineMenuBanner.FetchOnlineContent = false);
            AddStep("disable return to top on idle", () => Game.ChildrenOfType<ButtonSystem>().Single().ReturnToTopOnIdle = false);
            AddStep("set online content", () => onlineMenuBanner.Current.Value = new APIMenuContent
            {
                Images = new[]
                {
                    new APIMenuImage
                    {
                        Image = @"https://assets.ppy.sh/main-menu/project-loved-2@2x.png",
                        Url = $@"{API.Endpoints.WebsiteUrl}/home/news/2023-12-21-project-loved-december-2023",
                    }
                }
            });
            AddAssert("system title not visible", () => onlineMenuBanner.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddStep("enter menu", () => InputManager.Key(Key.Enter));
            AddUntilStep("system title visible", () => onlineMenuBanner.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("image loaded", () => onlineMenuBanner.ChildrenOfType<OnlineMenuBanner.MenuImage>().FirstOrDefault()?.IsLoaded, () => Is.True);

            AddStep("click banner", () =>
            {
                InputManager.MoveMouseTo(onlineMenuBanner);
                InputManager.Click(MouseButton.Left);
            });

            // Might not catch every occurrence due to async nature, but works in manual testing and saves annoying test setup.
            AddAssert("no dialog", () => Game.ChildrenOfType<DialogOverlay>().SingleOrDefault()?.CurrentDialog == null);
        }

        [Test]
        public void TestOnlineMenuBannerUntrustedDomain()
        {
            AddStep("don't fetch online content", () => onlineMenuBanner.FetchOnlineContent = false);
            AddStep("disable return to top on idle", () => Game.ChildrenOfType<ButtonSystem>().Single().ReturnToTopOnIdle = false);
            AddStep("set online content", () => onlineMenuBanner.Current.Value = new APIMenuContent
            {
                Images = new[]
                {
                    new APIMenuImage
                    {
                        Image = @"https://assets.ppy.sh/main-menu/project-loved-2@2x.png",
                        Url = @"https://google.com",
                    }
                }
            });
            AddAssert("system title not visible", () => onlineMenuBanner.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddStep("enter menu", () => InputManager.Key(Key.Enter));
            AddUntilStep("system title visible", () => onlineMenuBanner.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("image loaded", () => onlineMenuBanner.ChildrenOfType<OnlineMenuBanner.MenuImage>().FirstOrDefault()?.IsLoaded, () => Is.True);

            AddStep("click banner", () =>
            {
                InputManager.MoveMouseTo(onlineMenuBanner);
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("wait for dialog", () => Game.ChildrenOfType<DialogOverlay>().SingleOrDefault()?.CurrentDialog != null);
        }

        [Test]
        public void TestHub()
        {
            MenuHubPage hub() => Game.ChildrenOfType<MenuHubPage>().Single();
            HubBar bar() => Game.ChildrenOfType<HubBar>().Single();
            HubDock dock() => Game.ChildrenOfType<HubDock>().Single();
            ScoreSummaryDisplay score() => Game.ChildrenOfType<ScoreSummaryDisplay>().Single();
            ButtonSystem buttons() => Game.ChildrenOfType<ButtonSystem>().Single();

            AddStep("disable return to top on idle", () => buttons().ReturnToTopOnIdle = false);

            // The test game opens with the band out.
            AddStep("go to the logo", () => buttons().State = ButtonSystemState.Initial);
            AddAssert("no bar on the logo", () => bar().State.Value, () => Is.EqualTo(Visibility.Hidden));

            // Any key on the logo brings the band out (osu! does this), and Tab must not also open the hub then.
            AddStep("press tab on the logo", () => InputManager.Key(Key.Tab));
            AddAssert("band out", () => buttons().State, () => Is.EqualTo(ButtonSystemState.TopLevel));
            AddAssert("hub not opened by that key", () => hub().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("enter menu", () => buttons().State = ButtonSystemState.TopLevel);
            AddAssert("bar shown with the band", () => bar().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press tab", () => InputManager.Key(Key.Tab));
            AddAssert("hub open", () => hub().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("bar stays", () => bar().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("band docked", () => dock().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("first screen scrolled away", () => score().Parent!.Y, () => Is.EqualTo(-1));

            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddAssert("hub closed", () => hub().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("band still out", () => buttons().State, () => Is.EqualTo(ButtonSystemState.TopLevel));
            AddAssert("band undocked", () => dock().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddUntilStep("first screen back", () => score().Parent!.Y, () => Is.EqualTo(0));

            AddStep("press H", () => InputManager.Key(Key.H));
            AddAssert("hub open", () => hub().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("open play's options", () => buttons().State = ButtonSystemState.Play);
            AddAssert("hub stays open with the band", () => hub().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("go back to the logo", () => buttons().State = ButtonSystemState.Initial);
            AddAssert("hub closed with the band", () => hub().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("band undocked with it", () => dock().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("bar hidden with the band", () => bar().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void TestDockGoesBackUp()
        {
            MenuHubPage hub() => Game.ChildrenOfType<MenuHubPage>().Single();
            HubDock.DockButton backUp() => Game.ChildrenOfType<HubDock.DockButton>().Single(b => b.Item.Text == MenuHubStrings.BackUp);
            ButtonSystem buttons() => Game.ChildrenOfType<ButtonSystem>().Single();

            AddStep("disable return to top on idle", () => buttons().ReturnToTopOnIdle = false);
            AddStep("enter menu", () => buttons().State = ButtonSystemState.TopLevel);
            AddStep("press tab", () => InputManager.Key(Key.Tab));
            AddUntilStep("dock shown", () => backUp().IsPresent);

            AddStep("click back up", () =>
            {
                InputManager.MoveMouseTo(backUp());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("hub closed", () => hub().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("band still out", () => buttons().State, () => Is.EqualTo(ButtonSystemState.TopLevel));
        }

        [Test]
        public void TestHubOpensWithTheWheel()
        {
            MenuHubPage hub() => Game.ChildrenOfType<MenuHubPage>().Single();
            ButtonSystem buttons() => Game.ChildrenOfType<ButtonSystem>().Single();

            AddStep("disable return to top on idle", () => buttons().ReturnToTopOnIdle = false);
            AddStep("enter menu", () => buttons().State = ButtonSystemState.TopLevel);

            // Above the band, away from the cards.
            AddStep("mouse to an empty spot", () => InputManager.MoveMouseTo(Game.ScreenSpaceDrawQuad.TopLeft + Game.ScreenSpaceDrawQuad.Size * new Vector2(0.6f, 0.15f)));

            AddStep("scroll down", () => InputManager.ScrollVerticalBy(-1));
            AddAssert("hub open", () => hub().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("scroll up", () => InputManager.ScrollVerticalBy(1));
            AddAssert("hub closed", () => hub().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }
    }
}
