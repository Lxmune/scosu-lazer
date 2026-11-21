// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
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
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Overlays;
using osu.Game.Overlays.ScosuEvent;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The hub's bar along the bottom of the sheet: today's gain, the event, who is online, and the song playing with its controls
    /// (the now playing card in the corner is hidden while the hub is open).
    /// </summary>
    public partial class HubBar : CompositeDrawable
    {
        public const float HEIGHT = 46;

        private const float now_playing_width = 360;
        private const float cover_size = 32;

        [Resolved]
        private ScoreSummaryProvider? scoreProvider { get; set; }

        [Resolved]
        private ScosuEventProvider? eventProvider { get; set; }

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private MusicController musicController { get; set; } = null!;

        private readonly IBindable<APIScoreSummary?> summary = new Bindable<APIScoreSummary?>();
        private readonly IBindable<APIScosuEvent?> currentEvent = new Bindable<APIScosuEvent?>();
        private readonly IBindableDictionary<int, UserPresence> presences = new BindableDictionary<int, UserPresence>();

        private Item today = null!;
        private Item next = null!;
        private Item eventItem = null!;
        private Item online = null!;

        private Container cover = null!;
        private OsuSpriteText title = null!;
        private OsuSpriteText artist = null!;
        private IconButton playPauseButton = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ScosuMenuStyle.BAND,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 24, Right = now_playing_width + 36 },
                    // Whatever doesn't fit on a narrow window is cut off rather than overlapping the song.
                    Masking = true,
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(26, 0),
                        Children = new Drawable[]
                        {
                            today = new Item(null),
                            next = new Item(FontAwesome.Solid.ArrowUp) { Colour = ScosuMenuStyle.VIOLET },
                            eventItem = new Item(null) { Colour = ScosuMenuStyle.ICE },
                            online = new Item(null, true),
                        },
                    },
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    AutoSizeAxes = Axes.Y,
                    Width = now_playing_width,
                    Padding = new MarginPadding { Right = 12 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10, 0),
                    Children = new Drawable[]
                    {
                        cover = new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Size = new Vector2(cover_size),
                            Masking = true,
                            CornerRadius = 7,
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
                            Width = now_playing_width - 12 - cover_size - 3 * 30 - 3 * 10,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                title = new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold),
                                },
                                artist = new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Font = ScosuMenuStyle.Body(11),
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

            today.Alpha = next.Alpha = eventItem.Alpha = online.Alpha = 0;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (scoreProvider != null)
                summary.BindTo(scoreProvider.Summary);

            if (eventProvider != null)
                currentEvent.BindTo(eventProvider.Event);

            if (metadataClient != null)
                presences.BindTo(metadataClient.UserPresences);

            summary.BindValueChanged(_ => updateSummary(), true);
            currentEvent.BindValueChanged(_ => updateEvent(), true);
            presences.BindCollectionChanged((_, _) => updateOnline(), true);
            beatmap.BindValueChanged(b => Scheduler.AddOnce(() => displayBeatmap(b.NewValue)), true);

            Scheduler.AddDelayed(updateEvent, 1000, true);
        }

        protected override void Update()
        {
            base.Update();

            // The track can be paused from anywhere (the toolbar, media keys), so the icon follows it.
            playPauseButton.Icon = musicController.IsPlaying ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play;
        }

        private void updateSummary()
        {
            if (summary.Value is not APIScoreSummary s)
            {
                today.Alpha = next.Alpha = 0;
                return;
            }

            var line = LocalisableString.Interpolate($@"{ScoreSummaryStrings.Today} +{s.Today.RankedScore.ToLocalisableString(@"N0")} · {ScoreSummaryStrings.PlayCount(s.Today.PlayCount.ToLocalisableString(@"N0"))}");

            if (s.Rank is int rank)
                line = LocalisableString.Interpolate($@"{line} · #{rank.ToLocalisableString(@"N0")}");

            today.SetText(line);
            today.Alpha = 1;

            if (s.Next is APIScoreSummary.NextRank n)
            {
                next.SetText(ScoreSummaryStrings.ScoreToNextRank(ScosuMenuFormatting.CompactScore(n.RankedScoreNeeded), n.Rank.ToLocalisableString(@"N0"), n.Username));
                next.Alpha = 1;
            }
            else
                next.Alpha = 0;
        }

        private void updateEvent()
        {
            if (currentEvent.Value is not APIScosuEvent ev || EventDisplay.CountdownTarget(ev) is not DateTimeOffset target)
            {
                eventItem.Alpha = 0;
                return;
            }

            string left = EventDisplay.ShortCountdown(target - DateTimeOffset.Now);

            eventItem.SetText(ev.Status == ScosuEventStatus.Upcoming ? MenuHubStrings.EventStartsIn(ev.Title, left) : MenuHubStrings.EventEndsIn(ev.Title, left));
            eventItem.Alpha = 1;
        }

        private void updateOnline()
        {
            if (presences.Count == 0)
            {
                online.Alpha = 0;
                return;
            }

            online.SetText(MenuHubStrings.Online(presences.Count.ToString(@"N0")));
            online.Alpha = 1;
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

        /// <summary>
        /// One piece of the bar: a line of text, optionally behind an icon or a green dot. Hidden (and so skipped by the layout) at zero alpha.
        /// </summary>
        private partial class Item : FillFlowContainer
        {
            private readonly OsuSpriteText text;

            public Item(IconUsage? icon, bool dot = false)
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
                        Size = new Vector2(10),
                    });
                }

                if (dot)
                {
                    Add(new Circle
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(7),
                        Colour = Color4Extensions.FromHex(@"7ee2a0"),
                    });
                }

                Add(text = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold),
                });
            }

            public void SetText(LocalisableString value) => text.Text = value;
        }
    }
}
