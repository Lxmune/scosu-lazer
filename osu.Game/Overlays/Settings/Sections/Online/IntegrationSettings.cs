// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Overlays.Settings.Sections.Online
{
    public partial class IntegrationSettings : SettingsSubsection
    {
        protected override LocalisableString Header => OnlineSettingsStrings.IntegrationsHeader;

        private readonly IBindable<APIUser> localUser = new Bindable<APIUser>();

        private SettingsButtonV2 discordButton = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, IAPIProvider api, OsuGame? game)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<DiscordRichPresenceMode>
                {
                    Caption = OnlineSettingsStrings.DiscordRichPresence,
                    Current = config.GetBindable<DiscordRichPresenceMode>(OsuSetting.DiscordRichPresence)
                }),
                // scosu: players link Discord on the website (Discord's own login), for ScosuBot and their profile.
                discordButton = new SettingsButtonV2
                {
                    Action = () => game?.OpenUrlExternally($@"{api.Endpoints.WebsiteUrl}/home/account/edit?tab=account"),
                },
            };

            localUser.BindTo(api.LocalUser);
            discordButton.Keywords = new[] { @"discord", @"link", @"scosubot" };
            discordButton.TooltipText = ScosuIntegrationStrings.DiscordLinkTooltip;
            // LocalUser may change off the update thread (logging in).
            localUser.BindValueChanged(user => Schedule(() =>
            {
                // The server only sends a Discord name once its account is linked (refreshed when logging in).
                string? linked = user.NewValue?.Discord;
                discordButton.Text = string.IsNullOrEmpty(linked) ? ScosuIntegrationStrings.LinkDiscord : ScosuIntegrationStrings.DiscordLinked(linked);
            }), true);
        }
    }
}
