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
using osuTK;

namespace osu.Game.Overlays.Dashboard.CurrentlyOnline
{
    internal partial class OnlineUserGridPanel : OnlineUserPanel
    {
        public OnlineUserGridPanel(APIUser user)
            : base(user)
        {
            Size = new Vector2(290, 162);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new DelayedLoadUnloadWrapper(() =>
            {
                var content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(2),
                    Children = new Drawable[]
                    {
                        new UserGridPanel(User)
                        {
                            RelativeSizeAxes = Axes.X,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            ColumnDimensions = CanSendMessage
                                ? new[] { new Dimension(), new Dimension(GridSizeMode.Absolute, 2), new Dimension() }
                                : new[] { new Dimension() },
                            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                            Content = new[]
                            {
                                CanSendMessage
                                    ? new Drawable[] { createSpectateButton(), Empty(), createMessageButton() }
                                    : new Drawable[] { createSpectateButton() }
                            }
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

        private PurpleRoundedButton createSpectateButton() => new PurpleRoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Text = ContextMenuStrings.SpectatePlayer,
            Action = BeginSpectating,
            Enabled = { BindTarget = CanSpectate },
        };

        private PurpleRoundedButton createMessageButton() => new PurpleRoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Text = UsersStrings.CardSendMessage,
            Action = SendMessage,
        };
    }
}
