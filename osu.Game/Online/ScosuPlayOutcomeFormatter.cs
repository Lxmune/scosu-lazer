// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online
{
    /// <summary>
    /// Turns what a submitted play did (<see cref="APIPlayOutcome"/>) into the results screen's words: the status and place in the score panel's top bar,
    /// its ranked score statistic and the standing in the statistics view.
    /// </summary>
    public static class ScosuPlayOutcomeFormatter
    {
        /// <summary>
        /// The play's place on the beatmap's leaderboard.
        /// </summary>
        /// <param name="Rank">The place, from 1.</param>
        /// <param name="PlacesGained">The places this play went up, when it did.</param>
        /// <param name="TookFirst">Whether the play took first place from someone.</param>
        /// <param name="Tooltip">What the place means (and who first place was taken from).</param>
        public readonly record struct Place(long Rank, long? PlacesGained, bool TookFirst, LocalisableString Tooltip);

        /// <summary>
        /// The ranked score a play added.
        /// </summary>
        /// <param name="Amount">The ranked score, 0 when it added none.</param>
        /// <param name="Label">"+1,234,567", or why it added none ("Below your best").</param>
        /// <param name="Tooltip">What it means.</param>
        public readonly record struct Gain(long Amount, LocalisableString Label, LocalisableString Tooltip);

        /// <summary>
        /// What "Counted" or "Not counted" means for this play: the leaderboard it went to when that is one of scosu's own,
        /// or every reason it didn't count, one per line.
        /// </summary>
        public static LocalisableString StatusTooltip(APIPlayOutcome outcome)
        {
            if (!outcome.Counted)
                return join(outcome.NotCounted.Select(Reason), "\n");

            return LeaderboardName(outcome.Mode) is LocalisableString name ? ScosuOutcomeStrings.OwnLeaderboard(name) : default;
        }

        /// <summary>
        /// The play's place on the beatmap, or <see langword="null"/> when it isn't on its leaderboard (it didn't count).
        /// </summary>
        public static Place? MapPlace(APIPlayOutcome outcome)
        {
            if (!outcome.Counted || outcome.MapRank is not long rank)
                return null;

            long? gained = PlacesGained(outcome.MapRankBefore, rank);

            LocalisableString tooltip = outcome.TookFirstFrom is APIPlayOutcome.Player previous
                ? ScosuOutcomeStrings.TookFirstFrom(previous.Username)
                : outcome.MapRankBefore is long before && gained != null
                    ? ScosuOutcomeStrings.MapRankChangeTooltip(formatRank(before))
                    : ScosuOutcomeStrings.MapRankTooltip;

            return new Place(rank, gained, outcome.TookFirstFrom != null, tooltip);
        }

        /// <summary>
        /// The ranked score the play added, or why it added none.
        /// </summary>
        public static Gain RankedScoreGain(APIPlayOutcome outcome)
        {
            if (!outcome.Counted)
                return new Gain(0, ScosuOutcomeStrings.NotCounted, StatusTooltip(outcome));

            if (!outcome.GivesRankedScore)
                return new Gain(0, ScosuOutcomeStrings.NoRankedScoreYet, ScosuOutcomeStrings.QualifiedTooltip);

            if (!outcome.NewBest || outcome.RankedScoreGain <= 0)
                return new Gain(0, ScosuOutcomeStrings.BelowBest, ScosuOutcomeStrings.BelowBestTooltip);

            return new Gain(outcome.RankedScoreGain, LocalisableString.Interpolate($@"+{outcome.RankedScoreGain.ToLocalisableString(@"N0")}"),
                ScosuOutcomeStrings.RankedScoreGainTooltip);
        }

        /// <summary>
        /// "12.9M", "1.66M", "512k": short enough for a legend or a tile (a tooltip has the exact number).
        /// </summary>
        public static LocalisableString Compact(long value)
        {
            double shown = value;
            string suffix = string.Empty;

            if (value >= 1_000_000)
            {
                shown = value / 1_000_000.0;
                suffix = @"M";
            }
            else if (value >= 1_000)
            {
                shown = value / 1_000.0;
                suffix = @"k";
            }

            string format = suffix.Length == 0 || shown >= 100 ? @"0" : shown >= 10 ? @"0.#" : @"0.##";
            return LocalisableString.Interpolate($@"{shown.ToLocalisableString(format)}{suffix}");
        }

        /// <summary>
        /// "PlayerY (+3,477,888)", for a player to pass and the score needed.
        /// </summary>
        public static LocalisableString FormatTarget(APIPlayOutcome.Target target)
            => ScosuOutcomeStrings.PlayerAndGap(target.Username, target.ScoreNeeded.ToLocalisableString(@"N0"));

        /// <summary>
        /// The places a rank went up from <paramref name="before"/>, or <see langword="null"/> when it didn't (or there was no rank before).
        /// </summary>
        public static long? PlacesGained(long? before, long? after)
            => before is long b && after is long a && b > a ? b - a : null;

        /// <summary>
        /// The name of one of scosu's own leaderboards (Relax, Autopilot), or <see langword="null"/> for a ruleset's leaderboard.
        /// </summary>
        public static LocalisableString? LeaderboardName(int mode) => mode switch
        {
            // Matches the server's modes: osu!(4), osu!taiko (6) and osu!catch (7) Relax, osu! Autopilot (5).
            4 or 6 or 7 => ScosuOutcomeStrings.Relax,
            5 => ScosuOutcomeStrings.Autopilot,
            _ => (LocalisableString?)null,
        };

        /// <summary>
        /// Why a play didn't count, as a sentence.
        /// </summary>
        public static LocalisableString Reason(APIPlayOutcome.NotCountedReason reason) => reason.Code switch
        {
            @"failed" => ScosuOutcomeStrings.ReasonFailed,
            @"no_leaderboard" => ScosuOutcomeStrings.ReasonNoLeaderboard(formatStatus(reason.Status)),
            @"unranked_mod" => ScosuOutcomeStrings.ReasonUnrankedMod(reason.Acronym ?? string.Empty),
            @"mod_settings" => ScosuOutcomeStrings.ReasonModSettings(reason.Acronym ?? string.Empty),
            // A reason this client doesn't know yet.
            _ => reason.Code,
        };

        private static LocalisableString formatRank(long rank) => rank.ToLocalisableString(@"N0");

        private static LocalisableString formatStatus(string? status)
        {
            // The server sends the status as osu-web names it ("pending", "wip", "graveyard").
            if (status != null && Enum.TryParse(status, true, out BeatmapOnlineStatus parsed))
                return parsed.GetLocalisableDescription();

            return status ?? string.Empty;
        }

        private static LocalisableString join(IEnumerable<LocalisableString> parts, string separator)
        {
            LocalisableString? joined = null;

            foreach (var part in parts)
                joined = joined is LocalisableString before ? LocalisableString.Interpolate($@"{before}{separator}{part}") : part;

            return joined ?? default;
        }
    }
}
