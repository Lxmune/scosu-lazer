// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Overlays.FirstRunSetup;
using osu.Game.Screens;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneFirstRunScreenUIScale : OsuManualInputManagerTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Cached(typeof(BeatmapStore))]
        private BeatmapStore beatmapStore = new TestBeatmapStore();

        [Cached]
        private BackgroundScreenStack backgroundStack = new BackgroundScreenStack();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestToggleSongSelectConfigWithNestedSongSelect()
        {
            // the nested song select must not touch backgrounds when config changes (regression: crash toggling settings while first run setup is in background).
            AddWaitStep("wait for nested song select", 20);
            AddAssert("nested song select did not touch game background", () => backgroundStack.CurrentScreen, () => Is.Null);
            AddRepeatStep("toggle background blur", () => config.SetValue(OsuSetting.SongSelectBackgroundBlur, !config.Get<bool>(OsuSetting.SongSelectBackgroundBlur)), 3);
            AddStep("remove screen", () => Clear());
            AddStep("toggle after removal", () => config.SetValue(OsuSetting.SongSelectBackgroundBlur, !config.Get<bool>(OsuSetting.SongSelectBackgroundBlur)));
        }

        public TestSceneFirstRunScreenUIScale()
        {
            AddStep("load screen", () =>
            {
                Children = new Drawable[]
                {
                    backgroundStack,
                    new ScreenStack(new ScreenUIScale()),
                };
            });
        }
    }
}
