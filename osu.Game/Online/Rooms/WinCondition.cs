// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Online.Rooms
{
    /// <summary>
    /// scosu: what decides the results of a multiplayer playlist item.
    /// Ties under <see cref="Accuracy"/> and <see cref="Combo"/> are broken by score.
    /// </summary>
    /// <remarks>
    /// Sent as a number over the multiplayer hub (scosu's key 12 of <see cref="MultiplayerPlaylistItem"/>)
    /// and in snake case (<c>score</c>, <c>accuracy</c>, <c>combo</c>) in the web API, so the names and values shouldn't be changed.
    /// </remarks>
    public enum WinCondition
    {
        [LocalisableDescription(typeof(ScosuMultiplayerStrings), nameof(ScosuMultiplayerStrings.WinConditionScore))]
        Score = 0,

        [LocalisableDescription(typeof(ScosuMultiplayerStrings), nameof(ScosuMultiplayerStrings.WinConditionAccuracy))]
        Accuracy = 1,

        [LocalisableDescription(typeof(ScosuMultiplayerStrings), nameof(ScosuMultiplayerStrings.WinConditionCombo))]
        Combo = 2,
    }
}
