// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Utils;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;

namespace osu.Game.Online
{
    /// <summary>
    /// scosu: when the player comes back to the main menu after playing a while, a notification sums up the session so far:
    /// plays and ranked score gained (from the server, in the selected mode), new full combos and S ranks, and the best play (clicking opens it).
    /// </summary>
    public partial class SessionSummaryNotifier : Component
    {
        /// <summary>
        /// How long after the start (or the last summary) the next one may come.
        /// </summary>
        public static readonly TimeSpan MIN_INTERVAL = TimeSpan.FromMinutes(30);

        /// <summary>
        /// How many new local scores (passes) a summary needs.
        /// </summary>
        public const int MIN_NEW_SCORES = 3;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly DateTimeOffset sessionStart = DateTimeOffset.Now;
        private DateTimeOffset lastSummary = DateTimeOffset.Now;

        /// <summary>
        /// Session passes per ruleset at the last summary (summaries are per ruleset, see <see cref="sessionScores"/>).
        /// </summary>
        private readonly Dictionary<string, int> scoresAtLastSummary = new Dictionary<string, int>();

        private GetScoreSummaryRequest? request;

        /// <summary>
        /// Called when the main menu shows again (after song select, a play, results...).
        /// </summary>
        public void MenuResumed()
        {
            if (request != null || api.State.Value != APIState.Online || !api.Endpoints.RanksByStableScore)
                return;

            var now = DateTimeOffset.Now;
            if (now - lastSummary < MIN_INTERVAL)
                return;

            var scores = sessionScores();
            string mode = ruleset.Value.ShortName;
            if (scores.Count - scoresAtLastSummary.GetValueOrDefault(mode) < MIN_NEW_SCORES)
                return;

            lastSummary = now;
            scoresAtLastSummary[mode] = scores.Count;

            if (!ruleset.Value.IsLegacyRuleset())
            {
                post(new SessionStats(scores, null, null));
                return;
            }

            var req = request = new GetScoreSummaryRequest(ruleset.Value, sessionStart);
            req.Success += summary =>
            {
                request = null;
                post(new SessionStats(scores, summary.Today.PlayCount, summary.Today.RankedScore));
            };
            req.Failure += _ =>
            {
                request = null;
                post(new SessionStats(scores, null, null));
            };
            api.Queue(req);
        }

        /// <summary>
        /// The local user's scores (passes) set since the game started, in the selected ruleset: the server's plays and
        /// ranked score are for that ruleset, so every number of one summary describes the same mode.
        /// </summary>
        private List<ScoreInfo> sessionScores()
        {
            int userId = api.LocalUser.Value.Id;
            string mode = ruleset.Value.ShortName;

            return realm.Run(r => ScoresOf(r.All<ScoreInfo>().Where(s => s.Date >= sessionStart && !s.DeletePending), userId, mode)
                                  .Select(s => s.Detach())
                                  .ToList());
        }

        /// <summary>
        /// The scores of <paramref name="userId"/> in the ruleset <paramref name="rulesetShortName"/>.
        /// </summary>
        public static IEnumerable<ScoreInfo> ScoresOf(IEnumerable<ScoreInfo> scores, int userId, string rulesetShortName)
            => scores.AsEnumerable().Where(s => s.User.OnlineID == userId && s.Ruleset.ShortName == rulesetShortName);

        private void post(SessionStats stats) => Schedule(() =>
        {
            var best = stats.Best;

            notifications?.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.ChartLine,
                Text = stats.Describe(ruleset.Value.Name),
                Activated = () =>
                {
                    if (best != null)
                        game?.PresentScore(best);
                    return true;
                },
            });
        });

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }

        /// <summary>
        /// What a session summary says, apart from how it is shown.
        /// </summary>
        public class SessionStats
        {
            /// <summary>
            /// Plays submitted in the selected mode (passed or not), when the server said. Never fewer than <see cref="Passes"/>:
            /// every pass is also a play, and the server may not have processed the latest ones yet.
            /// </summary>
            public readonly int? Plays;

            /// <summary>
            /// Ranked score gained in the selected mode, when the server said.
            /// </summary>
            public readonly long? RankedScoreGained;

            public readonly int Passes;
            public readonly int FullCombos;
            public readonly int SRanks;

            /// <summary>
            /// The play with the most pp (or score, without pp).
            /// </summary>
            public readonly ScoreInfo? Best;

            public SessionStats(IReadOnlyCollection<ScoreInfo> scores, int? plays, long? rankedScoreGained)
            {
                Passes = scores.Count;
                Plays = plays is int count ? Math.Max(count, Passes) : null;
                RankedScoreGained = rankedScoreGained;
                FullCombos = scores.Count(s => s.IsPerfect());
                SRanks = scores.Count(s => s.Rank >= ScoreRank.S);
                Best = scores.OrderByDescending(s => s.PP ?? 0).ThenByDescending(s => s.TotalScore).FirstOrDefault();
            }

            /// <summary>
            /// "This session: 23 plays · +12.3M ranked score (osu!) · 3 full combos · 2 S ranks. Best: Artist - Title [Diff], 98.21%"
            /// </summary>
            public LocalisableString Describe(string rulesetName)
            {
                var parts = new List<string>();

                parts.Add(Plays is int plays ? ScosuSessionStrings.Plays(plays).ToString() : ScosuSessionStrings.Passes(Passes).ToString());

                if (RankedScoreGained is long gained)
                    parts.Add(ScosuSessionStrings.RankedScore($@"+{ScosuMenuFormatting.CompactScore(gained)}", rulesetName).ToString());

                if (FullCombos > 0)
                    parts.Add(ScosuSessionStrings.FullCombos(FullCombos).ToString());

                if (SRanks > 0)
                    parts.Add(ScosuSessionStrings.SRanks(SRanks).ToString());

                string summary = string.Join(@" · ", parts);

                if (Best == null)
                    return ScosuSessionStrings.Summary(summary);

                var beatmap = Best.BeatmapInfo;
                string map = beatmap == null
                    ? string.Empty
                    : $@"{beatmap.Metadata.Artist} - {beatmap.Metadata.Title} [{beatmap.DifficultyName}]";

                return ScosuSessionStrings.SummaryWithBest(summary, map, Best.Accuracy.FormatAccuracy().ToString());
            }
        }
    }
}
