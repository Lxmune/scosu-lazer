// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: what the daily tasks tile and the login briefing card say, apart from how they look.
    /// </summary>
    public static class DailyTaskFormatting
    {
        /// <summary>
        /// How many lost first places the briefing names (it counts the rest).
        /// </summary>
        public const int MAX_SNIPES_SHOWN = 2;

        /// <summary>
        /// The task in words, with its number.
        /// </summary>
        public static LocalisableString Describe(APIDailyTasks.DailyTask task)
        {
            switch (task.Kind)
            {
                case @"gain_ranked_score":
                    return ScosuDailyStrings.GainRankedScore(ScosuMenuFormatting.CompactScore(task.Target));

                case @"improve_best":
                    return ScosuDailyStrings.ImproveBest(task.Param);

                case @"fail_then_pass":
                    return ScosuDailyStrings.FailThenPass;

                case @"return_to_map":
                    return ScosuDailyStrings.ReturnToMap(task.Param);

                case @"one_miss":
                    return ScosuDailyStrings.OneMiss;

                case @"daily_challenge_top":
                    return ScosuDailyStrings.DailyChallengeTop(task.Param);

                case @"take_first":
                    return ScosuDailyStrings.TakeFirst;

                case @"rising_stars":
                    return ScosuDailyStrings.RisingStars(task.Target);

                case @"full_combo_above":
                    return ScosuDailyStrings.FullComboAbove((task.Param / 100.0).ToString(@"0.#", CultureInfo.InvariantCulture));

                case @"high_accuracy":
                    return ScosuDailyStrings.HighAccuracy(task.Param);

                case @"rank_s":
                    return ScosuDailyStrings.RankS;

                case @"modded_pass":
                    return ScosuDailyStrings.ModdedPass;

                case @"new_map":
                    return ScosuDailyStrings.NewMap;

                case @"combo_above":
                    return ScosuDailyStrings.ComboAbove(task.Param.ToString(@"N0", CultureInfo.InvariantCulture));

                case @"different_maps":
                    return ScosuDailyStrings.DifferentMaps(task.Target);

                case @"long_map":
                    return ScosuDailyStrings.LongMap(Math.Round(task.Param / 60.0).ToString(CultureInfo.InvariantCulture));

                default:
                    // A task this build doesn't know yet: its key is better than nothing.
                    return task.Kind;
            }
        }

        /// <summary>
        /// How far along the task is, from 0 to 1.
        /// </summary>
        public static float Progress(APIDailyTasks.DailyTask task)
        {
            if (task.Completed)
                return 1;

            if (task.Target <= 0)
                return 0;

            return Math.Clamp((float)task.Progress / task.Target, 0, 1);
        }

        /// <summary>
        /// A rank change, better (lower) being positive: "+3", "-12"; null when either rank is unknown or it didn't change.
        /// </summary>
        public static string? RankDelta(long? before, long? now)
        {
            if (before is not long then || now is not long current || then == current)
                return null;

            long up = then - current;
            return up > 0 ? $@"+{up.ToString(@"N0", CultureInfo.InvariantCulture)}" : $@"-{(-up).ToString(@"N0", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// What changed since the last session, one line each: ranks, ranked score, first places lost, unread messages.
        /// Empty when nothing did (then there is no briefing to show). The daily tasks and the account standing aren't in it.
        /// </summary>
        public static IReadOnlyList<BriefingChange> ChangeLines(APIBriefing briefing)
        {
            var lines = new List<BriefingChange>();

            if (briefing.GlobalRank is APIBriefing.RankChange global && RankDelta(global.Before, global.Now) is string globalDelta)
                lines.Add(new BriefingChange(BriefingChangeKind.GlobalRank, ScosuDailyStrings.GlobalRankChange(formatRank(global.Now), globalDelta), global.Now < global.Before));

            if (briefing.CountryRank is APIBriefing.RankChange country && RankDelta(country.Before, country.Now) is string countryDelta)
                lines.Add(new BriefingChange(BriefingChangeKind.CountryRank, ScosuDailyStrings.CountryRankChange(formatRank(country.Now), countryDelta), country.Now < country.Before));

            if (briefing.RankedScore is APIBriefing.ScoreChange score && score.Now != score.Before)
            {
                long gain = score.Now - score.Before;
                lines.Add(new BriefingChange(BriefingChangeKind.RankedScore,
                    ScosuDailyStrings.RankedScoreChange($@"{(gain > 0 ? @"+" : string.Empty)}{ScosuMenuFormatting.CompactScore(gain)}"), gain > 0));
            }

            var named = briefing.Snipes.Take(MAX_SNIPES_SHOWN).ToList();

            foreach (var snipe in named)
            {
                lines.Add(new BriefingChange(BriefingChangeKind.LostFirst,
                    snipe.By != null ? ScosuDailyStrings.Sniped(snipe.By.Username, snipe.Title) : ScosuDailyStrings.LostFirst(snipe.Title), false, snipe.BeatmapId));
            }

            int unnamed = Math.Max(briefing.SnipeCount, named.Count) - named.Count;

            if (unnamed > 0)
                lines.Add(new BriefingChange(BriefingChangeKind.MoreLostFirsts, ScosuDailyStrings.MoreSnipes(unnamed), false));

            return lines;
        }

        /// <summary>
        /// One thing that changed since the last session.
        /// </summary>
        /// <param name="Kind">What it is about, which is where clicking it goes.</param>
        /// <param name="Text">It in words.</param>
        /// <param name="Improved">Whether it got better (a rank up, score gained) or worse (a rank down, a first place lost); null when neither.</param>
        /// <param name="BeatmapId">The beatmap of a lost first place.</param>
        public readonly record struct BriefingChange(BriefingChangeKind Kind, LocalisableString Text, bool? Improved = null, long BeatmapId = 0);

        public enum BriefingChangeKind
        {
            GlobalRank,
            CountryRank,
            RankedScore,
            LostFirst,
            MoreLostFirsts,
        }

        private static string formatRank(long? rank) => rank is long r ? $@"#{r.ToString(@"N0", CultureInfo.InvariantCulture)}" : @"-";
    }
}
