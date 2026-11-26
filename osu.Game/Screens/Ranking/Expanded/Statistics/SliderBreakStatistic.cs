// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Screens.Ranking.Expanded.Statistics
{
    /// <summary>
    /// scosu: an osu! play's slider breaks, shown next to its judgements as osu!stable players count them.
    /// </summary>
    public partial class SliderBreakStatistic : CounterStatistic, IHasTooltip
    {
        public LocalisableString TooltipText => ScosuOutcomeStrings.SliderBreaksTooltip;

        public SliderBreakStatistic(IReadOnlyDictionary<HitResult, int> statistics)
            : base(ScosuOutcomeStrings.SliderBreakHeader, Count(statistics))
        {
        }

        /// <summary>
        /// The slider breaks among an osu! play's judgements: its missed slider ticks and repeats, which reset the combo.
        /// </summary>
        public static int Count(IReadOnlyDictionary<HitResult, int> statistics) => statistics.GetValueOrDefault(HitResult.LargeTickMiss);

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            HeaderText.Colour = colours.Orange1;
        }
    }
}
