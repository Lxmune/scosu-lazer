// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the main menu's hub, a sheet that slides up from the bottom of the screen while the band rises.
    /// <see cref="MainMenu"/> moves its cards into <see cref="Columns"/> while the sheet is open; below them are shortcuts.
    /// The handle above the sheet opens and closes it (by clicking, or scrolling on it).
    /// </summary>
    public partial class MenuHubSheet : VisibilityContainer
    {
        public const float HANDLE_HEIGHT = 30;

        /// <summary>
        /// The share of the screen's height the open sheet takes.
        /// </summary>
        public const float SHEET_HEIGHT = 0.55f;

        private const float transition_duration = 600;

        /// <summary>
        /// The columns the menu's cards are moved into while the sheet is open (yours, the event, the rest).
        /// </summary>
        public readonly FillFlowContainer[] Columns;

        /// <summary>
        /// Asks for the sheet to be opened (true) or closed (false), from its handle.
        /// The menu decides, since opening also moves the band and the cards.
        /// </summary>
        public Action<bool>? RequestState;

        private readonly Container panel;
        private readonly FillFlowContainer shortcuts;
        private readonly HubHandle handle;

        private bool available;

        public MenuHubSheet()
        {
            RelativeSizeAxes = Axes.Both;
            Height = SHEET_HEIGHT;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            Columns = new FillFlowContainer[3];

            for (int i = 0; i < Columns.Length; i++)
            {
                Columns[i] = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                };
            }

            InternalChild = panel = new Container
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Y,
                Padding = new MarginPadding { Horizontal = 20 },
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        // Only the top corners show: the background reaches past the bottom of the screen.
                        Padding = new MarginPadding { Bottom = -20 },
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 15,
                            CornerExponent = 2.5f,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                                Alpha = 0.6f,
                            },
                        },
                    },
                    new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Vertical = 10 },
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 15),
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Children = Columns,
                                },
                                shortcuts = new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Margin = new MarginPadding { Bottom = 10 },
                                },
                            },
                        },
                    },
                    handle = new HubHandle
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.BottomCentre,
                        // Shown once the hub is available (see Available).
                        Alpha = 0,
                        Action = () => RequestState?.Invoke(State.Value == Visibility.Hidden),
                        Scrolled = up => RequestState?.Invoke(up),
                    },
                },
            };

            handle.Enabled.Value = false;
        }

        [BackgroundDependencyLoader]
        private void load(NewsOverlay? news, RankingsOverlay? rankings, ChatOverlay? chat, ChangelogOverlay? changelog, WikiOverlay? wiki)
        {
            shortcuts.AddRange(new Drawable[]
            {
                new HubShortcut(FontAwesome.Solid.Newspaper, "News", () => news?.Show()),
                new HubShortcut(FontAwesome.Solid.ChartLine, "Rankings", () => rankings?.Show()),
                new HubShortcut(FontAwesome.Solid.Comments, "Chat", () => chat?.Show()),
                new HubShortcut(FontAwesome.Solid.Bullhorn, "Changelog", () => changelog?.Show()),
                new HubShortcut(FontAwesome.Solid.Book, "Wiki", () => wiki?.Show()),
            });
        }

        /// <summary>
        /// Whether the hub can be opened at all: only while the band is out (not on the logo, nor while leaving the menu).
        /// The handle is hidden otherwise.
        /// </summary>
        public bool Available
        {
            get => available;
            set
            {
                if (available == value)
                    return;

                available = value;

                if (!available)
                    Hide();

                handle.FadeTo(available ? 1 : 0, 300, Easing.OutQuint);
                handle.Enabled.Value = available;
            }
        }

        protected override void PopIn()
        {
            panel.MoveToY(0, transition_duration, Easing.OutQuint);
            handle.Open = true;

            // The columns settle in after the sheet, one after another, then the shortcuts.
            for (int i = 0; i < Columns.Length; i++)
            {
                Columns[i].FadeOut().MoveToY(20)
                          .Delay(150 + i * 80)
                          .FadeIn(400, Easing.OutQuint).MoveToY(0, 400, Easing.OutQuint);
            }

            shortcuts.FadeOut().Delay(150 + Columns.Length * 80).FadeIn(400, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            panel.MoveToY(1, transition_duration * 0.8f, Easing.OutQuint);
            handle.Open = false;
        }

        private partial class HubHandle : OsuClickableContainer
        {
            public Action<bool>? Scrolled;

            private SpriteIcon chevron = null!;
            private OsuSpriteText label = null!;

            private bool open;

            public bool Open
            {
                set
                {
                    open = value;

                    if (!IsLoaded)
                        return;

                    chevron.RotateTo(open ? 180 : 0, 400, Easing.OutQuint);
                    label.Text = open ? "Hide" : "On scosu now";
                }
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.X;
                Height = HANDLE_HEIGHT;
                Masking = true;
                CornerRadius = HANDLE_HEIGHT / 2;
                Margin = new MarginPadding { Bottom = 6 };

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.6f,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Padding = new MarginPadding { Horizontal = 16 },
                        Children = new Drawable[]
                        {
                            chevron = new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.Centre,
                                Icon = FontAwesome.Solid.ChevronUp,
                                Size = new Vector2(10),
                                Margin = new MarginPadding { Left = 5 },
                            },
                            label = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                            },
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Open = open;
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                Scrolled?.Invoke(e.ScrollDelta.Y > 0);
                return true;
            }
        }

        private partial class HubShortcut : OsuAnimatedButton
        {
            public HubShortcut(IconUsage icon, LocalisableString text, Action action)
            {
                Size = new Vector2(120, 64);
                Action = action;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White.Opacity(0.06f),
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            new SpriteIcon
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Icon = icon,
                                Size = new Vector2(18),
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = text,
                                Font = OsuFont.GetFont(size: 13, weight: FontWeight.SemiBold),
                            },
                        },
                    },
                };
            }
        }
    }
}
