// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Ranking.Expanded.Accuracy;
using osuTK;

namespace osu.Game.Screens.Ranking.Expanded.Statistics
{
    /// <summary>
    /// scosu: the ranked score a play just submitted added, in place of its pp: scosu ranks players by score.
    /// </summary>
    public partial class RankedScoreStatistic : StatisticDisplay, IHasTooltip
    {
        public LocalisableString TooltipText => gain.Tooltip;

        private readonly ScosuPlayOutcomeFormatter.Gain gain;

        private RollingCounter<long> counter = null!;

        public RankedScoreStatistic(APIPlayOutcome outcome)
            : base(ScosuOutcomeStrings.RankedScore)
        {
            gain = ScosuPlayOutcomeFormatter.RankedScoreGain(outcome);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // As lazer does for pp a play doesn't give: dimmed, with the reason on hover.
            if (gain.Amount <= 0)
                Alpha = 0.5f;
        }

        public override void Appear()
        {
            base.Appear();
            counter.Current.Value = gain.Amount;
        }

        protected override Drawable CreateContent() => counter = new GainCounter
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
        };

        private partial class GainCounter : RollingCounter<long>
        {
            protected override double RollingDuration => AccuracyCircle.ACCURACY_TRANSFORM_DURATION;

            protected override Easing RollingEasing => AccuracyCircle.ACCURACY_TRANSFORM_EASING;

            protected override LocalisableString FormatCount(long count)
                => count > 0 ? LocalisableString.Interpolate($@"+{count.ToLocalisableString(@"N0")}") : count.ToLocalisableString(@"N0");

            // A little smaller than the other statistics: gains run to eight digits.
            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
            {
                s.Font = OsuFont.Torus.With(size: 18, fixedWidth: true);
                s.Spacing = new Vector2(-2, 0);
            });
        }
    }
}
