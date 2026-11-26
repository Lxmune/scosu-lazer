// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens.OnlinePlay.Match.Components;
using osu.Game.Users;

namespace osu.Game.Overlays.Dashboard.CurrentlyOnline
{
    public partial class OnlineUserListPanel : OnlineUserPanel
    {
        public OnlineUserListPanel(APIUser user)
            : base(user)
        {
            RelativeSizeAxes = Axes.X;
            Height = 40;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new DelayedLoadUnloadWrapper(() =>
            {
                var content = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new UserListPanel(User),
                            new PurpleRoundedButton
                            {
                                Width = 100,
                                Text = ContextMenuStrings.SpectatePlayer,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Action = BeginSpectating,
                                Enabled = { BindTarget = CanSpectate },
                            },
                            CanSendMessage
                                ? new PurpleRoundedButton
                                {
                                    Width = 120,
                                    Margin = new MarginPadding { Left = 2 },
                                    Text = UsersStrings.CardSendMessage,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Action = SendMessage,
                                }
                                : Empty(),
                        }
                    }
                };

                content.OnLoadComplete += _ => content.FadeInFromZero(800, Easing.OutQuint);

                return content;
            }, 40, 5000)
            {
                RelativeSizeAxes = Axes.Both,
            };
        }
    }
}
