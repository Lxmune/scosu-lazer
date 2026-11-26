// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Select;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Navigation
{
    public partial class TestSceneButtonSystemNavigation : OsuGameTestScene
    {
        private ButtonSystem buttons => ((MainMenu)Game.ScreenStack.CurrentScreen).ChildrenOfType<ButtonSystem>().Single();

        private MainMenuButton exitButton => buttons.ChildrenOfType<MainMenuButton>().Single(b => b.TriggerKeys.Contains(Key.Q));

        [Test]
        public void TestGlobalActionHasPriority()
        {
            AddAssert("state is initial", () => buttons.State == ButtonSystemState.Initial);

            // triggering the cookie in the initial state with any key should only happen if no other action is bound to that key.
            // here, F10 is bound to GlobalAction.ToggleGameplayMouseButtons.
            AddStep("press F10", () => InputManager.Key(Key.F10));
            AddAssert("state is initial", () => buttons.State == ButtonSystemState.Initial);

            AddStep("press P", () => InputManager.Key(Key.P));
            AddAssert("state is top level", () => buttons.State == ButtonSystemState.TopLevel);
        }

        [Test]
        public void TestFastShortcutKeys()
        {
            AddAssert("state is initial", () => buttons.State == ButtonSystemState.Initial);

            AddStep("press P three times", () =>
            {
                InputManager.Key(Key.P);
                InputManager.Key(Key.P);
                InputManager.Key(Key.P);
            });

            AddAssert("entered song select", () => Game.ScreenStack.CurrentScreen is SoloSongSelect);
        }

        /// <summary>
        /// scosu: clicking exit asks for confirmation, as osu!stable does.
        /// </summary>
        [Test]
        public void TestClickingExitAsksForConfirmation()
        {
            AddUntilStep("wait for dialog overlay", () => Game.ChildrenOfType<DialogOverlay>().SingleOrDefault() != null);
            AddStep("enter menu", () => buttons.State = ButtonSystemState.TopLevel);
            AddUntilStep("exit button shown", () => exitButton.Alpha == 1 && !exitButton.Transforms.Any());
            AddStep("click exit", () =>
            {
                InputManager.MoveMouseTo(exitButton);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("exit dialog is shown", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog is ConfirmExitDialog);
            AddAssert("still in the menu", () => Game.ScreenStack.CurrentScreen is MainMenu);
        }

        [Test]
        public void TestShortcutKeys()
        {
            AddAssert("state is initial", () => buttons.State == ButtonSystemState.Initial);

            AddStep("press P", () => InputManager.Key(Key.P));
            AddAssert("state is top level", () => buttons.State == ButtonSystemState.TopLevel);

            AddStep("press P", () => InputManager.Key(Key.P));
            AddAssert("state is play", () => buttons.State == ButtonSystemState.Play);

            AddStep("press P", () => InputManager.Key(Key.P));
            AddAssert("entered song select", () => Game.ScreenStack.CurrentScreen is SoloSongSelect);
        }
    }
}
