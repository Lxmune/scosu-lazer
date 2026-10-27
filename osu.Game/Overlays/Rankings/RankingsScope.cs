// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Rankings
{
    public enum RankingsScope
    {
        [LocalisableDescription(typeof(RankingsStrings), nameof(RankingsStrings.StatPerformance))]
        Performance,

        [LocalisableDescription(typeof(RankingsStrings), nameof(RankingsStrings.StatRankedScore))]
        Score,

        /// <summary>
        /// The ranked score players gained this week or month. Only on servers that rank by osu!stable score.
        /// </summary>
        [LocalisableDescription(typeof(GainedRankingsStrings), nameof(GainedRankingsStrings.Gained))]
        Gained,

        [LocalisableDescription(typeof(RankingsStrings), nameof(RankingsStrings.TypeCountry))]
        Country,

        [LocalisableDescription(typeof(RankingsStrings), nameof(RankingsStrings.TypePlaylists))]
        Playlists,

        [LocalisableDescription(typeof(RankingsStrings), nameof(RankingsStrings.TypeKudosu))]
        Kudosu,
    }
}
