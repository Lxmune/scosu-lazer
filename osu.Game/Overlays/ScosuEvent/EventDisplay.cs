// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osuTK.Graphics;

namespace osu.Game.Overlays.ScosuEvent
{
    /// <summary>
    /// What the main menu's event card and the event overlay draw alike.
    /// </summary>
    public static class EventDisplay
    {
        public static Color4 Accent(APIScosuEvent ev) => parse(ev.Appearance.Accent) ?? Color4Extensions.FromHex(@"7856ce");

        /// <summary>
        /// A colour lifted towards white, so small text in it stays readable on the dark cards.
        /// </summary>
        public static Color4 Lifted(Color4 colour, float amount = 0.4f)
            => new Color4(colour.R + (1 - colour.R) * amount, colour.G + (1 - colour.G) * amount, colour.B + (1 - colour.B) * amount, colour.A);

        public static Color4 RewardColour(APIScosuEvent ev, APIScosuEvent.Reward reward) => (reward.Colour != null ? parse(reward.Colour) : null) ?? Accent(ev);

        private static Color4? parse(string hex)
        {
            try
            {
                return Color4Extensions.FromHex(hex);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// When the countdown runs out: the start while upcoming, the end while live, none once ended.
        /// </summary>
        public static DateTimeOffset? CountdownTarget(APIScosuEvent ev) => ev.Status switch
        {
            ScosuEventStatus.Upcoming => ev.StartsAt,
            ScosuEventStatus.Live => ev.EndsAt,
            _ => null,
        };

        public static LocalisableString CountdownLabel(APIScosuEvent ev) => ev.Status switch
        {
            ScosuEventStatus.Upcoming => ScosuEventStrings.StartsIn,
            ScosuEventStatus.Live => ScosuEventStrings.EndsIn,
            _ => ScosuEventStrings.Ended,
        };

        public static LocalisableString StatusText(ScosuEventStatus status) => status switch
        {
            ScosuEventStatus.Upcoming => ScosuEventStrings.Upcoming,
            ScosuEventStatus.Live => ScosuEventStrings.Live,
            _ => ScosuEventStrings.Ended,
        };

        /// <summary>
        /// "3d 14h 22m", or "14m 07s" in the last hour.
        /// </summary>
        public static string ShortCountdown(TimeSpan left)
        {
            if (left < TimeSpan.Zero)
                left = TimeSpan.Zero;

            if (left.TotalDays >= 1)
                return $@"{(int)left.TotalDays}d {left.Hours}h {left.Minutes:00}m";
            if (left.TotalHours >= 1)
                return $@"{left.Hours}h {left.Minutes:00}m";

            return $@"{left.Minutes}m {left.Seconds:00}s";
        }

        /// <summary>
        /// "15 Jul", in the user's time zone.
        /// </summary>
        public static string Day(DateTimeOffset date) => date.ToLocalTime().ToString(@"d MMM", CultureInfo.CurrentCulture);

        /// <summary>
        /// "15 Jul, 18:00", in the user's time zone.
        /// </summary>
        public static string DayAndTime(DateTimeOffset date) => date.ToLocalTime().ToString(@"d MMM, HH:mm", CultureInfo.CurrentCulture);

        /// <summary>
        /// The small pill telling whether the event is upcoming, live or over.
        /// </summary>
        public static Drawable StatusPill(APIScosuEvent ev) => new CircularContainer
        {
            AutoSizeAxes = Axes.Both,
            Masking = true,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ev.Status == ScosuEventStatus.Live ? Accent(ev) : Color4.White.Opacity(0.15f),
                },
                new OsuSpriteText
                {
                    Text = StatusText(ev.Status).ToUpper(),
                    Font = OsuFont.GetFont(size: 10, weight: FontWeight.Bold),
                    Margin = new MarginPadding { Horizontal = 8, Vertical = 2 },
                },
            },
        };

        /// <summary>
        /// A bar filled up to <paramref name="fraction"/> in the accent.
        /// </summary>
        public static Drawable ProgressBar(Color4 accent, double fraction, float height = 5) => new CircularContainer
        {
            RelativeSizeAxes = Axes.X,
            Height = height,
            Masking = true,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White.Opacity(0.12f),
                },
                new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = (float)Math.Clamp(fraction, 0, 1),
                    Masking = true,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = accent,
                    },
                },
            },
        };

        /// <summary>
        /// The event's picture (a banner or the menu card's image), loaded in the background, darkened by <paramref name="dim"/>.
        /// </summary>
        public static Drawable Picture(string url, float dim) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new DelayedLoadWrapper(() => new OnlinePicture(url)) { RelativeSizeAxes = Axes.Both },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex(@"100a16"),
                    Alpha = Math.Clamp(dim, 0, 1),
                },
            },
        };
    }

    /// <summary>
    /// A picture from the server, fetched while it loads.
    /// </summary>
    internal partial class OnlinePicture : Sprite
    {
        private readonly string url;

        public OnlinePicture(string url)
        {
            this.url = url;

            RelativeSizeAxes = Axes.Both;
            FillMode = FillMode.Fill;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures)
        {
            Texture = textures.Get(url);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(300, Easing.OutQuint);
        }
    }
}
