// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Metadata;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Menu.Hub;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the main menu's hub, a sheet of tiles (your score, the event, the daily challenge, friends, the weekly report, news)
    /// and a bar (today's gain, who is online, the song playing) that slides up from the bottom of the screen while the band rises.
    /// The handle above the sheet opens and closes it (by clicking, or scrolling on it).
    /// </summary>
    public partial class MenuHubSheet : VisibilityContainer
    {
        public const float HANDLE_HEIGHT = 30;

        /// <summary>
        /// The share of the screen's height the open sheet takes (the bar included).
        /// </summary>
        public const float SHEET_HEIGHT = 0.52f;

        private const float open_duration = 600;
        private const float close_duration = 450;

        /// <summary>
        /// Below this the tiles stop shrinking and the sheet scrolls instead.
        /// </summary>
        private const float min_grid_height = 300;

        private const float cell_gap = 8;

        /// <summary>
        /// Asks for the sheet to be opened (true) or closed (false), from its handle.
        /// The menu decides, since opening also moves the band and the cards.
        /// </summary>
        public Action<bool>? RequestState;

        /// <summary>
        /// Called when the daily challenge tile is clicked, with the challenge's room.
        /// </summary>
        public Action<Room>? OnDailyChallenge;

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        private readonly Container panel;
        private readonly OsuScrollContainer scroll;
        private readonly Container gridHolder;
        private readonly HubTile[] tiles;
        private readonly HubNewsTile newsTile;
        private readonly HubHandle handle;

        private IDisposable? presenceWatch;
        private bool available;

        public MenuHubSheet()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            var dailyTile = new HubDailyTile { OnDailyChallenge = room => OnDailyChallenge?.Invoke(room) };

            tiles = new HubTile[]
            {
                new HubScoreTile(),
                new HubEventTile(),
                dailyTile,
                new HubFriendsTile(),
                new HubReportTile(),
                newsTile = new HubNewsTile(),
            };

            // Four columns by two rows: the score takes two columns, the event two rows.
            (int column, int row, int columns, int rows)[] cells =
            {
                (0, 0, 2, 1),
                (2, 0, 1, 2),
                (3, 0, 1, 1),
                (0, 1, 1, 1),
                (1, 1, 1, 1),
                (3, 1, 1, 1),
            };

            gridHolder = new Container
            {
                RelativeSizeAxes = Axes.X,
                Padding = new MarginPadding(14 - cell_gap / 2),
            };

            for (int i = 0; i < tiles.Length; i++)
            {
                gridHolder.Add(new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    X = cells[i].column / 4f,
                    Y = cells[i].row / 2f,
                    Width = cells[i].columns / 4f,
                    Height = cells[i].rows / 2f,
                    Padding = new MarginPadding(cell_gap / 2),
                    Child = tiles[i],
                });
            }

            InternalChildren = new Drawable[]
            {
                panel = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Y,
                    Height = SHEET_HEIGHT,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    // Closed until shown, below the screen.
                    Y = 1,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 20, Bottom = HubBar.HEIGHT + 8 },
                            Children = new Drawable[]
                            {
                                new InputBlocker
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = ScosuMenuStyle.CreateGlassBackground(),
                                },
                                scroll = new OsuScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = gridHolder,
                                },
                            },
                        },
                        new HubBar
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                        },
                    },
                },
                handle = new HubHandle
                {
                    RelativePositionAxes = Axes.Y,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    // Shown once the hub is available (see Available).
                    Alpha = 0,
                    Action = () => RequestState?.Invoke(State.Value == Visibility.Hidden),
                    Scrolled = up => RequestState?.Invoke(up),
                },
            };

            handle.Enabled.Value = false;
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

        protected override void Update()
        {
            base.Update();

            // The grid fills the sheet, unless the window is too small for the tiles.
            gridHolder.Height = Math.Max(scroll.DrawHeight, min_grid_height);
        }

        protected override void PopIn()
        {
            panel.MoveToY(0, open_duration, Easing.OutQuint);
            handle.MoveToY(-SHEET_HEIGHT, open_duration, Easing.OutQuint);
            handle.Open = true;

            // Presence is only sent to clients that watch it, so watch it while the bar shows who is online.
            presenceWatch ??= metadataClient?.BeginWatchingUserPresence();

            newsTile.RefreshIfStale();

            // The tiles settle in after the sheet, one after another.
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].FadeOut().MoveToY(20)
                        .Delay(150 + i * 70)
                        .FadeIn(400, Easing.OutQuint).MoveToY(0, 400, Easing.OutQuint);
            }
        }

        protected override void PopOut()
        {
            panel.MoveToY(1, close_duration, Easing.OutQuint);
            handle.MoveToY(0, close_duration, Easing.OutQuint);
            handle.Open = false;

            presenceWatch?.Dispose();
            presenceWatch = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            presenceWatch?.Dispose();
            presenceWatch = null;

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Keeps clicks on the sheet from reaching the menu behind it.
        /// </summary>
        private partial class InputBlocker : Container
        {
            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;
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
                    label.Text = open ? MenuHubStrings.Hide : MenuHubStrings.OnScosuNow;
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
    }
}
