// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays.Music;
using osu.Game.Screens.Menu;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    public partial class NowPlayingOverlay : OsuFocusedOverlayContainer, INamedOverlayComponent
    {
        public const double TRACK_DRAG_SEEK_DEBOUNCE = 40;

        public IconUsage Icon => OsuIcon.Music;
        public LocalisableString Title => NowPlayingStrings.HeaderTitle;
        public LocalisableString Description => NowPlayingStrings.HeaderDescription;

        private const float player_width = 400;
        private const float player_height = 150;
        private const float transition_length = 500;
        private const float progress_height = 12;
        private const float bottom_black_area_height = 74;
        private const float margin = 10;

        private Drawable background = null!;
        private ProgressBar progressBar = null!;
        private OsuSpriteText elapsedText = null!, totalText = null!;
        private int lastElapsedSecond = -1, lastTotalSecond = -1;

        private IconButton prevButton = null!;
        private IconButton playButton = null!;
        private IconButton nextButton = null!;
        private MusicIconButton shuffleButton = null!;
        private IconButton playlistButton = null!;

        private MarqueeContainer title = null!, artist = null!;

        private PlaylistOverlay? playlist;

        private Container dragContainer = null!;
        private Container playerContainer = null!;
        private Container playlistContainer = null!;

        protected override double PopInOutSampleBalance => OsuGameBase.SFX_STEREO_STRENGTH * 0.75f;

        [Resolved]
        private MusicController musicController { get; set; } = null!;

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private Bindable<bool> allowTrackControl = null!;
        private readonly BindableBool shuffle = new BindableBool(true);
        private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

        private static readonly Color4 inactive_colour = new Color4(1f, 1f, 1f, 0.65f);

        private static string formatTime(double ms)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            return t.TotalHours >= 1 ? $@"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $@"{t.Minutes}:{t.Seconds:00}";
        }

        private static readonly FontUsage title_font = OsuFont.GetFont(size: 22, weight: FontWeight.SemiBold);
        private static readonly FontUsage artist_font = OsuFont.GetFont(size: 14, weight: FontWeight.Medium);

        public NowPlayingOverlay()
        {
            Width = player_width;
            Margin = new MarginPadding(margin);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                dragContainer = new DragContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    Children = new Drawable[]
                    {
                        playerContainer = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = player_height,
                            Masking = true,
                            CornerRadius = 5,
                            EdgeEffect = new EdgeEffectParameters
                            {
                                Type = EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(40),
                                Radius = 5,
                            },
                            Children = new[]
                            {
                                background = Empty(),
                                title = new MarqueeContainer
                                {
                                    Origin = Anchor.BottomCentre,
                                    Anchor = Anchor.TopCentre,
                                    Position = new Vector2(0, 42),
                                    Colour = Color4.White,
                                    CreateContent = () => new OsuSpriteText
                                    {
                                        Font = title_font,
                                        Text = @"Nothing to play",
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    NonOverflowingContentAnchor = Anchor.Centre,
                                    Padding = new MarginPadding { Horizontal = 15 },
                                },
                                artist = new MarqueeContainer
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Position = new Vector2(0, 46),
                                    Colour = Color4.White,
                                    CreateContent = () => new OsuSpriteText
                                    {
                                        Font = artist_font,
                                        Text = @"Nothing to play",
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    NonOverflowingContentAnchor = Anchor.Centre,
                                    Padding = new MarginPadding { Horizontal = 15 },
                                },
                                new Container
                                {
                                    Padding = new MarginPadding { Bottom = progress_height },
                                    Height = bottom_black_area_height,
                                    RelativeSizeAxes = Axes.X,
                                    Origin = Anchor.BottomCentre,
                                    Anchor = Anchor.BottomCentre,
                                    Children = new Drawable[]
                                    {
                                        new FillFlowContainer<IconButton>
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(5),
                                            Origin = Anchor.Centre,
                                            Anchor = Anchor.Centre,
                                            Children = new[]
                                            {
                                                prevButton = new MusicIconButton
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Action = () => musicController.PreviousTrack(),
                                                    Icon = FontAwesome.Solid.StepBackward,
                                                },
                                                playButton = new MusicIconButton
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Scale = new Vector2(1.4f),
                                                    IconScale = new Vector2(1.4f),
                                                    Action = () => musicController.TogglePause(),
                                                    Icon = FontAwesome.Regular.PlayCircle,
                                                },
                                                nextButton = new MusicIconButton
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Action = () => musicController.NextTrack(),
                                                    Icon = FontAwesome.Solid.StepForward,
                                                },
                                            }
                                        },
                                        elapsedText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Margin = new MarginPadding { Left = 15, Bottom = 3 },
                                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold, fixedWidth: true),
                                            Colour = colours.PinkLighter,
                                            Text = formatTime(0),
                                        },
                                        totalText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.BottomRight,
                                            Origin = Anchor.BottomRight,
                                            Margin = new MarginPadding { Right = 15, Bottom = 3 },
                                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold, fixedWidth: true),
                                            Colour = colours.PinkLight,
                                            Text = formatTime(0),
                                        },
                                        shuffleButton = new MusicIconButton
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.Centre,
                                            Position = new Vector2(37, -6),
                                            Action = shuffle.Toggle,
                                            Icon = FontAwesome.Solid.Random,
                                        },
                                        playlistButton = new MusicIconButton
                                        {
                                            Origin = Anchor.Centre,
                                            Anchor = Anchor.CentreRight,
                                            Position = new Vector2(-37, -6),
                                            Icon = FontAwesome.Solid.Bars,
                                            Colour = inactive_colour,
                                            Action = togglePlaylist
                                        },
                                    }
                                },
                                progressBar = new HoverableProgressBar
                                {
                                    Origin = Anchor.BottomCentre,
                                    Anchor = Anchor.BottomCentre,
                                    Height = progress_height / 3,
                                    FillColour = colours.PinkLight,
                                    BackgroundColour = colours.PinkDarker.Opacity(0.6f),
                                    OnSeek = onSeek,
                                    OnCommit = onCommit,
                                }
                            },
                        },
                        playlistContainer = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Y = player_height + margin,
                        }
                    }
                },
            };
        }

        private double? lastSeekGameTime;
        private double? lastSeekAudioTargetTime;

        private void onSeek(double progress)
        {
            if (!musicController.IsPlaying || lastSeekGameTime == null || Time.Current - lastSeekGameTime > TRACK_DRAG_SEEK_DEBOUNCE)
            {
                musicController.SeekTo(progress);
                lastSeekGameTime = Time.Current;
                lastSeekAudioTargetTime = progress;
            }
        }

        private void onCommit(double progress)
        {
            // Avoid a second seek to the same location, which could occur when using keyboard navigation from `OnSeek` and subsequent `OnCommit` calls.
            if (progress != lastSeekAudioTargetTime)
                musicController.SeekTo(progress);

            lastSeekGameTime = null;
            lastSeekAudioTargetTime = null;
        }

        private void togglePlaylist()
        {
            if (playlist == null)
            {
                LoadComponentAsync(playlist = new PlaylistOverlay
                {
                    RelativeSizeAxes = Axes.Both,
                }, _ =>
                {
                    playlistContainer.Add(playlist);

                    playlist.State.BindValueChanged(s => playlistButton.FadeColour(s.NewValue == Visibility.Visible ? colours.PinkLight : inactive_colour, 200, Easing.OutQuint), true);

                    togglePlaylist();
                });

                return;
            }

            if (!beatmap.Disabled)
                playlist.ToggleVisibility();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.BindDisabledChanged(_ => Scheduler.AddOnce(updateEnabledStates));

            allowTrackControl = musicController.AllowTrackControl.GetBoundCopy();
            allowTrackControl.BindValueChanged(_ => Scheduler.AddOnce(updateEnabledStates), true);

            shuffle.BindTo(musicController.Shuffle);
            shuffle.BindValueChanged(s => shuffleButton.FadeColour(s.NewValue ? colours.PinkLight : inactive_colour, 200, Easing.OutQuint), true);

            accentHue.BindValueChanged(_ =>
            {
                elapsedText.Colour = colours.PinkLighter;
                totalText.Colour = colours.PinkLight;
                progressBar.FillColour = progressBar.IsHovered ? colours.Violet : colours.PinkLight;
                progressBar.BackgroundColour = colours.PinkDarker.Opacity(0.6f);
                shuffleButton.Colour = shuffle.Value ? colours.PinkLight : inactive_colour;
                if (playlist != null)
                    playlistButton.Colour = playlist.State.Value == Visibility.Visible ? colours.PinkLight : inactive_colour;
            }, true);

            musicController.TrackChanged += trackChanged;
            trackChanged(beatmap.Value);
        }

        protected override void PopIn()
        {
            this.FadeIn(transition_length, Easing.OutQuint);
            dragContainer.ScaleTo(1, transition_length, Easing.OutElasticHalf);
        }

        protected override void PopOut()
        {
            base.PopOut();

            this.FadeOut(transition_length, Easing.OutQuint);
            dragContainer.ScaleTo(0.9f, transition_length, Easing.OutQuint);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            playlistContainer.Height = MathF.Min(Parent!.DrawHeight - margin * 3 - player_height, PlaylistOverlay.PLAYLIST_HEIGHT);

            float height = player_height;

            if (playlist != null)
            {
                height += playlist.DrawHeight;
                if (playlist.State.Value == Visibility.Visible)
                    height += margin;
            }

            Height = dragContainer.Height = height;
        }

        protected override void Update()
        {
            base.Update();

            if (pendingBeatmapSwitch != null)
            {
                pendingBeatmapSwitch();
                pendingBeatmapSwitch = null;
            }

            var track = musicController.CurrentTrack;
            updateTimeLabels();

            if (!progressBar.Seeking)
            {
                if (!track.IsDummyDevice)
                {
                    progressBar.EndTime = track.Length;
                    progressBar.CurrentTime = track.CurrentTime;

                    playButton.Icon = track.IsRunning ? FontAwesome.Regular.PauseCircle : FontAwesome.Regular.PlayCircle;
                }
                else
                {
                    progressBar.CurrentTime = 0;
                    progressBar.EndTime = 1;
                    playButton.Icon = FontAwesome.Regular.PlayCircle;
                }
            }
        }

        private void updateTimeLabels()
        {
            double current = progressBar.Current.Value;
            double total = ((BindableNumber<double>)progressBar.Current).MaxValue;
            if (musicController.CurrentTrack.IsDummyDevice) total = 0;

            int cs = (int)(current / 1000);
            int ts = (int)(total / 1000);

            if (cs != lastElapsedSecond)
            {
                lastElapsedSecond = cs;
                elapsedText.Text = formatTime(current);
            }

            if (ts != lastTotalSecond)
            {
                lastTotalSecond = ts;
                totalText.Text = formatTime(total);
            }
        }

        private Action? pendingBeatmapSwitch;

        private CancellationTokenSource? backgroundLoadCancellation;

        private WorkingBeatmap? currentBeatmap;

        private void trackChanged(WorkingBeatmap beatmap, TrackChangeDirection direction = TrackChangeDirection.None)
        {
            currentBeatmap = beatmap;

            // avoid using scheduler as our scheduler may not be run for a long time, holding references to beatmaps.
            pendingBeatmapSwitch = delegate
            {
                BeatmapMetadata metadata = beatmap.Metadata;

                title.CreateContent = () => new OsuSpriteText
                {
                    Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                    Font = title_font,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
                artist.CreateContent = () => new OsuSpriteText
                {
                    Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                    Font = artist_font,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };

                backgroundLoadCancellation?.Cancel();

                LoadComponentAsync(new Background(beatmap) { Depth = float.MaxValue }, newBackground =>
                {
                    if (beatmap != currentBeatmap)
                    {
                        newBackground.Dispose();
                        return;
                    }

                    switch (direction)
                    {
                        case TrackChangeDirection.Next:
                            newBackground.Position = new Vector2(player_width, 0);
                            newBackground.MoveToX(0, 500, Easing.OutCubic);
                            background.MoveToX(-player_width, 500, Easing.OutCubic);
                            break;

                        case TrackChangeDirection.Prev:
                            newBackground.Position = new Vector2(-player_width, 0);
                            newBackground.MoveToX(0, 500, Easing.OutCubic);
                            background.MoveToX(player_width, 500, Easing.OutCubic);
                            break;
                    }

                    background.Expire();
                    background = newBackground;

                    playerContainer.Add(newBackground);
                }, (backgroundLoadCancellation = new CancellationTokenSource()).Token);
            };
        }

        private void updateEnabledStates()
        {
            bool beatmapDisabled = beatmap.Disabled;
            bool trackControlDisabled = !musicController.AllowTrackControl.Value;

            if (beatmapDisabled || trackControlDisabled)
                playlist?.Hide();

            prevButton.Enabled.Value = !beatmapDisabled && !trackControlDisabled;
            nextButton.Enabled.Value = !beatmapDisabled && !trackControlDisabled;
            playlistButton.Enabled.Value = !beatmapDisabled && !trackControlDisabled;
            playButton.Enabled.Value = !trackControlDisabled;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (musicController.IsNotNull())
                musicController.TrackChanged -= trackChanged;
        }

        private partial class MusicIconButton : IconButton
        {
            private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

            public MusicIconButton()
            {
                AutoSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                accentHue.BindValueChanged(_ =>
                {
                    HoverColour = colours.Pink.Opacity(0.55f);
                    FlashColour = colours.PinkLighter;
                }, true);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // works with AutoSizeAxes above to make buttons autosize with the scale animation.
                Content.AutoSizeAxes = Axes.None;
                Content.Size = new Vector2(DEFAULT_BUTTON_SIZE);
            }
        }

        private partial class Background : BufferedContainer
        {
            private readonly Sprite sprite;
            private readonly WorkingBeatmap beatmap;
            private readonly Box tint;

            public Background(WorkingBeatmap beatmap)
                : base(cachedFrameBuffer: true)
            {
                this.beatmap = beatmap;

                Depth = float.MaxValue;
                RelativeSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    sprite = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(170),
                        FillMode = FillMode.Fill,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    tint = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.5f)
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textures, OsuColour colours)
            {
                tint.Colour = ColourInfo.GradientVertical(colours.GreyVioletDarker.Opacity(0.15f), colours.GreyVioletDarker.Opacity(0.9f));
                sprite.Texture = beatmap.GetBackground() ?? textures.Get(@"Backgrounds/bg2");
            }
        }

        private partial class DragContainer : Container
        {
            protected override bool OnDragStart(DragStartEvent e)
            {
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                Vector2 change = e.MousePosition - e.MouseDownPosition;

                // Diminish the drag distance as we go further to simulate "rubber band" feeling.
                change *= change.Length <= 0 ? 0 : MathF.Pow(change.Length, 0.7f) / change.Length;

                this.MoveTo(change);
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                this.MoveTo(Vector2.Zero, 800, Easing.OutElastic);
                base.OnDragEnd(e);
            }
        }

        private partial class HoverableProgressBar : ProgressBar
        {
            public override bool HandleNonPositionalInput => IsHovered;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            public HoverableProgressBar()
                : base(true)
            {
            }

            protected override bool OnHover(HoverEvent e)
            {
                FillColour = colours.Violet;
                this.ResizeHeightTo(progress_height / 1.5f, 300, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                FillColour = colours.PinkLight;
                this.ResizeHeightTo(progress_height / 3, 300, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
