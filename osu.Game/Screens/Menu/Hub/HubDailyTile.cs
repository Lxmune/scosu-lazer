// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.Metadata;
using osu.Game.Online.Rooms;
using osu.Game.Overlays.ScosuEvent;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// Today's daily challenge: the map, and how long is left to play it.
    /// </summary>
    public partial class HubDailyTile : HubTile
    {
        /// <summary>
        /// Called when the tile is clicked while there is a challenge to open.
        /// </summary>
        public Action<Room>? OnDailyChallenge;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        private readonly IBindable<DailyChallengeInfo?> info = new Bindable<DailyChallengeInfo?>();

        private GetRoomRequest? request;
        private Room? room;

        /// <summary>
        /// Today's challenge room, once it has loaded (null when there is no challenge).
        /// </summary>
        public Room? Room => room;

        public HubDailyTile()
        {
            Eyebrow.Text = MenuHubStrings.DailyChallenge.ToUpper();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (metadataClient != null)
                info.BindTo(metadataClient.DailyChallengeInfo);

            info.BindValueChanged(_ => fetch(), true);

            Scheduler.AddDelayed(updateRemaining, 1000, true);
        }

        private void fetch()
        {
            request?.Cancel();
            request = null;
            room = null;

            if (info.Value is not DailyChallengeInfo challenge)
            {
                ShowPlaceholder(FontAwesome.Solid.Clock, MenuHubStrings.NoDailyChallenge);
                return;
            }

            // Until the room arrives, there is nothing worth showing.
            ShowPlaceholder(FontAwesome.Solid.Clock, MenuHubStrings.TodaysChallenge);

            var req = request = new GetRoomRequest(challenge.RoomID);

            req.Success += r => Schedule(() =>
            {
                if (req != request)
                    return;

                display(r);
            });
            api.Queue(req);
        }

        private void display(Room r)
        {
            room = r;

            var beatmap = r.Playlist.FirstOrDefault()?.Beatmap;

            ShowContent();

            if (beatmap?.BeatmapSet is APIBeatmapSet set)
            {
                SetCoverBackground(new UpdateableOnlineBeatmapSetCover(timeBeforeLoad: 0, timeBeforeUnload: 600_000)
                {
                    RelativeSizeAxes = Axes.Both,
                    OnlineInfo = set,
                });
            }

            if (beatmap != null)
            {
                var metadata = beatmap.Metadata;

                Body.Add(new TruncatingSpriteText
                {
                    RelativeSizeAxes = Axes.X,
                    Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                    Font = ScosuMenuStyle.Display(20),
                });
                Body.Add(new TruncatingSpriteText
                {
                    RelativeSizeAxes = Axes.X,
                    Text = $@"{beatmap.DifficultyName} · {beatmap.StarRating.ToString(@"0.0#", CultureInfo.InvariantCulture)}★",
                    Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold),
                    Colour = ScosuMenuStyle.TEXT_DIM,
                });
            }
            else
            {
                Body.Add(new TruncatingSpriteText
                {
                    RelativeSizeAxes = Axes.X,
                    Text = r.Name,
                    Font = ScosuMenuStyle.Display(20),
                });
            }

            SetAction(() => OnDailyChallenge?.Invoke(r));
            updateRemaining();
        }

        private void updateRemaining()
        {
            if (room?.EndDate is not DateTimeOffset end)
                return;

            var left = end - DateTimeOffset.Now;

            if (left <= TimeSpan.Zero)
            {
                SetFoot(null, default);
                return;
            }

            // Hours and minutes are enough here; the seconds only matter for events.
            SetFoot(FontAwesome.Solid.Clock, MenuHubStrings.EndsInTime(EventDisplay.ShortCountdown(left)));
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
