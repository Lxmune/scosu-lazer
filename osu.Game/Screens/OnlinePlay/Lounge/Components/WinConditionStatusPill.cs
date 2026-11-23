// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.Rooms;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Lounge.Components
{
    /// <summary>
    /// scosu: shows "Accuracy wins" or "Combo wins" when the current playlist item isn't decided by score.
    /// </summary>
    public partial class WinConditionStatusPill : OnlinePlayPill
    {
        private readonly Room room;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        protected override FontUsage Font => base.Font.With(weight: FontWeight.SemiBold);

        public WinConditionStatusPill(Room room)
        {
            this.room = room;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Pill.Background.Alpha = 1;
            Pill.Background.Colour = colours.Orange1;

            TextFlow.Colour = Color4.Black;

            room.PropertyChanged += onRoomPropertyChanged;
            updateStatus();
        }

        private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Room.CurrentPlaylistItem):
                case nameof(Room.Playlist):
                    updateStatus();
                    break;
            }
        }

        private void updateStatus()
        {
            PlaylistItem? currentItem = room.Playlist.GetCurrentItem() ?? room.CurrentPlaylistItem;

            switch (currentItem?.WinCondition)
            {
                case WinCondition.Accuracy:
                    TextFlow.Text = ScosuMultiplayerStrings.AccuracyWins;
                    Alpha = 1;
                    break;

                case WinCondition.Combo:
                    TextFlow.Text = ScosuMultiplayerStrings.ComboWins;
                    Alpha = 1;
                    break;

                default:
                    Alpha = 0;
                    break;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            room.PropertyChanged -= onRoomPropertyChanged;
        }
    }
}
