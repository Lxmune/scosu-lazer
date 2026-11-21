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
        /// The share of the screen's height the open sheet takes (not the bar, which MainMenu puts under the sheet).
        /// </summary>
        public const float SHEET_HEIGHT = 0.46f;

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

        private readonly Container panel;
        private readonly HubScrollContainer scroll;
        private readonly Container gridHolder;
        private readonly HubTile[] tiles;
        private readonly HubNewsTile newsTile;
        private readonly HubScoreTile scoreTile;
        private readonly HubHandle handle;

        private bool available;

        public MenuHubSheet()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            var dailyTile = new HubDailyTile { OnDailyChallenge = room => OnDailyChallenge?.Invoke(room) };

            tiles = new HubTile[]
            {
                scoreTile = new HubScoreTile(),
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
                    // Closed until shown: below the sheet's bottom, and invisible, since the bar in front of it is see-through.
                    Y = 1,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 20, Bottom = 8 },
                            Children = new Drawable[]
                            {
                                new InputBlocker
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = ScosuMenuStyle.CreateGlassBackground(),
                                },
                                scroll = new HubScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = gridHolder,
                                },
                            },
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
            panel.FadeIn(100);
            panel.MoveToY(0, open_duration, Easing.OutQuint);
            handle.MoveToY(-SHEET_HEIGHT, open_duration, Easing.OutQuint);
            handle.Open = true;

            newsTile.RefreshIfStale();
            scoreTile.RefreshIfStale();

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
            // Hidden at the end, so that nothing of the panel shows through the bar once it is tucked behind it.
            panel.MoveToY(1, close_duration, Easing.OutQuint).Then().FadeOut();
            handle.MoveToY(0, close_duration, Easing.OutQuint);
            handle.Open = false;
        }

        /// <summary>
        /// Only takes the scroll it can use: the menu behind the sheet closes the hub on scrolling up, and opens it on scrolling down.
        /// </summary>
        private partial class HubScrollContainer : OsuScrollContainer
        {
            protected override bool OnScroll(ScrollEvent e)
            {
                if (ScrollContent.DrawHeight <= DisplayableContent + 0.5f)
                    return false;

                bool up = e.ScrollDelta.Y > 0;

                if (up ? Current <= 0 : IsScrolledToEnd())
                    return false;

                return base.OnScroll(e);
            }
        }

        /// <summary>
        /// Keeps clicks on the sheet from reaching the menu behind it.
        /// </summary>
        private partial class InputBlocker : Container
        {
            // Scrolling is left to the menu behind the sheet (it opens and closes the hub), see HubScrollContainer.
            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;
        }

        /// <summary>
        /// "On scosu now" with a chevron under it, on the menu's background just above the bar: no box around it.
        /// </summary>
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
                Margin = new MarginPadding { Bottom = 4 };

                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    RelativeSizeAxes = Axes.None,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Padding = new MarginPadding { Horizontal = 28 },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        label = new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                            Shadow = true,
                        },
                        chevron = new SpriteIcon
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.Centre,
                            Icon = FontAwesome.Solid.ChevronUp,
                            Size = new Vector2(10),
                            Margin = new MarginPadding { Top = 5 },
                            Colour = ScosuMenuStyle.TEXT_DIM,
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
