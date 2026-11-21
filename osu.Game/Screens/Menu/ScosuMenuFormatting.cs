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
    /// What the main menu's score summary and weekly report card say (and which report is news), apart from how they look.
    /// </summary>
    public static class ScosuMenuFormatting
    {
        /// <summary>
        /// Reports older than this (counted from their week's start) aren't news any more.
        /// </summary>
        public static readonly TimeSpan MAX_REPORT_AGE = TimeSpan.FromDays(14);

        /// <summary>
        /// How many highlights the card lists.
        /// </summary>
        public const int MAX_HIGHLIGHTS = 3;

        /// <summary>
        /// "950", "12.3K", "2.4M" or "1.2B".
        /// </summary>
        public static string CompactScore(long score)
        {
            long abs = Math.Abs(score);

            if (abs < 1_000)
                return score.ToString(CultureInfo.InvariantCulture);

            (double divisor, string suffix) = abs switch
            {
                < 1_000_000 => (1e3, @"K"),
                < 1_000_000_000 => (1e6, @"M"),
                _ => (1e9, @"B"),
            };

            return $@"{(score / divisor).ToString(@"0.#", CultureInfo.InvariantCulture)}{suffix}";
        }

        /// <summary>
        /// The newest report the user hasn't seen: later than <paramref name="seenWeek"/> (a week's start, <c>yyyy-MM-dd</c>, or empty)
        /// and recent. <c>null</c> when there is none.
        /// </summary>
        public static APIWeeklyReport? FindUnseenReport(IEnumerable<APIWeeklyReport> reports, string seenWeek, DateTimeOffset now)
        {
            APIWeeklyReport? newest = null;

            foreach (var report in reports)
            {
                if (!TryParseWeek(report.WeekStart, out var start))
                    continue;

                if (now - start > MAX_REPORT_AGE)
                    continue;

                if (newest == null || string.CompareOrdinal(report.WeekStart, newest.WeekStart) > 0)
                    newest = report;
            }

            if (newest == null || (!string.IsNullOrEmpty(seenWeek) && string.CompareOrdinal(newest.WeekStart, seenWeek) <= 0))
                return null;

            return newest;
        }

        public static bool TryParseWeek(string week, out DateTimeOffset start)
            => DateTimeOffset.TryParseExact(week, @"yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out start);

        /// <summary>
        /// The highlights to show, ranked score first, then the ones that got better, at most <see cref="MAX_HIGHLIGHTS"/>.
        ///
        /// </summary>
        public static IReadOnlyList<APIWeeklyReport.Highlight> PickHighlights(APIWeeklyReport report)
        {
            return report.Highlights
                         .Select((h, i) => (h, i))
                         .OrderBy(x => x.h.Measure == @"ranked_score" ? 0 : x.h.Change == @"improved" ? 1 : 2)
                         .ThenBy(x => x.i)
                         .Select(x => x.h)
                         .Take(MAX_HIGHLIGHTS)
                         .ToList();
        }

        /// <summary>
        /// A highlight's value: "4.2M" (ranked score), "96.8%", "5.4★", "-3.1ms", or a number.
        /// </summary>
        public static string FormatValue(string measure, double value)
        {
            switch (measure)
            {
                case @"ranked_score":
                    return CompactScore((long)Math.Round(value));

                case @"accuracy":
                case @"pass_rate":
                    return (value * 100).ToString(@"0.0", CultureInfo.InvariantCulture) + @"%";

                case @"stars":
                    return value.ToString(@"0.00", CultureInfo.InvariantCulture) + @"★";

                case @"offset":
                case @"stream_rush":
                    return (value > 0 ? @"+" : string.Empty) + value.ToString(@"0.0", CultureInfo.InvariantCulture) + @"ms";

                case @"spinner_rpm":
                    return Math.Round(value).ToString(CultureInfo.InvariantCulture);

                default:
                    return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// "+4.2M ranked score" or "UR 92 → 87".
        /// </summary>
        public static LocalisableString HighlightText(APIWeeklyReport.Highlight highlight)
        {
            if (highlight.Measure == @"ranked_score")
                return ScosuMenuStrings.HighlightRankedScore($@"+{FormatValue(highlight.Measure, highlight.Value)}");

            return ScosuMenuStrings.HighlightChange(
                ScosuMenuStrings.MeasureName(highlight.Measure),
                FormatValue(highlight.Measure, highlight.Before),
                FormatValue(highlight.Measure, highlight.Value));
        }
    }
}
