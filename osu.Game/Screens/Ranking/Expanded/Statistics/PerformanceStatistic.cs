// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Scoring;
using osu.Game.Localisation;
using osu.Game.Utils;

namespace osu.Game.Screens.Ranking.Expanded.Statistics
{
    public partial class PerformanceStatistic : StatisticDisplay, IHasTooltip
    {
        public LocalisableString TooltipText { get; private set; }

        private readonly ScoreInfo score;

        private readonly Bindable<int> performance = new Bindable<int>();

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private RollingCounter<int> counter = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public PerformanceStatistic(ScoreInfo score)
            : base(BeatmapsetsStrings.ShowScoreboardHeaderspp)
        {
            this.score = score;
        }

        [BackgroundDependencyLoader]
        private void load(BeatmapDifficultyCache difficultyCache, CancellationToken? cancellationToken)
        {
            if (score.PP.HasValue)
            {
                setPerformanceValue(score, score.PP.Value);
            }
            else
            {
                Task.Run(async () =>
                {
                    var attributes = await difficultyCache.GetDifficultyAsync(score.BeatmapInfo!, score.Ruleset, score.Mods, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    var performanceCalculator = score.Ruleset.CreateInstance().CreatePerformanceCalculator();

                    // Performance calculation requires the beatmap and ruleset to be locally available. If not, return a default value.
                    if (attributes?.DifficultyAttributes == null || performanceCalculator == null)
                        return;

                    var result = await performanceCalculator.CalculateAsync(score, attributes.Value.DifficultyAttributes, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

                    Schedule(() => setPerformanceValue(score, result.Total));
                }, cancellationToken ?? CancellationToken.None);
            }
        }

        private void setPerformanceValue(ScoreInfo scoreInfo, double? pp)
        {
            if (pp.HasValue)
            {
                performance.Value = (int)Math.Round(pp.Value, MidpointRounding.AwayFromZero);

                if (!scoreInfo.BeatmapInfo!.Status.GrantsPerformancePoints())
                {
                    Alpha = 0.5f;
                    TooltipText = ResultsScreenStrings.NoPPForUnrankedBeatmaps;
                }
                else if (hasUnrankedMods(scoreInfo))
                {
                    Alpha = 0.5f;
                    TooltipText = ResultsScreenStrings.NoPPForUnrankedMods;
                }
                else if (scoreInfo.Rank == ScoreRank.F)
                {
                    Alpha = 0.5f;
                    TooltipText = ResultsScreenStrings.NoPPForFailedScores;
                }
                else
                {
                    Alpha = 1f;
                    TooltipText = default;
                }
            }
        }

        private bool hasUnrankedMods(ScoreInfo scoreInfo) => ModUtils.HasUnrankedMods(scoreInfo.Mods, scoreInfo.Ruleset, scoreInfo.IsLegacyScore, api.Endpoints.RanksByStableScore);

        public override void Appear()
        {
            base.Appear();
            counter.Current.BindTo(performance);
        }

        protected override void Dispose(bool isDisposing)
        {
            cancellationTokenSource.Cancel();
            base.Dispose(isDisposing);
        }

        protected override Drawable CreateContent() => counter = new StatisticCounter
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre
        };
    }
}
