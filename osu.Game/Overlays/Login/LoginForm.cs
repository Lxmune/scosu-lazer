// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Overlays.Settings;
using osu.Game.Resources.Localisation.Web;
using osuTK;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Login
{
    public partial class LoginForm : FillFlowContainer
    {
        private TextBox username = null!;
        private TextBox password = null!;
        private ShakeContainer shakeSignIn = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public Action? RequestHide;

        public override bool AcceptsFocus => true;

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(OsuConfigManager config, AccountCreationOverlay accountCreation)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, SettingsSection.ITEM_SPACING);

            ErrorTextFlowContainer errorText;
            LinkFlowContainer forgottenPasswordLink;

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = LoginPanelStrings.Account.ToUpper(),
                            Font = OsuFont.GetFont(weight: FontWeight.Bold),
                        },
                        username = new OsuTextBox
                        {
                            InputProperties = new TextInputProperties(TextInputType.Username, false),
                            PlaceholderText = UsersStrings.LoginUsername.ToLower(),
                            RelativeSizeAxes = Axes.X,
                            Text = api.ProvidedUsername,
                            TabbableContentContainer = this
                        },
                        password = new OsuPasswordTextBox
                        {
                            PlaceholderText = UsersStrings.LoginPassword.ToLower(),
                            RelativeSizeAxes = Axes.X,
                            TabbableContentContainer = this,
                        },
                        errorText = new ErrorTextFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Alpha = 0,
                        },
                    },
                },
                new SettingsCheckbox
                {
                    LabelText = LoginPanelStrings.RememberUsername,
                    Current = config.GetBindable<bool>(OsuSetting.SaveUsername),
                },
                new SettingsCheckbox
                {
                    LabelText = LoginPanelStrings.StaySignedIn,
                    Current = config.GetBindable<bool>(OsuSetting.SavePassword),
                },
                forgottenPasswordLink = new LinkFlowContainer
                {
                    Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        shakeSignIn = new ShakeContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Child = new SettingsButton
                            {
                                Text = UsersStrings.LoginButton,
                                Action = performLogin
                            },
                        }
                    }
                },
                new SettingsButton
                {
                    Text = LoginPanelStrings.Register,
                    Action = () =>
                    {
                        RequestHide?.Invoke();
                        accountCreation.Show();
                    }
                }
            };

            // scosu: point to scosu-web's routes and provide quick links to register and Discord.
            string forgotUrl = api.Endpoints.RanksByStableScore
                ? $@"{api.Endpoints.WebsiteUrl}/forgot-password"
                : $@"{api.Endpoints.WebsiteUrl}/home/password-reset";

            forgottenPasswordLink.AddLink(LayoutStrings.PopupLoginLoginForgot, forgotUrl);
            forgottenPasswordLink.AddText(@" · ");
            forgottenPasswordLink.AddLink(ScosuMenuStrings.RegisterOnWebsite, $@"{api.Endpoints.WebsiteUrl}/register");
            forgottenPasswordLink.AddText(@" · ");
            forgottenPasswordLink.AddLink(ScosuMenuStrings.Discord, ProductionEndpointConfiguration.DISCORD_URL);

            password.OnCommit += (_, _) => performLogin();

            if (api.LastLoginError is Exception loginError)
            {
                errorText.Alpha = 1;

                if (IsUnreachableError(loginError))
                {
                    errorText.AddErrors(new[] { ScosuMenuStrings.ServerUnreachable.ToString() });
                    forgottenPasswordLink.NewParagraph();
                    forgottenPasswordLink.AddText(@"Status & help: ");
                    forgottenPasswordLink.AddLink(ScosuMenuStrings.Discord, ProductionEndpointConfiguration.DISCORD_URL);
                }
                else if (loginError.Message is string error)
                {
                    errorText.AddErrors(new[] { error });
                }
            }
        }

        /// <summary>
        /// scosu: whether a login failure looks like the server or network being unreachable (DNS/socket/timeout, 5xx or Cloudflare tunnel errors)
        /// rather than wrong credentials.
        /// </summary>
        internal static bool IsUnreachableError(Exception? error)
        {
            for (var current = error; current != null; current = current.InnerException)
            {
                if (current is APIException { StatusCode: HttpStatusCode status })
                    return (int)status >= 500;

                if (current is WebException webException)
                {
                    if (webException.Response is HttpWebResponse response)
                        return (int)response.StatusCode >= 500;

                    if (webException.Status != WebExceptionStatus.ProtocolError)
                        return true;
                }

                if (current is HttpRequestException httpException)
                    return httpException.StatusCode == null || (int)httpException.StatusCode >= 500;

                if (current is TaskCanceledException or TimeoutException)
                    return true;

                string msg = current.Message;

                if (msg.Contains(@"502", StringComparison.Ordinal)
                    || msg.Contains(@"503", StringComparison.Ordinal)
                    || msg.Contains(@"504", StringComparison.Ordinal)
                    || msg.Contains(@"1033", StringComparison.Ordinal)
                    || msg.Contains(@"Unable to connect", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains(@"timed out", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains(@"Name or service not known", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains(@"No such host", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void performLogin()
        {
            if (!string.IsNullOrEmpty(username.Text) && !string.IsNullOrEmpty(password.Text))
                api.Login(username.Text, password.Text);
            else
                shakeSignIn.Shake();
        }

        protected override bool OnClick(ClickEvent e) => true;

        protected override void OnFocus(FocusEvent e)
        {
            Schedule(() => { GetContainingFocusManager()!.ChangeFocus(string.IsNullOrEmpty(username.Text) ? username : password); });
        }
    }
}
