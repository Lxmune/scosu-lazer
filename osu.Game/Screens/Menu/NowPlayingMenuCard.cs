// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the main menu's top-right card, the song playing with its cover and controls.
    /// It stays on screen, unlike the <see cref="SongTicker"/> it replaces, and makes way for the hub.
    /// </summary>
    public partial class NowPlayingMenuCard : CornerCard
    {
        private const float transition_duration = 500;
        private const float cover_size = 44;

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private MusicController musicController { get; set; } = null!;

        private Container content = null!;
        private Container cover = null!;
        private OsuSpriteText title = null!;
        private OsuSpriteText artist = null!;
        private IconButton playPauseButton = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Margined = content = new Container
            {
                Width = CARD_WIDTH,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    ScosuMenuStyle.CreateGlassBackground(),
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(ScosuMenuStyle.CARD_PADDING - 4),
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                cover = new Container
                                {
                                    Size = new Vector2(cover_size),
                                    Masking = true,
                                    CornerRadius = 6,
                                    Child = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = ScosuMenuStyle.VIOLET.Opacity(0.35f),
                                    },
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Direction = FillDirection.Vertical,
                                    Padding = new MarginPadding { Horizontal = 10 },
                                    Children = new Drawable[]
                                    {
                                        title = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Font = ScosuMenuStyle.Body(15, FontWeight.SemiBold),
                                        },
                                        artist = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Font = ScosuMenuStyle.Body(12),
                                            Colour = ScosuMenuStyle.TEXT_DIM,
                                        },
                                    },
                                },
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Direction = FillDirection.Horizontal,
                                    Children = new Drawable[]
                                    {
                                        new IconButton
                                        {
                                            Icon = FontAwesome.Solid.StepBackward,
                                            IconScale = new Vector2(0.6f),
                                            Action = () => musicController.PreviousTrack(),
                                        },
                                        playPauseButton = new IconButton
                                        {
                                            IconScale = new Vector2(0.7f),
                                            Action = () => musicController.TogglePause(),
                                        },
                                        new IconButton
                                        {
                                            Icon = FontAwesome.Solid.StepForward,
                                            IconScale = new Vector2(0.6f),
                                            Action = () => musicController.NextTrack(),
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.BindValueChanged(b => Scheduler.AddOnce(() => display(b.NewValue)), true);
        }

        protected override void Update()
        {
            base.Update();

            // The track can be paused from anywhere (the toolbar, media keys), so the icon follows it.
            playPauseButton.Icon = musicController.IsPlaying ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play;
        }

        private void display(WorkingBeatmap working)
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
                sprite.FadeInFromZero(transition_duration, Easing.OutQuint);

                // Keep the placeholder underneath; drop older covers.
                while (cover.Count > 2)
                    cover.Remove(cover[1], true);
            });
        }

        protected override void PopIn() => content.MoveToY(0, transition_duration, Easing.OutQuint).FadeIn(transition_duration, Easing.OutQuint);

        protected override void PopOut() => content.MoveToY(-30, transition_duration, Easing.OutQuint).FadeOut(transition_duration, Easing.OutQuint);
    }
}
