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
    /// Pieces without data are hidden together with their divider. The hub's sheet slides up from behind it.
    /// </summary>
    public partial class HubBar : VisibilityContainer
    {
        public const float HEIGHT = 58;

        private const float transition_duration = 500;
        private const float now_playing_width = 450;
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

        private Container songHost = null!;
        private Container progressRow = null!;
        private SongView? songView;
        private TrackChangeDirection pendingDirection = TrackChangeDirection.None;
        private bool? shownPlaying;
        private IconButton playPauseButton = null!;
        private ProgressBar progress = null!;
        private OsuSpriteText elapsedText = null!;
        private OsuSpriteText totalText = null!;
        private int shownElapsed = -1;
        private int shownTotal = -1;
        private double? lastSeekTime;

        public HubBar()
        {
            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;

            // Tucked below the screen's edge until shown.
            Y = HEIGHT;
            Alpha = 0;
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
                new Container
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Y,
                    Width = now_playing_width,
                    Padding = new MarginPadding { Right = 14 },
                    Children = new Drawable[]
                    {
                        songHost = new Container { RelativeSizeAxes = Axes.Both },
                        progressRow = new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.X,
                            Height = 14,
                            Y = 19,
                            Padding = new MarginPadding { Left = cover_size + 14, Right = 3 * 32 + 10 },
                            Children = new Drawable[]
                            {
                                elapsedText = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = ScosuMenuStyle.Body(10, FontWeight.SemiBold),
                                    Colour = ScosuMenuStyle.TEXT_DIM,
                                    Text = @"0:00",
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 34 },
                                    Child = progress = new SeekBar
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        OnSeek = onSeek,
                                        OnCommit = onCommit,
                                    },
                                },
                                totalText = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Font = ScosuMenuStyle.Body(10, FontWeight.SemiBold),
                                    Colour = ScosuMenuStyle.TEXT_DIM,
                                    Text = @"0:00",
                                },
                            },
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(2, 0),
                            Children = new Drawable[]
                            {
                                new IconButton
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(30),
                                    Icon = FontAwesome.Solid.StepBackward,
                                    IconScale = new Vector2(0.65f),
                                    Action = () => musicController.PreviousTrack(),
                                },
                                playPauseButton = new IconButton
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(36),
                                    IconScale = new Vector2(0.8f),
                                    Action = () => musicController.TogglePause(),
                                },
                                new IconButton
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(30),
                                    Icon = FontAwesome.Solid.StepForward,
                                    IconScale = new Vector2(0.65f),
                                    Action = () => musicController.NextTrack(),
                                },
                            },
                        },
                    },
                },
            };
        }

        private void onSeek(double position)
        {
            // Dragging seeks now and then rather than on every frame, which the audio thread wouldn't keep up with.
            if (!musicController.IsPlaying || lastSeekTime == null || Time.Current - lastSeekTime > 200)
            {
                musicController.SeekTo(position);
                lastSeekTime = Time.Current;
            }
        }

        private void onCommit(double position)
        {
            musicController.SeekTo(position);
            lastSeekTime = null;
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
            musicController.TrackChanged += onTrackChanged;
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
            bool playing = musicController.IsPlaying;

            if (shownPlaying != playing)
            {
                playPauseButton.Icon = playing ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play;

                // A quick squeeze, so that the swap isn't a flicker.
                if (shownPlaying != null)
                    playPauseButton.ScaleTo(0.7f, 70, Easing.OutQuint).Then().ScaleTo(1, 250, Easing.OutBack);

                shownPlaying = playing;
            }

            layoutItems();
            updateProgress();
        }

        private void updateProgress()
        {
            var track = musicController.CurrentTrack;

            if (!progress.Seeking)
            {
                progress.EndTime = Math.Max(track.Length, 1);
                progress.CurrentTime = track.CurrentTime;
            }

            // Only touched when the second changes.
            int elapsed = (int)(progress.Current.Value / 1000);
            int total = (int)(track.Length / 1000);

            if (elapsed != shownElapsed)
            {
                shownElapsed = elapsed;
                elapsedText.Text = formatTime(elapsed);
            }

            if (total != shownTotal)
            {
                shownTotal = total;
                totalText.Text = formatTime(total);
            }
        }

        private static string formatTime(int seconds) => $@"{Math.Max(seconds, 0) / 60}:{Math.Max(seconds, 0) % 60:00}";

        /// <summary>
        /// Drops the least important pieces while the window is too narrow for all of them (so they never run into the song),
        /// and keeps a divider only between pieces that are shown.
        /// </summary>
        private void layoutItems()
        {
            float available = DrawWidth - side_padding - now_playing_width - item_spacing;

            foreach (var item in items)
                item.Squeezed = false;

            // Least important first.
            var droppable = new[] { next, today, friendsItem, eventItem };

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

            if (s.Next is APIScoreSummary.NextRank n)
            {
                nextLine.Set(default, ScoreSummaryStrings.ScoreToNextRank(ScosuMenuFormatting.CompactScore(n.RankedScoreNeeded), n.Rank.ToLocalisableString(@"N0"), n.Username), default);
                next.Wanted = true;
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

        private void onTrackChanged(WorkingBeatmap _, TrackChangeDirection direction) => Schedule(() => pendingDirection = direction);

        private void displayBeatmap(WorkingBeatmap working)
        {
            var direction = pendingDirection;
            pendingDirection = TrackChangeDirection.None;

            // Only slide when something is on screen to slide away.
            float shift = songView == null || direction == TrackChangeDirection.None ? 0 : direction == TrackChangeDirection.Next ? 40 : -40;

            var old = songView;

            if (old != null)
            {
                // A quick succession of clicks: whatever is still moving settles first.
                old.FinishTransforms();
                old.MoveToX(-shift, 350, Easing.OutQuint).FadeOut(350, Easing.OutQuint).Expire();
            }

            var view = songView = new SongView(working);
            songHost.Add(view);

            if (old != null)
            {
                view.X = shift;
                view.Alpha = 0;
                view.MoveToX(0, 350, Easing.OutQuint).FadeIn(350, Easing.OutQuint);

                // The progress would jump from full to empty, so it is out of sight while it does.
                progressRow.FinishTransforms();
                progressRow.FadeTo(0, 120).Then().FadeIn(300, Easing.OutQuint);
            }

            view.LoadCover(() => beatmap.Value == working);
        }

        /// <summary>
        /// The song's cover, title and artist, one per song so that the old one can slide away while the new one comes in.
        /// </summary>
        private partial class SongView : Container
        {
            private readonly WorkingBeatmap working;
            private readonly Container cover;

            public SongView(WorkingBeatmap working)
            {
                this.working = working;

                RelativeSizeAxes = Axes.Both;

                var metadata = working.Metadata;

                Children = new Drawable[]
                {
                    cover = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(cover_size),
                        Masking = true,
                        CornerRadius = 10,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Colour = ScosuMenuStyle.VIOLET.Opacity(0.32f),
                            Radius = 12,
                        },
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
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Y = -8,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 1),
                        Padding = new MarginPadding { Left = cover_size + 14, Right = 3 * 32 + 10 },
                        Children = new Drawable[]
                        {
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                                Font = ScosuMenuStyle.Body(14, FontWeight.Bold),
                            },
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                                Font = ScosuMenuStyle.Body(12),
                                Colour = ScosuMenuStyle.TEXT_DIM,
                            },
                        },
                    },
                };
            }

            /// <summary>
            /// Loads the cover over the placeholder, which stays until the texture is there.
            /// </summary>
            public void LoadCover(Func<bool> stillWanted)
            {
                LoadComponentAsync(new BeatmapBackgroundSprite(working)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }, sprite =>
                {
                    // Gone, or replaced by a newer song, while it loaded.
                    if (IsDisposed || !stillWanted())
                        return;

                    cover.Add(sprite);
                    sprite.FadeInFromZero(400, Easing.OutQuint);
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (musicController != null)
                musicController.TrackChanged -= onTrackChanged;
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
        /// The song's progress: a thin track that grows under the cursor, and seeks on click and drag.
        /// </summary>
        private partial class SeekBar : ProgressBar
        {
            private const float height = 3;

            public override bool HandleNonPositionalInput => IsHovered;

            public SeekBar()
                : base(true)
            {
                RelativeSizeAxes = Axes.X;
                Height = height;
                FillColour = ScosuMenuStyle.VIOLET;
                BackgroundColour = Color4.White.Opacity(0.14f);
            }

            // The track is too thin to hit, so it takes the clicks around it too.
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
                => new RectangleF(0, -5, DrawWidth, DrawHeight + 10).Contains(ToLocalSpace(screenSpacePos));

            protected override bool OnHover(HoverEvent e)
            {
                this.ResizeHeightTo(height * 1.8f, 200, Easing.OutQuint);
                FillColour = ScosuMenuStyle.VIOLET_INK;
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ResizeHeightTo(height, 200, Easing.OutQuint);
                FillColour = ScosuMenuStyle.VIOLET;
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
            /// Its width when it was last shown, to know whether it would fit again.
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
        }
    }
}
