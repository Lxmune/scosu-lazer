// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Menu.Hub;
using osuTK;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the main menu's hub, its second screen: "On scosu now" and a grid of tiles (your score, the event, the daily challenge,
    /// friends, the weekly report, news, then your profile's plays, accuracy, country rank, medals, best and recent plays, and
    /// today's daily tasks).
    /// The menu scrolls down to it (the band, the logo and the corner cards leave at the top while it comes in from below),
    /// and back up (see MainMenu.setHub()).
    /// </summary>
    public partial class MenuHubPage : VisibilityContainer
    {
        /// <summary>
        /// How long the menu takes to scroll from one screen to the other.
        /// </summary>
        public const float SCROLL_DURATION = 400;

        /// <summary>
        /// Below this the tiles stop shrinking and the page scrolls instead.
        /// </summary>
        private const float min_grid_height = 520;

        private const float cell_gap = 12;

        private const int columns = 6;
        private const int rows = 4;

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
        private readonly HubEventTile eventTile;
        private readonly IBindable<IReadOnlyList<APIWeeklyReport>?> reports = new Bindable<IReadOnlyList<APIWeeklyReport>?>();
        private readonly Dictionary<HubTile, Container> cells = new Dictionary<HubTile, Container>();
        private readonly (HubTile tile, int column, int row, int columns, int rows)[] withEvent;
        private readonly (HubTile tile, int column, int row, int columns, int rows)[] withEventNoReport;
        private readonly (HubTile tile, int column, int row, int columns, int rows)[] withoutEvent;
        private readonly (HubTile tile, int column, int row, int columns, int rows)[] withoutEventNoReport;
        private readonly IBindable<APIScosuEvent?> currentEvent = new Bindable<APIScosuEvent?>();

        [Resolved]
        private ScosuEventProvider? eventProvider { get; set; }

        [Resolved]
        private WeeklyReportProvider? reportProvider { get; set; }
        private readonly HubNewsTile newsTile;
        private readonly HubTasksTile tasksTile;
        private readonly HubProfileProvider profile;

        public MenuHubPage()
        {
            RelativeSizeAxes = Axes.Both;

            profile = new HubProfileProvider();

            dailyTile = new HubDailyTile { OnDailyChallenge = room => OnDailyChallenge?.Invoke(room) };

            eventTile = new HubEventTile();

            var score = new HubScoreTile(profile);
            var friends = new HubFriendsTile();
            var report = new HubReportTile();
            newsTile = new HubNewsTile();
            var plays = new HubPlaysTile(profile);
            var accuracy = new HubAccuracyTile(profile);
            var country = new HubCountryTile(profile);
            var medals = new HubMedalsTile(profile);
            var best = new HubBestTile(profile);
            var recent = new HubRecentTile(profile);
            tasksTile = new HubTasksTile();

            // Six columns by four rows, all on the page unless the window is too small for them.
            // With an event, its tile takes a 2×2 block in the middle of the first two rows.
            withEvent = new (HubTile, int, int, int, int)[]
            {
                (score, 0, 0, 3, 1),
                (eventTile, 3, 0, 2, 2),
                (dailyTile, 5, 0, 1, 1),
                (friends, 0, 1, 1, 1),
                (report, 1, 1, 2, 1),
                (newsTile, 5, 1, 1, 1),
                (plays, 0, 2, 2, 1),
                (accuracy, 2, 2, 2, 1),
                (country, 4, 2, 1, 1),
                (medals, 5, 2, 1, 1),
                (best, 0, 3, 2, 1),
                (recent, 2, 3, 2, 1),
                (tasksTile, 4, 3, 2, 1),
            };

            // Without one, its tile is gone and the others close up: the plays get wider, and the recent plays most of the last row.
            withoutEvent = new (HubTile, int, int, int, int)[]
            {
                (score, 0, 0, 3, 1),
                (dailyTile, 3, 0, 1, 1),
                (newsTile, 4, 0, 2, 1),
                (friends, 0, 1, 1, 1),
                (report, 1, 1, 2, 1),
                (plays, 3, 1, 3, 1),
                (accuracy, 0, 2, 2, 1),
                (country, 2, 2, 1, 1),
                (medals, 3, 2, 1, 1),
                (best, 4, 2, 2, 1),
                (recent, 0, 3, 4, 1),
                (tasksTile, 4, 3, 2, 1),
            };

            // Without a weekly report, its tile is left out too (an empty tile only takes room), and its neighbours widen.
            withEventNoReport = new (HubTile, int, int, int, int)[]
            {
                (score, 0, 0, 3, 1),
                (eventTile, 3, 0, 2, 2),
                (dailyTile, 5, 0, 1, 1),
                (friends, 0, 1, 1, 1),
                (newsTile, 1, 1, 2, 1),
                (medals, 5, 1, 1, 1),
                (plays, 0, 2, 2, 1),
                (accuracy, 2, 2, 2, 1),
                (country, 4, 2, 2, 1),
                (best, 0, 3, 2, 1),
                (recent, 2, 3, 2, 1),
                (tasksTile, 4, 3, 2, 1),
            };

            withoutEventNoReport = new (HubTile, int, int, int, int)[]
            {
                (score, 0, 0, 3, 1),
                (dailyTile, 3, 0, 1, 1),
                (newsTile, 4, 0, 2, 1),
                (friends, 0, 1, 2, 1),
                (plays, 2, 1, 4, 1),
                (accuracy, 0, 2, 2, 1),
                (country, 2, 2, 1, 1),
                (medals, 3, 2, 1, 1),
                (best, 4, 2, 2, 1),
                (recent, 0, 3, 4, 1),
                (tasksTile, 4, 3, 2, 1),
            };

            tiles = withEvent.Select(l => l.tile).ToArray();

            gridHolder = new Container
            {
                RelativeSizeAxes = Axes.X,
                Padding = new MarginPadding(-cell_gap / 2),
            };

            foreach (var tile in tiles)
            {
                var cell = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,
                    Padding = new MarginPadding(cell_gap / 2),
                    Child = tile,
                };

                cells[tile] = cell;
                gridHolder.Add(cell);
            }

            // Laid out without the event and the report until they are known.
            applyLayout(false);

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
                    Padding = new MarginPadding { Top = HubDock.HEIGHT + 16, Bottom = HubBar.HEIGHT + 16, Horizontal = 24 },
                    Children = new Drawable[]
                    {
                        new InputBlocker { RelativeSizeAxes = Axes.Both },
                        // No heading: the dock above already says where this is.
                        scroll = new HubScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = gridHolder,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (eventProvider != null)
                currentEvent.BindTo(eventProvider.Event);

            // Asked of the providers, not the tiles: a tile left out isn't updated, so it couldn't say it has something again.
            if (reportProvider != null)
                reports.BindTo(reportProvider.Reports);

            currentEvent.BindValueChanged(_ => applyLayout(true));
            reports.BindValueChanged(_ => applyLayout(true));
            applyLayout(false);
        }

        /// <summary>
        /// Places the tiles for whether there is an event and a weekly report: they glide to their new places when either
        /// changes while the hub is open.
        /// </summary>
        private void applyLayout(bool animate)
        {
            bool hasEvent = currentEvent.Value != null;
            // The report tile shows the latest report when there is no new one, so any report will do.
            bool hasReport = reports.Value?.Count > 0;

            var layout = hasEvent
                ? hasReport ? withEvent : withEventNoReport
                : hasReport ? withoutEvent : withoutEventNoReport;
            double duration = animate && State.Value == Visibility.Visible ? 500 : 0;

            foreach (var (tile, cell) in cells)
            {
                int index = Array.FindIndex(layout, l => l.tile == tile);

                if (index < 0)
                {
                    // Out of the layout: hidden, so it takes no input either.
                    cell.FadeOut(duration / 2, Easing.OutQuint);
                    continue;
                }

                var l = layout[index];

                cell.FadeIn(duration, Easing.OutQuint);
                cell.MoveTo(new Vector2((float)l.column / columns, (float)l.row / rows), duration, Easing.OutQuint);
                cell.ResizeTo(new Vector2((float)l.columns / columns, (float)l.rows / rows), duration, Easing.OutQuint);
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

            scroll.ScrollToStart(false);
            newsTile.RefreshIfStale();
            tasksTile.RefreshIfStale();
            profile.RefreshIfStale();

            // The tiles settle in with the page, a touch after one another (all within ~0.4s).
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].FadeOut().MoveToY(12)
                        .Delay(Math.Min(i, 5) * 25)
                        .FadeIn(250, Easing.OutQuint).MoveToY(0, 300, Easing.OutQuint);
            }
        }

        protected override void PopOut()
        {
            // Hidden at the end, so that nothing of the panel shows through the bar once it is a screen below.
            panel.MoveToY(1, SCROLL_DURATION, Easing.OutQuint).Then().FadeOut();
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
    }
}
