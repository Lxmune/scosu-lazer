// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays.Settings;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Overlays.Login
{
    public partial class LoginPanel : Container
    {
        private bool bounding = true;

        private Drawable? form;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private UserDropdown? dropdown;

        /// <summary>
        /// Called to request a hide of a parent displaying this container.
        /// </summary>
        public Action? RequestHide;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly Bindable<UserStatus> configUserStatus = new Bindable<UserStatus>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        public override RectangleF BoundingBox => bounding ? base.BoundingBox : RectangleF.Empty;

        public bool Bounding
        {
            get => bounding;
            set
            {
                bounding = value;
                Invalidate(Invalidation.MiscGeometry);
            }
        }

        public LoginPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            config.BindWith(OsuSetting.UserOnlineStatus, configUserStatus);
            configUserStatus.BindValueChanged(e => updateDropdownCurrent(e.NewValue), true);

            apiState.BindTo(api.State);
            apiState.BindValueChanged(onlineStateChanged, true);
        }

        private void onlineStateChanged(ValueChangedEvent<APIState> state) => Schedule(() =>
        {
            form = null;

            switch (state.NewValue)
            {
                case APIState.Offline:
                    Child = form = new LoginForm
                    {
                        RequestHide = RequestHide
                    };
                    break;

                case APIState.RequiresSecondFactorAuth:
                    Child = form = new SecondFactorAuthForm();
                    break;

                case APIState.Failing:
                case APIState.Connecting:
                    LinkFlowContainer linkFlow;

                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                        Children = new Drawable[]
                        {
                            new LoadingSpinner
                            {
                                State = { Value = Visibility.Visible },
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            },
                            linkFlow = new LinkFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                TextAnchor = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                            },
                        },
                    };

                    if (!string.IsNullOrEmpty(api.UserFacingOutageMessage.Value))
                    {
                        linkFlow.AddText("Server outage in progress".ToUpperInvariant(), s =>
                        {
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold);
                            s.Colour = Colour4.Orange;
                        });

                        linkFlow.AddParagraph(api.UserFacingOutageMessage.Value, s => s.Font = OsuFont.Style.Caption1);
                    }
                    else if (state.NewValue == APIState.Failing)
                    {
                        linkFlow.AddParagraph(state.NewValue == APIState.Failing ? ToolbarStrings.AttemptingToReconnect : ToolbarStrings.Connecting, s =>
                        {
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold);
                            s.Colour = Colour4.Orange;
                        });
                    }
                    else
                    {
                        linkFlow.AddParagraph(ToolbarStrings.Connecting, s =>
                            s.Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold));
                    }

                    linkFlow.NewParagraph();
                    linkFlow.AddLink(LoginPanelStrings.SignOut, api.Logout, string.Empty, s => s.Font = OsuFont.Style.Caption2);
                    break;

                case APIState.Online:
                    Child = new FillFlowContainer
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
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = LoginPanelStrings.SignedIn,
                                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                            },
                            new UserRankPanel(api.LocalUser.Value)
                            {
                                RelativeSizeAxes = Axes.X,
                                Action = RequestHide
                            },
                            dropdown = new UserDropdown { RelativeSizeAxes = Axes.X },
                            createWebsiteLinks(),
                        },
                    };

                    updateDropdownCurrent(configUserStatus.Value);
                    dropdown.Current.BindValueChanged(action =>
                    {
                        switch (action.NewValue)
                        {
                            case UserAction.Online:
                                configUserStatus.Value = UserStatus.Online;
                                dropdown.StatusColour = colours.Green;
                                break;

                            case UserAction.DoNotDisturb:
                                configUserStatus.Value = UserStatus.DoNotDisturb;
                                dropdown.StatusColour = colours.Red;
                                break;

                            case UserAction.AppearOffline:
                                configUserStatus.Value = UserStatus.Offline;
                                dropdown.StatusColour = colours.Gray7;
                                break;

                            case UserAction.SignOut:
                                api.Logout();
                                break;
                        }
                    }, true);

                    break;
            }

            if (form != null)
                ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(form));
        });

        /// <summary>
        /// scosu: the player's own pages on the website (restricted players get the standing and appeal page like everyone else).
        /// Nothing on servers that aren't scosu servers.
        /// </summary>
        private Drawable createWebsiteLinks()
        {
            if (!api.Endpoints.RanksByStableScore)
                return Empty();

            string website = api.Endpoints.WebsiteUrl;

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(5),
                        Spacing = new Vector2(0, 2),
                        Children = new Drawable[]
                        {
                            new WebsiteLink(FontAwesome.Solid.CalendarAlt, ScosuMenuStrings.WeeklyReports, $@"{website}/weekly-reports"),
                            new WebsiteLink(FontAwesome.Solid.BalanceScale, ScosuMenuStrings.AccountStanding, $@"{website}/appeal"),
                            new WebsiteLink(FontAwesome.Solid.Cog, ScosuMenuStrings.AccountSettings, $@"{website}/settings"),
                        },
                    },
                },
            };
        }

        /// <summary>
        /// scosu: a row of <see cref="createWebsiteLinks"/>, opening a page of the website in the browser.
        /// </summary>
        private partial class WebsiteLink : OsuClickableContainer
        {
            private readonly IconUsage icon;
            private readonly LocalisableString text;
            private readonly string url;

            private Box hoverBox = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [Resolved]
            private OsuGame? game { get; set; }

            public WebsiteLink(IconUsage icon, LocalisableString text, string url)
            {
                this.icon = icon;
                this.text = text;
                this.url = url;

                RelativeSizeAxes = Axes.X;
                Height = 38;
                Masking = true;
                CornerRadius = 6;
                Action = () => game?.OpenUrlExternally(url);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    hoverBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background3,
                        Alpha = 0,
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 12,
                        Size = new Vector2(15),
                        Icon = icon,
                        Colour = colours.PinkLight,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 40,
                        Text = text,
                        Font = OsuFont.Style.Body,
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -12,
                        Size = new Vector2(10),
                        Icon = FontAwesome.Solid.ExternalLinkAlt,
                        Colour = colourProvider.Light4,
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hoverBox.FadeIn(150, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hoverBox.FadeOut(300, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        private void updateDropdownCurrent(UserStatus? status)
        {
            if (dropdown == null)
                return;

            switch (status)
            {
                case UserStatus.Online:
                    dropdown.Current.Value = UserAction.Online;
                    break;

                case UserStatus.DoNotDisturb:
                    dropdown.Current.Value = UserAction.DoNotDisturb;
                    break;

                case UserStatus.Offline:
                    dropdown.Current.Value = UserAction.AppearOffline;
                    break;
            }
        }

        public override bool AcceptsFocus => true;

        protected override bool OnClick(ClickEvent e) => true;

        protected override void OnFocus(FocusEvent e)
        {
            if (form != null) GetContainingFocusManager()!.ChangeFocus(form);
            base.OnFocus(e);
        }
    }
}
