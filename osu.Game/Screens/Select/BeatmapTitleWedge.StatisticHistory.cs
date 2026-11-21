// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        /// <summary>
        /// On servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>), for the logged in user:
        /// their history on the selected beatmap in the selected leaderboard (including Relax and Autopilot): plays, pass rate, starts never finished,
        /// the closest attempt to a full combo, and the ranked score beating their best would add (the server's estimate).
        /// Looks the server up once the selection has settled, so scrolling the carousel doesn't send a request per beatmap.
        /// </summary>
        public partial class StatisticHistory : FillFlowContainer
        {
            /// <summary>
            /// How long the selection has to stay put before the server is asked.
            /// </summary>
            public const double DEBOUNCE_DELAY = 400;

            [Resolved]
            private IBindable<WorkingBeatmap> working { get; set; } = null!;

            [Resolved]
            private IBindable<RulesetInfo> ruleset { get; set; } = null!;

            [Resolved]
            private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            private readonly Statistic plays;
            private readonly Statistic closest;

            private ScheduledDelegate? pendingLookup;
            private GetBeatmapAttemptsRequest? attemptsRequest;
            private GetBeatmapGainRequest? gainRequest;

            private APIBeatmapAttempts? attempts;
            private APIBeatmapGain? gain;

            public StatisticHistory()
            {
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Vertical;
                Spacing = new Vector2(0, 2);
                Alpha = 0;

                Children = new Drawable[]
                {
                    plays = new Statistic(FontAwesome.Solid.History)
                    {
                        TooltipText = ScosuResultsStrings.HistoryTooltip,
                        Alpha = 0,
                    },
                    closest = new Statistic(FontAwesome.Solid.Bullseye)
                    {
                        TooltipText = ScosuResultsStrings.HistoryTooltip,
                        Alpha = 0,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                working.BindValueChanged(_ => Refresh());
                ruleset.BindValueChanged(_ => Refresh());
                mods.BindValueChanged(_ => Refresh());
                api.LocalUser.BindValueChanged(_ => Refresh());

                Refresh();
            }

            private RulesetInfo currentLeaderboard => ruleset.Value.GetLeaderboardRuleset(mods.Value.Select(m => m.Acronym));

            /// <summary>
            /// Looks the history up again (it changes after a play), after the debounce delay.
            /// </summary>
            public void Refresh()
            {
                pendingLookup?.Cancel();
                attemptsRequest?.Cancel();
                gainRequest?.Cancel();
                attemptsRequest = null;
                gainRequest = null;
                attempts = null;
                gain = null;

                var beatmap = working.Value.BeatmapInfo;

                if (!api.Endpoints.RanksByStableScore || !api.IsLoggedIn || !ruleset.Value.IsLegacyRuleset() || beatmap.OnlineID <= 0)
                {
                    this.FadeOut(120, Easing.OutQuint);
                    return;
                }

                pendingLookup = Scheduler.AddDelayed(() => lookup(beatmap, currentLeaderboard), DEBOUNCE_DELAY);
            }

            private void lookup(IBeatmapInfo beatmap, RulesetInfo leaderboard)
            {
                var attemptsRequest = this.attemptsRequest = new GetBeatmapAttemptsRequest(beatmap, leaderboard);
                attemptsRequest.Success += result => Schedule(() =>
                {
                    if (attemptsRequest != this.attemptsRequest)
                        return;

                    attempts = result;
                    updateText();
                });
                api.Queue(attemptsRequest);

                // Giving no ranked score (a loved map is fine, a graveyard one isn't) leaves the estimate empty, which is simply not shown.
                var gainRequest = this.gainRequest = new GetBeatmapGainRequest(api.LocalUser.Value.OnlineID, beatmap, leaderboard);
                gainRequest.Success += result => Schedule(() =>
                {
                    if (gainRequest != this.gainRequest)
                        return;

                    gain = result;
                    updateText();
                });
                api.Queue(gainRequest);
            }

            private void updateText()
            {
                var playsText = attempts == null ? null : ScosuBeatmapHistoryFormatter.FormatPlays(attempts);
                var closestText = ScosuBeatmapHistoryFormatter.FormatClosest(attempts, gain);

                plays.Text = playsText;
                plays.Alpha = playsText == null ? 0 : 1;
                closest.Text = closestText;
                closest.Alpha = closestText == null ? 0 : 1;

                if (playsText == null && closestText == null)
                    this.FadeOut(120, Easing.OutQuint);
                else
                    this.FadeIn(120, Easing.OutQuint);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                pendingLookup?.Cancel();
                attemptsRequest?.Cancel();
                gainRequest?.Cancel();
            }
        }
    }
}
