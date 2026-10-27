// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Scoring;

namespace osu.Game.Database
{
    /// <summary>
    /// The <see cref="StableScoreAttributes"/> of beatmaps, on servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public partial class StableScoreAttributesCache : MemoryCachingComponent<(int BeatmapId, int RulesetId), StableScoreAttributes>
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        // A failed lookup (such as when offline) is tried again next time.
        protected override bool CacheNullValues => false;

        /// <summary>
        /// The attributes of a beatmap played in a ruleset.
        /// </summary>
        /// <param name="beatmapId">The online ID of the beatmap.</param>
        /// <param name="rulesetId">The online ID of the ruleset played (0 to 3).</param>
        /// <param name="token">An optional cancellation token.</param>
        /// <returns>The attributes, or <see langword="null"/> if the server doesn't rank plays by osu!stable score or the request failed.</returns>
        public Task<StableScoreAttributes?> GetAttributesAsync(int beatmapId, int rulesetId, CancellationToken token = default)
            => GetAsync((beatmapId, rulesetId), token);

        protected override async Task<StableScoreAttributes?> ComputeValueAsync((int BeatmapId, int RulesetId) lookup, CancellationToken token = default)
        {
            if (!api.Endpoints.RanksByStableScore || lookup.BeatmapId <= 0 || lookup.RulesetId < 0)
                return null;

            var request = new GetStableScoreAttributesRequest(lookup.BeatmapId, lookup.RulesetId);
            await api.PerformAsync(request).ConfigureAwait(false);
            return request.Response;
        }
    }
}
