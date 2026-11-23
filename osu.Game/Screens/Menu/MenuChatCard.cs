// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: #general on the main menu, to read and write in without opening the chat. Shown while the player is in #general
    /// (players start in it; one who left it doesn't get it back here). The button opens the full chat on it.
    /// Typing needs a click in the box first, and Esc or sending a message gives the keyboard back to the menu.
    /// </summary>
    public partial class MenuChatCard : CornerCard
    {
        /// <summary>
        /// The channel shown, as the server names it.
        /// </summary>
        public const string CHANNEL_NAME = @"#general";

        private const float card_height = 220;
        private const float header_height = 28;
        private const float transition_duration = 500;

        [Resolved]
        private ChannelManager? channelManager { get; set; }

        [Resolved]
        private ChatOverlay? chatOverlay { get; set; }

        private readonly IBindableList<Channel> joinedChannels = new BindableList<Channel>();
        private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

        private MenuChatDisplay chat = null!;
        private Container content = null!;
        private Box background = null!;
        private OsuSpriteText headerText = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Margined = content = new Container
            {
                Width = CARD_WIDTH,
                Height = card_height,
                Alpha = 0,
                Masking = true,
                CornerRadius = ScosuMenuStyle.CORNER_RADIUS,
                CornerExponent = 2.5f,
                BorderThickness = 1.5f,
                BorderColour = ScosuMenuStyle.OUTLINE,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ScosuMenuStyle.GLASS,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = header_height,
                        Padding = new MarginPadding { Left = 12, Right = 4 },
                        Children = new Drawable[]
                        {
                            headerText = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = CHANNEL_NAME.ToUpperInvariant(),
                                Font = ScosuMenuStyle.Eyebrow,
                                Spacing = new Vector2(1.5f, 0),
                                Colour = ScosuMenuStyle.VIOLET,
                            },
                            new IconButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Icon = FontAwesome.Solid.Comments,
                                IconScale = new Vector2(0.8f),
                                TooltipText = Localisation.ChatStrings.HeaderDescription,
                                Action = openChat,
                            },
                        },
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = header_height },
                        Child = chat = new MenuChatDisplay
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentHue.BindValueChanged(_ =>
            {
                background.Colour = ScosuMenuStyle.GLASS;
                headerText.Colour = ScosuMenuStyle.VIOLET;
            }, true);

            if (channelManager == null)
                return;

            joinedChannels.BindTo(channelManager.JoinedChannels);
            joinedChannels.BindCollectionChanged(joinedChannelsChanged, true);
        }

        private void joinedChannelsChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            var general = joinedChannels.FirstOrDefault(c => c.Name == CHANNEL_NAME && c.Type == ChannelType.Public);

            if (chat.Channel.Value != general)
                chat.Channel.Value = general;

            if (general == null)
                content.FadeOut(transition_duration, Easing.OutQuint);
            else
                content.FadeIn(transition_duration, Easing.OutQuint);
        }

        private void openChat()
        {
            if (channelManager != null && chat.Channel.Value != null)
                channelManager.CurrentChannel.Value = chat.Channel.Value;

            chatOverlay?.Show();
        }

        protected override void PopIn() => this.FadeIn(transition_duration, Easing.OutQuint);

        protected override void PopOut() => this.FadeOut(transition_duration, Easing.OutQuint);

        private partial class MenuChatDisplay : StandAloneChatDisplay
        {
            public MenuChatDisplay()
                : base(postingTextBox: true)
            {
                // The card has its own background and corners.
                Background.Alpha = 0;
                CornerRadius = 0;

                // Unlike in a match, the menu's keys stay the menu's until the box is clicked, and come back after sending.
                TextBox!.HoldFocus = false;
                TextBox.ReleaseFocusOnCommit = true;
            }
        }
    }
}
