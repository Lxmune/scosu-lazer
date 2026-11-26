// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Game.Beatmaps;

namespace osu.Game.Scoring
{
    /// <summary>
    /// The performance points of a score: the server's when it has them, otherwise worked out here.
    /// </summary>
    public static class ScorePerformance
    {
        /// <returns>The score's pp, or <see langword="null"/> when they can't be worked out (the beatmap or the ruleset isn't available locally).</returns>
        public static async Task<double?> CalculateAsync(ScoreInfo score, BeatmapDifficultyCache difficultyCache, CancellationToken cancellationToken = default)
        {
            if (score.PP.HasValue)
                return score.PP.Value;

            var attributes = await difficultyCache.GetDifficultyAsync(score.BeatmapInfo!, score.Ruleset, score.Mods, cancellationToken).ConfigureAwait(false);
            var performanceCalculator = score.Ruleset.CreateInstance().CreatePerformanceCalculator();

            // Performance calculation requires the beatmap and ruleset to be locally available.
            if (attributes?.DifficultyAttributes == null || performanceCalculator == null)
                return null;

            var result = await performanceCalculator.CalculateAsync(score, attributes.Value.DifficultyAttributes, cancellationToken).ConfigureAwait(false);
            return result.Total;
        }
    }
}
