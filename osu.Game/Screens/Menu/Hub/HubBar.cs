// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Utils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Overlays;
using osu.Game.Overlays.Dashboard;
using osu.Game.Overlays.ScosuEvent;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The menu's bar along the bottom of the screen, shown whenever the band is out: today's gain, the event, who is online,
    /// your friends, and the song playing with its controls (the now playing card in the corner is hidden while the hub is open).
    /// Pieces without data are hidden together with their divider. The hub scrolls in from behind it.
    /// </summary>
    public partial class HubBar : VisibilityContainer
    {
        public const float HEIGHT = 58;

        private const float transition_duration = 500;
        private const float now_playing_width = 380;
        private const float cover_size = 40;
        private const float item_spacing = 30;
        private const float side_padding = 24;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private ScoreSummaryProvider? scoreProvider { get; set; }

        [Resolved]
        private ScosuEventProvider? eventProvider { get; set; }

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        [Resolved]
        private DashboardOverlay? dashboard { get; set; }

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private MusicController musicController { get; set; } = null!;

        private readonly IBindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();
        private readonly IBindable<APIScosuEvent?> currentEvent = new Bindable<APIScosuEvent?>();
        private readonly IBindableDictionary<int, UserPresence> presences = new BindableDictionary<int, UserPresence>();
        private readonly IBindableDictionary<int, UserPresence> friendPresences = new BindableDictionary<int, UserPresence>();
        private readonly IBindableList<APIRelation> friends = new BindableList<APIRelation>();

        private Item[] items = Array.Empty<Item>();
        private Item today = null!;
        private Item next = null!;
        private Item eventItem = null!;
        private Item online = null!;
        private Item friendsItem = null!;

        private Line todayLine = null!;
        private Line nextLine = null!;
        private Line eventLine = null!;
        private Line friendsLine = null!;

        private OnlinePlayersDisplay? onlineDisplay;
        private FillFlowContainer itemFlow = null!;

        private Container cover = null!;
        private TruncatingSpriteText title = null!;
        private TruncatingSpriteText artist = null!;
        private IconButton playPauseButton = null!;

        public HubBar()
        {
            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;

            // Tucked below the screen's edge until shown.
            Y = HEIGHT;
            Alpha = 0;

            // Keeps updating while hidden (the countdown, the measured widths and which pieces fit), so that it slides in
            // already laid out instead of settling its pieces during the first frames.
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (metadataClient != null)
            {
                // Its list opens upward from the bar, since the display is anchored to the bottom.
                onlineDisplay = new OnlinePlayersDisplay
                {
                    // Just the dot and the count, like the bar's other text.
                    Flat = true,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Y = -6,
                };
            }

            today = new Item(todayLine = new Line(FontAwesome.Solid.Trophy));
            next = new Item(nextLine = new Line(FontAwesome.Solid.ArrowUp) { Colour = ScosuMenuStyle.VIOLET });
            eventItem = new Item(eventLine = new Line(FontAwesome.Solid.CalendarAlt) { Colour = ScosuMenuStyle.ICE });
            online = new Item(new Container
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.X,
                Height = HEIGHT,
                Child = (Drawable?)onlineDisplay ?? Empty(),
            });
            friendsItem = new Item(new BarButton(openFriends, friendsLine = new Line(FontAwesome.Solid.UserFriends)));

            items = new[] { today, next, eventItem, online, friendsItem };

            InternalChildren = new Drawable[]
            {
                // A soft shadow upward, so that the bar reads over a bright background.
                new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 24,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.BottomLeft,
                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0), Color4.Black.Opacity(0.28f)),
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex(@"120f19").Opacity(0.96f),
                },
                new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 1,
                    Colour = Color4.White.Opacity(0.08f),
                },
                itemFlow = new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(item_spacing, 0),
                    Padding = new MarginPadding { Left = side_padding },
                    Children = items,
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    AutoSizeAxes = Axes.Y,
                    Width = now_playing_width,
                    Padding = new MarginPadding { Right = 16 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(12, 0),
                    Children = new Drawable[]
                    {
                        cover = new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Size = new Vector2(cover_size),
                            Masking = true,
                            CornerRadius = 8,
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ScosuMenuStyle.VIOLET.Opacity(0.4f),
                            },
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Width = now_playing_width - 16 - cover_size - 3 * 30 - 4 * 12,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                title = new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Font = ScosuMenuStyle.Body(14, FontWeight.Bold),
                                },
                                artist = new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Font = ScosuMenuStyle.Body(12),
                                    Colour = ScosuMenuStyle.TEXT_DIM,
                                },
                            },
                        },
                        new IconButton
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Icon = FontAwesome.Solid.StepBackward,
                            IconScale = new Vector2(0.6f),
                            Action = () => musicController.PreviousTrack(),
                        },
                        playPauseButton = new IconButton
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            IconScale = new Vector2(0.7f),
                            Action = () => musicController.TogglePause(),
                        },
                        new IconButton
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Icon = FontAwesome.Solid.StepForward,
                            IconScale = new Vector2(0.6f),
                            Action = () => musicController.NextTrack(),
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (scoreProvider != null)
                summary.BindTo(scoreProvider.Summary);

            if (eventProvider != null)
                currentEvent.BindTo(eventProvider.Event);

            if (metadataClient != null)
            {
                presences.BindTo(metadataClient.UserPresences);
                friendPresences.BindTo(metadataClient.FriendPresences);
            }

            friends.BindTo(api.LocalUserState.Friends);

            summary.BindValueChanged(_ => updateSummary(), true);
            currentEvent.BindValueChanged(_ => updateEvent(), true);
            presences.BindCollectionChanged((_, _) => updateOnline(), true);
            friendPresences.BindCollectionChanged((_, _) => updateFriends());
            friends.BindCollectionChanged((_, _) => updateFriends(), true);
            beatmap.BindValueChanged(b => Scheduler.AddOnce(() => displayBeatmap(b.NewValue)), true);

            // The online list only watches presence while it is shown, so it follows the bar.
            State.BindValueChanged(state =>
            {
                if (onlineDisplay == null)
                    return;

                if (state.NewValue == Visibility.Visible)
                    onlineDisplay.Show();
                else
                    onlineDisplay.Hide();
            }, true);

            Scheduler.AddDelayed(updateEvent, 1000, true);
        }

        protected override void Update()
        {
            base.Update();

            // The track can be paused from anywhere (the toolbar, media keys), so the icon follows it.
            playPauseButton.Icon = musicController.IsPlaying ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play;

            layoutItems();
        }

        /// <summary>
        /// Drops the least important pieces while the window is too narrow for all of them (so they never run into the song),
        /// and keeps a divider only between pieces that are shown.
        /// </summary>
        private void layoutItems()
        {
            float available = DrawWidth - side_padding - now_playing_width - item_spacing;

            foreach (var item in items)
                item.Squeezed = false;

            // Least important first. The rank to pass outlasts the friends count: it is what the bar is mostly for.
            var droppable = new[] { friendsItem, next, today, eventItem };

            float total() => items.Where(i => i.Wanted && !i.Squeezed).Sum(i => i.NaturalWidth + item_spacing);

            foreach (var item in droppable)
            {
                if (total() <= available)
                    break;

                item.Squeezed = true;
            }

            bool first = true;

            foreach (var item in items)
            {
                item.Alpha = item.Present ? 1 : 0;
                item.ShowDivider = item.Present && !first;
                first &= !item.Present;

                if (item.Present)
                    item.Measure();
            }
        }

        private void updateSummary()
        {
            if (summary.Value is not APIScoreSummary s)
            {
                today.Wanted = next.Wanted = false;
                return;
            }

            var tail = LocalisableString.Interpolate($@"· {ScoreSummaryStrings.PlayCount(s.Today.PlayCount.ToLocalisableString(@"N0"))}");

            if (s.Rank is int rank)
                tail = LocalisableString.Interpolate($@"{tail} · #{rank.ToLocalisableString(@"N0")}");

            todayLine.Set(ScoreSummaryStrings.Today, $@"+{s.Today.RankedScore:N0}", tail);
            today.Wanted = true;
            today.ContentChanged();

            if (s.Next is APIScoreSummary.NextRank n)
            {
                nextLine.Set(default, ScoreSummaryStrings.ScoreToNextRank(ScosuMenuFormatting.CompactScore(n.RankedScoreNeeded), n.Rank.ToLocalisableString(@"N0"), n.Username), default);
                next.Wanted = true;
                next.ContentChanged();
            }
            else
                next.Wanted = false;
        }

        private void updateEvent()
        {
            if (currentEvent.Value is not APIScosuEvent ev || EventDisplay.CountdownTarget(ev) is not DateTimeOffset target)
            {
                eventItem.Wanted = false;
                return;
            }

            string left = EventDisplay.ShortCountdown(target - DateTimeOffset.Now);

            eventLine.Set(default, ev.Title, ev.Status == ScosuEventStatus.Upcoming ? MenuHubStrings.StartsIn(left) : MenuHubStrings.EndsInLower(left));
            eventItem.Wanted = true;
        }

        private void updateOnline()
        {
            // The list itself hides when nobody else is on; this only decides whether its divider is shown.
            online.Wanted = onlineDisplay != null && presences.Keys.Any(id => id != api.LocalUser.Value.OnlineID);
        }

        private void updateFriends()
        {
            if (!api.IsLoggedIn || friends.Count == 0)
            {
                friendsItem.Wanted = false;
                return;
            }

            int onlineCount = friends.Count(f => friendPresences.ContainsKey(f.TargetID));

            friendsLine.Set(MenuHubStrings.Friends, MenuHubStrings.FriendsOnline(onlineCount.ToString(@"N0"), friends.Count.ToString(@"N0")), default);
            friendsItem.Wanted = true;
        }

        private void openFriends()
        {
            if (dashboard == null)
                return;

            dashboard.Header.Current.Value = DashboardOverlayTabs.Friends;
            dashboard.Show();
        }

        private void displayBeatmap(WorkingBeatmap working)
        {
            var metadata = working.Metadata;

            title.Text = new RomanisableString(metadata.TitleUnicode, metadata.Title);
            artist.Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist);

            LoadComponentAsync(new BeatmapBackgroundSprite(working)
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fill,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            }, sprite =>
            {
                // A newer song may have been picked while this one loaded.
                if (beatmap.Value != working)
                    return;

                cover.Add(sprite);
                sprite.FadeInFromZero(400, Easing.OutQuint);

                // Keep the placeholder underneath; drop older covers.
                while (cover.Count > 2)
                    cover.Remove(cover[1], true);
            });
        }

        protected override void PopIn() => this.MoveToY(0, transition_duration, Easing.OutQuint).FadeIn(transition_duration, Easing.OutQuint);

        protected override void PopOut() => this.MoveToY(HEIGHT, transition_duration, Easing.OutQuint).FadeOut(transition_duration, Easing.OutQuint);

        // Hover and clicks stop at the bar; the scroll is left to the menu, which opens and closes the hub with it.
        protected override bool OnHover(HoverEvent e) => true;

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        /// <summary>
        /// A line of text: an optional icon, a dim label, the bright value and a dim tail.
        /// </summary>
        private partial class Line : FillFlowContainer
        {
            private readonly OsuSpriteText label;
            private readonly OsuSpriteText value;
            private readonly OsuSpriteText tail;

            public Line(IconUsage? icon)
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(6, 0);

                if (icon != null)
                {
                    Add(new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Icon = icon.Value,
                        Size = new Vector2(13),
                        Margin = new MarginPadding { Right = 2 },
                    });
                }

                AddRange(new Drawable[]
                {
                    label = text(FontWeight.Regular, ScosuMenuStyle.TEXT_DIM),
                    value = text(FontWeight.Bold, Color4.White),
                    tail = text(FontWeight.Regular, ScosuMenuStyle.TEXT_DIM),
                });
            }

            private static OsuSpriteText text(FontWeight weight, Color4 colour) => new OsuSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Font = ScosuMenuStyle.Body(14, weight),
                Colour = colour,
            };

            public void Set(LocalisableString labelText, LocalisableString valueText, LocalisableString tailText)
            {
                label.Text = labelText;
                value.Text = valueText;
                tail.Text = tailText;

                // Empty pieces take no space (nor spacing).
                label.Alpha = string.IsNullOrEmpty(labelText.ToString()) ? 0 : 1;
                tail.Alpha = string.IsNullOrEmpty(tailText.ToString()) ? 0 : 1;
            }
        }

        /// <summary>
        /// A clickable piece of the bar, lit up under the cursor.
        /// </summary>
        private partial class BarButton : OsuClickableContainer
        {
            private readonly Box highlight;

            public BarButton(Action action, Drawable content)
            {
                Action = action;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 8;
                Padding = new MarginPadding { Horizontal = 10, Vertical = 7 };

                // The padding is the highlight's margin around the text; the item's spacing stays visually the same.
                Margin = new MarginPadding { Horizontal = -10 };

                Child = content;

                // On top of the text, but faint enough to read through.
                AddInternal(highlight = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0,
                });
            }

            protected override bool OnHover(HoverEvent e)
            {
                highlight.FadeTo(0.09f, 150, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                highlight.FadeOut(200, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        /// <summary>
        /// One piece of the bar, behind a thin divider (unless it is the first one shown).
        /// </summary>
        private partial class Item : FillFlowContainer
        {
            private readonly Drawable divider;
            private readonly Drawable content;

            /// <summary>
            /// Whether there is anything to show.
            /// </summary>
            public bool Wanted;

            /// <summary>
            /// Whether the bar is too narrow for it right now.
            /// </summary>
            public bool Squeezed;

            public bool Present => Wanted && !Squeezed;

            /// <summary>
            /// Its width when it was last shown, to know whether it would fit again (0 once its content changed: unknown).
            /// </summary>
            public float NaturalWidth { get; private set; }

            public bool ShowDivider
            {
                set => divider.Alpha = value ? 1 : 0;
            }

            public Item(Drawable content)
            {
                this.content = content;

                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(item_spacing, 0);
                Alpha = 0;

                InternalChildren = new[]
                {
                    divider = new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Width = 1,
                        Height = HEIGHT * 0.6f,
                        Colour = osuTK.Graphics.Color4.White.Opacity(0.15f),
                        Alpha = 0,
                    },
                    content,
                };
            }

            public void Measure() => NaturalWidth = content.DrawWidth + 1 + item_spacing;

            /// <summary>
            /// Its content changed while it may be hidden: it is tried again (shown for a frame, measured, and squeezed out
            /// again only if it still doesn't fit), rather than kept out by the width of what it showed before.
            /// </summary>
            public void ContentChanged() => NaturalWidth = 0;
        }
    }
}
