// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        /// <summary>
        /// On servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>), for beatmaps that give ranked score:
        /// the stable score of a perfect play with the selected mods, next to the local user's best, which is what the beatmap adds to their ranked score.
        /// </summary>
        public partial class StatisticMaximumScore : Statistic
        {
            [Resolved]
            private IBindable<WorkingBeatmap> working { get; set; } = null!;

            [Resolved]
            private IBindable<RulesetInfo> ruleset { get; set; } = null!;

            [Resolved]
            private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            [Resolved]
            private StableScoreAttributesCache attributesCache { get; set; } = null!;

            private ModSettingChangeTracker? settingChangeTracker;
            private CancellationTokenSource? cancellationSource;
            private GetUserBestStableScoreRequest? bestRequest;

            /// <summary>
            /// The leaderboard the local user's best was looked up on, which Relax and Autopilot change.
            /// </summary>
            private RulesetInfo? leaderboard;

            private bool attributesLoaded;
            private StableScoreAttributes? attributes;
            private long? best;

            public StatisticMaximumScore()
                : base(OsuIcon.Crown)
            {
                TooltipText = StableScoreStrings.MaximumScoreTooltip;
                Alpha = 0;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                working.BindValueChanged(_ => Refresh());
                ruleset.BindValueChanged(_ => Refresh());
                mods.BindValueChanged(m =>
                {
                    settingChangeTracker?.Dispose();
                    settingChangeTracker = new ModSettingChangeTracker(m.NewValue);
                    settingChangeTracker.SettingChanged += _ => updateText();

                    if (leaderboard != null && !leaderboard.Equals(currentLeaderboard))
                        Refresh();
                    else
                        updateText();
                });

                Refresh();
            }

            private RulesetInfo currentLeaderboard => ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym));

            /// <summary>
            /// Looks the beatmap's attributes and the local user's best up again (the best changes after a play).
            /// </summary>
            public void Refresh()
            {
                cancellationSource?.Cancel();
                bestRequest?.Cancel();
                bestRequest = null;
                leaderboard = null;
                attributesLoaded = false;
                attributes = null;
                best = null;

                var beatmap = working.Value.BeatmapInfo;

                if (!api.Endpoints.RanksByStableScore || !ruleset.Value.IsLegacyRuleset() || beatmap.OnlineID <= 0 || !StableScoreCalculator.GivesRankedScore(beatmap.Status))
                {
                    this.FadeOut(120, Easing.OutQuint);
                    return;
                }

                this.FadeIn(120, Easing.OutQuint);
                Text = null;

                var token = (cancellationSource = new CancellationTokenSource()).Token;

                attributesCache.GetAttributesAsync(beatmap.OnlineID, ruleset.Value.OnlineID, token).ContinueWith(task => Schedule(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    attributesLoaded = true;
                    attributes = task.GetResultSafely();
                    updateText();
                }), token);

                leaderboard = currentLeaderboard;

                if (!api.IsLoggedIn)
                    return;

                var request = bestRequest = new GetUserBestStableScoreRequest(beatmap, leaderboard);
                request.Success += _ => Schedule(() =>
                {
                    if (request != bestRequest)
                        return;

                    best = request.BestStableScore;
                    updateText();
                });
                api.Queue(request);
            }

            private void updateText()
            {
                if (!attributesLoaded)
                    return;

                if (attributes == null)
                {
                    this.FadeOut(120, Easing.OutQuint);
                    return;
                }

                long maximum = StableScoreCalculator.MaximumScore(attributes, ruleset.Value.OnlineID, mods.Value.Select(m => new APIMod(m)).ToArray());
                var maximumText = maximum.ToLocalisableString(@"N0");

                if (best is not long bestScore || bestScore <= 0)
                    Text = StableScoreStrings.MaximumScore(maximumText);
                else if (bestScore < maximum)
                    Text = StableScoreStrings.BestAndMaximumScoreWithGap(bestScore.ToLocalisableString(@"N0"), maximumText, (maximum - bestScore).ToLocalisableString(@"N0"));
                else
                    Text = StableScoreStrings.BestAndMaximumScore(bestScore.ToLocalisableString(@"N0"), maximumText);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                settingChangeTracker?.Dispose();
                cancellationSource?.Cancel();
                bestRequest?.Cancel();
            }
        }
    }
}
