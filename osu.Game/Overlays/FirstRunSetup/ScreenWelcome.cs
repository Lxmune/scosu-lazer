// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;

namespace osu.Game.Overlays.FirstRunSetup
{
    [LocalisableDescription(typeof(FirstRunSetupOverlayStrings), nameof(FirstRunSetupOverlayStrings.WelcomeTitle))]
    public partial class ScreenWelcome : WizardScreen
    {
        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig, OsuColour colours)
        {
            Content.Children = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    RowDimensions = new[]
                    {
                        // Avoid height changes when changing language.
                        new Dimension(GridSizeMode.AutoSize, minSize: 100),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: CONTENT_FONT_SIZE))
                            {
                                Text = FirstRunSetupOverlayStrings.WelcomeDescription,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            },
                        },
                    }
                },
                new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: CONTENT_FONT_SIZE, weight: FontWeight.Bold))
                {
                    Text = FirstRunSetupOverlayStrings.HeadsUpTitle,
                    Colour = colours.Yellow,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                },
                new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: CONTENT_FONT_SIZE))
                {
                    Text = FirstRunSetupOverlayStrings.HeadsUpDescription,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                },
                new SettingsCheckbox
                {
                    LabelText = GeneralSettingsStrings.PreferOriginalMetadataLanguage,
                    Current = frameworkConfig.GetBindable<bool>(FrameworkSetting.ShowUnicode)
                }
            };
        }

        public override LocalisableString? NextStepText => FirstRunSetupOverlayStrings.GetStarted;
    }
}
