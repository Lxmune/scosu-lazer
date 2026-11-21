// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Effects;
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
        public const float HANDLE_HEIGHT = 48;

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

        private const int columns = 6;
        private const int rows = 4;

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
        private readonly HubProfileProvider profile;
        private readonly HubHandle handle;

        private bool available;

        public MenuHubSheet()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            profile = new HubProfileProvider();

            var dailyTile = new HubDailyTile { OnDailyChallenge = room => OnDailyChallenge?.Invoke(room) };

            // Six columns by four rows; the sheet shows two rows and scrolls to the other two.
            (HubTile tile, int column, int row, int columns, int rows)[] layout =
            {
                (new HubScoreTile(profile), 0, 0, 3, 1),
                (new HubEventTile(), 3, 0, 2, 2),
                (dailyTile, 5, 0, 1, 1),
                (new HubFriendsTile(), 0, 1, 1, 1),
                (new HubReportTile(), 1, 1, 2, 1),
                (newsTile = new HubNewsTile(), 5, 1, 1, 1),
                (new HubPlaysTile(profile), 0, 2, 2, 1),
                (new HubAccuracyTile(profile), 2, 2, 2, 1),
                (new HubCountryTile(profile), 4, 2, 1, 1),
                (new HubMedalsTile(profile), 5, 2, 1, 1),
                (new HubBestTile(profile), 0, 3, 3, 1),
                (new HubRecentTile(profile), 3, 3, 3, 1),
            };

            tiles = layout.Select(l => l.tile).ToArray();

            gridHolder = new Container
            {
                RelativeSizeAxes = Axes.X,
                Padding = new MarginPadding(14 - cell_gap / 2),
            };

            foreach (var l in layout)
            {
                gridHolder.Add(new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    X = (float)l.column / columns,
                    Y = (float)l.row / rows,
                    Width = (float)l.columns / columns,
                    Height = (float)l.rows / rows,
                    Padding = new MarginPadding(cell_gap / 2),
                    Child = l.tile,
                });
            }

            InternalChildren = new Drawable[]
            {
                profile,
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
            // Two rows fit in the sheet, the other two are scrolled to.
            gridHolder.Height = Math.Max(scroll.DrawHeight, min_grid_height) * rows / 2;
        }

        protected override void PopIn()
        {
            panel.FadeIn(100);
            panel.MoveToY(0, open_duration, Easing.OutQuint);
            handle.MoveToY(-SHEET_HEIGHT, open_duration, Easing.OutQuint);
            handle.Open = true;

            newsTile.RefreshIfStale();
            profile.RefreshIfStale();

            // The tiles settle in after the sheet, one after another.
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].FadeOut().MoveToY(20)
                        .Delay(150 + Math.Min(i, 7) * 70)
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
                Margin = new MarginPadding { Bottom = 2 };

                // A dark glow behind the text, so that it reads on a bright background.
                Masking = true;
                CornerRadius = HANDLE_HEIGHT / 2;
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.35f),
                    Radius = 18,
                };

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.22f,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 1),
                        // A wide hit area around the text.
                        Padding = new MarginPadding { Horizontal = 48 },
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            label = new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Font = OsuFont.GetFont(size: 19, weight: FontWeight.SemiBold),
                                Shadow = true,
                                ShadowColour = Color4.Black.Opacity(0.85f),
                                ShadowOffset = new Vector2(0, 0.06f),
                            },
                            chevron = new SpriteIcon
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.Centre,
                                Icon = FontAwesome.Solid.ChevronUp,
                                Size = new Vector2(14),
                                Margin = new MarginPadding { Top = 7 },
                                Colour = ScosuMenuStyle.VIOLET_INK,
                            },
                        },
                    },
                };
            }

            private void bob()
            {
                // The chevron nudges upward now and then while closed, to say the hub can be pulled up.
                if (open)
                    return;

                chevron.MoveToY(-5, 220, Easing.OutQuad).Then().MoveToY(0, 520, Easing.OutBounce);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Open = open;

                Scheduler.AddDelayed(bob, 4000, true);
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                Scrolled?.Invoke(e.ScrollDelta.Y > 0);
                return true;
            }
        }
    }
}
