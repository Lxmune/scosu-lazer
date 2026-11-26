// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: how many of the play's hits were early, and how many late: "62% early · 38% late".
    /// The average hit error says which way the play leant, this says how much of it did.
    /// </summary>
    public partial class EarlyLateShare : SimpleStatisticItem<(double Early, double Late)?>
    {
        public EarlyLateShare(IEnumerable<HitEvent> hitEvents)
            : base(ScosuResultsStrings.EarlyLateTitle)
        {
            var offsets = hitEvents.Where(HitEventExtensions.AffectsUnstableRate).Select(e => e.TimeOffset).ToList();

            // Hits right on time count as neither.
            int early = offsets.Count(o => o < 0);
            int late = offsets.Count(o => o > 0);

            if (offsets.Count > 0)
                Value = ((double)early / offsets.Count, (double)late / offsets.Count);
        }

        protected override LocalisableString DisplayValue((double Early, double Late)? value) => value is var (early, late)
            ? ScosuResultsStrings.EarlyLateShares(early.ToLocalisableString(@"0%"), late.ToLocalisableString(@"0%"))
            : RankingStatisticsStrings.NotAvailable;
    }
}
