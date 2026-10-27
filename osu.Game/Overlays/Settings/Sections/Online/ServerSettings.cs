// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Overlays.Settings.Sections.Online
{
    public partial class ServerSettings : SettingsSubsection
    {
        protected override LocalisableString Header => OnlineSettingsStrings.ServerHeader;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly Bindable<string> serverUrl = new Bindable<string>(string.Empty);

        private SettingsItemV2 serverUrlItem = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            serverUrl.Value = config.Get<string>(OsuSetting.CustomServerUrl);

            FormTextBox textBox;

            Children = new Drawable[]
            {
                serverUrlItem = new SettingsItemV2(textBox = new FormTextBox
                {
                    Caption = OnlineSettingsStrings.ServerAddress,
                    HintText = OnlineSettingsStrings.ServerAddressHint(ProductionEndpointConfiguration.DEFAULT_SERVER_URL),
                    PlaceholderText = ProductionEndpointConfiguration.DEFAULT_SERVER_URL,
                    Current = serverUrl,
                })
                {
                    ShowRevertToDefaultButton = false,
                    Keywords = new[] { @"api", @"url", @"custom" },
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = SettingsPanel.CONTENT_PADDING,
                    Child = new OsuTextFlowContainer(s => s.Font = OsuFont.Style.Caption1.With(weight: FontWeight.SemiBold))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Colour = colours.Red1,
                        Text = OnlineSettingsStrings.ChangeServerRisk,
                    },
                },
            };

            textBox.OnCommit += (_, _) => commit();
        }

        private void commit()
        {
            if (!ProductionEndpointConfiguration.TryNormaliseServerUrl(serverUrl.Value, out string normalised))
            {
                serverUrlItem.Note.Value = new SettingsNote.Data(OnlineSettingsStrings.InvalidServerAddress, SettingsNote.Type.Critical);
                return;
            }

            string newValue = normalised == ProductionEndpointConfiguration.DEFAULT_SERVER_URL ? string.Empty : normalised;
            string currentValue = config.Get<string>(OsuSetting.CustomServerUrl);

            serverUrlItem.Note.Value = null;
            serverUrl.Value = newValue;

            if (newValue == currentValue)
                return;

            dialogOverlay?.Push(new ConfirmDialog(OnlineSettingsStrings.ChangeServerConfirmation, () =>
            {
                // The saved login belongs to the previous server and must never be sent to the new one.
                api.Logout();
                config.SetValue(OsuSetting.CustomServerUrl, newValue);

                if (game?.RestartAppWhenExited() == true)
                    game.AttemptExit();
                else
                    serverUrlItem.Note.Value = new SettingsNote.Data(OnlineSettingsStrings.RestartToChangeServer, SettingsNote.Type.Warning);
            }, () => serverUrl.Value = currentValue));
        }
    }
}
