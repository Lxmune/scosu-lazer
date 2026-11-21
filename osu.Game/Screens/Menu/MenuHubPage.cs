// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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
    /// scosu: the main menu's hub, its second screen: "On scosu now" and a grid of tiles (your score, the event, the daily challenge,
    /// friends, the weekly report, news, then your profile's plays, accuracy, country rank, medals, best and recent plays).
    /// The menu scrolls down to it (the band, the logo and the corner cards leave at the top while it comes in from below),
    /// and back up. The hint at the bottom of the first screen opens it (by clicking, or scrolling on it).
    /// </summary>
    public partial class MenuHubPage : VisibilityContainer
    {
        /// <summary>
        /// The height of the "On scosu now" hint at the bottom of the first screen.
        /// </summary>
        public const float HANDLE_HEIGHT = 48;

        /// <summary>
        /// How long the menu takes to scroll from one screen to the other.
        /// </summary>
        public const float SCROLL_DURATION = 700;

        /// <summary>
        /// Below this the tiles stop shrinking and the page scrolls instead.
        /// </summary>
        private const float min_grid_height = 520;

        private const float cell_gap = 8;

        private const float heading_height = 46;

        private const int columns = 6;
        private const int rows = 4;

        /// <summary>
        /// Asks for the hub to be opened (true) or closed (false), from its hint.
        /// The menu decides, since opening also scrolls the band and the cards away.
        /// </summary>
        public Action<bool>? RequestState;

        /// <summary>
        /// Called when the daily challenge tile is clicked, with the challenge's room.
        /// </summary>
        public Action<Room>? OnDailyChallenge;

        /// <summary>
        /// Today's daily challenge room, once the daily challenge tile has it.
        /// </summary>
        public Room? DailyChallengeRoom => dailyTile.Room;

        private readonly Container panel;
        private readonly HubScrollContainer scroll;
        private readonly Container gridHolder;
        private readonly HubTile[] tiles;
        private readonly HubDailyTile dailyTile;
        private readonly HubNewsTile newsTile;
        private readonly HubProfileProvider profile;
        private readonly HubHandle handle;

        private bool available;

        public MenuHubPage()
        {
            RelativeSizeAxes = Axes.Both;

            profile = new HubProfileProvider();

            dailyTile = new HubDailyTile { OnDailyChallenge = room => OnDailyChallenge?.Invoke(room) };

            // Six columns by four rows, all on the page unless the window is too small for them.
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
                Padding = new MarginPadding(-cell_gap / 2),
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
                    // Closed: a screen below, out of sight (and hidden, so that nothing of it shows through the bar).
                    Y = 1,
                    Alpha = 0,
                    // Under the dock along the top, above the bar along the bottom.
                    Padding = new MarginPadding { Top = HubDock.HEIGHT + 14, Bottom = HubBar.HEIGHT + 14, Horizontal = 24 },
                    Children = new Drawable[]
                    {
                        new InputBlocker { RelativeSizeAxes = Axes.Both },
                        new OsuSpriteText
                        {
                            Text = MenuHubStrings.OnScosuNow,
                            Font = ScosuMenuStyle.Display(24),
                            Shadow = true,
                            ShadowColour = Color4.Black.Opacity(0.6f),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = heading_height },
                            Child = scroll = new HubScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = gridHolder,
                            },
                        },
                    },
                },
                handle = new HubHandle
                {
                    RelativePositionAxes = Axes.Y,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Margin = new MarginPadding { Bottom = HubBar.HEIGHT + 2 },
                    // Shown once the hub is available (see Available).
                    Alpha = 0,
                    Action = () => RequestState?.Invoke(true),
                    Scrolled = up => RequestState?.Invoke(!up),
                },
            };

            handle.Enabled.Value = false;
        }

        /// <summary>
        /// Whether the hub can be opened at all: only while the band is out (not on the logo, nor while leaving the menu).
        /// The hint is hidden otherwise.
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

                updateHandle();
            }
        }

        protected override void Update()
        {
            base.Update();

            // The grid fills the page, unless the window is too small for the tiles.
            gridHolder.Height = Math.Max(scroll.DrawHeight, min_grid_height);
        }

        protected override void PopIn()
        {
            panel.FadeIn();
            panel.MoveToY(0, SCROLL_DURATION, Easing.OutQuint);
            handle.MoveToY(-1, SCROLL_DURATION, Easing.OutQuint);
            updateHandle();

            scroll.ScrollToStart(false);
            newsTile.RefreshIfStale();
            profile.RefreshIfStale();

            // The tiles settle in as the page arrives, one after another.
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].FadeOut().MoveToY(30)
                        .Delay(120 + Math.Min(i, 9) * 40)
                        .FadeIn(450, Easing.OutQuint).MoveToY(0, 500, Easing.OutQuint);
            }
        }

        protected override void PopOut()
        {
            // Hidden at the end, so that nothing of the panel shows through the bar once it is a screen below.
            panel.MoveToY(1, SCROLL_DURATION, Easing.OutQuint).Then().FadeOut();
            handle.MoveToY(0, SCROLL_DURATION, Easing.OutQuint);
            updateHandle();
        }

        private void updateHandle()
        {
            bool shown = available && State.Value == Visibility.Hidden;

            handle.FadeTo(shown ? 1 : 0, shown ? 400 : 200, Easing.OutQuint);
            handle.Enabled.Value = shown;
        }

        /// <summary>
        /// Only takes the scroll it can use: the menu behind the page scrolls back up to the band on scrolling up,
        /// and down to the hub on scrolling down.
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
        /// Keeps clicks on the page from reaching the menu behind it.
        /// </summary>
        private partial class InputBlocker : Container
        {
            // Scrolling is left to the menu behind the page (it scrolls between the band and the hub), see HubScrollContainer.
            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;
        }

        /// <summary>
        /// "On scosu now" with a chevron under it, at the bottom of the first screen just above the bar:
        /// says there is more below, and leaves with the first screen when the hub opens.
        /// </summary>
        private partial class HubHandle : OsuClickableContainer
        {
            public Action<bool>? Scrolled;

            private SpriteIcon chevron = null!;

            public HubHandle()
            {
                // Before load: OsuClickableContainer sets its content up for auto-sizing there.
                AutoSizeAxes = Axes.X;
                Height = HANDLE_HEIGHT;
            }

            [BackgroundDependencyLoader]
            private void load()
            {

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
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = MenuHubStrings.OnScosuNow,
                                Font = OsuFont.GetFont(size: 19, weight: FontWeight.SemiBold),
                                Shadow = true,
                                ShadowColour = Color4.Black.Opacity(0.85f),
                                ShadowOffset = new Vector2(0, 0.06f),
                            },
                            chevron = new SpriteIcon
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.Centre,
                                Icon = FontAwesome.Solid.ChevronDown,
                                Size = new Vector2(14),
                                Margin = new MarginPadding { Top = 7 },
                                Colour = ScosuMenuStyle.VIOLET_INK,
                            },
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // The chevron nudges downward now and then, to say there is more below.
                Scheduler.AddDelayed(() => chevron.MoveToY(5, 220, Easing.OutQuad).Then().MoveToY(0, 520, Easing.OutBounce), 4000, true);
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                Scrolled?.Invoke(e.ScrollDelta.Y > 0);
                return true;
            }
        }
    }
}
