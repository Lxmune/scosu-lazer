// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Humanizer;
using osu.Framework.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Beatmaps.Drawables.Cards.Statistics
{
    /// <summary>
    /// Shows a play or favourite count taken from bancho (the official server), as opposed to scosu's own counts.
    /// </summary>
    public partial class BanchoStatistic : BeatmapCardStatistic
    {
        private BanchoStatistic(IconUsage icon, int count, string tooltipPrefix, bool withLabel)
        {
            Icon = icon;
            Text = count.ToMetric(decimals: 1);
            TooltipText = $"{tooltipPrefix} on bancho: {count:N0}";

            if (withLabel)
                SourceLabel = "bancho";
        }

        /// <summary>
        /// Creates the bancho play count statistic, or <c>null</c> if the server did not send one.
        /// </summary>
        /// <param name="onlineInfo">The beatmap set.</param>
        /// <param name="withLabel">Whether to show the "bancho" source label (used on the first statistic of the group).</param>
        public static BanchoStatistic? CreatePlaysFor(IBeatmapSetOnlineInfo onlineInfo, bool withLabel = true)
            => onlineInfo is APIBeatmapSet { BanchoPlayCount: int plays } ? new BanchoStatistic(FontAwesome.Solid.PlayCircle, plays, "Plays", withLabel) : null;

        /// <summary>
        /// Creates the bancho favourite count statistic, or <c>null</c> if the server did not send one.
        /// </summary>
        public static BanchoStatistic? CreateFavouritesFor(IBeatmapSetOnlineInfo onlineInfo, bool withLabel = false)
            => onlineInfo is APIBeatmapSet { BanchoFavouriteCount: int favourites } ? new BanchoStatistic(FontAwesome.Regular.Heart, favourites, "Favourites", withLabel) : null;
    }
}
