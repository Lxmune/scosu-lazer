// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online
{
    /// <summary>
    /// Turns the local user's history on a beatmap (see <see cref="APIBeatmapAttempts"/>) into the lines song select shows.
    /// </summary>
    public static class ScosuBeatmapHistoryFormatter
    {
        /// <summary>
        /// "12 plays, 58% passed", with the starts never finished appended when there are any. <see langword="null"/> without plays or unfinished starts.
        /// </summary>
        public static LocalisableString? FormatPlays(APIBeatmapAttempts attempts)
        {
            if (attempts.Plays <= 0 && attempts.Unsubmitted <= 0)
                return null;

            var plays = ScosuResultsStrings.PlaysAndPassRate(attempts.Plays.ToLocalisableString(@"N0"), (attempts.PassRate ?? 0).ToLocalisableString(@"0%"));

            if (attempts.Unsubmitted <= 0)
                return plays;

            return LocalisableString.Interpolate($@"{plays} · {ScosuResultsStrings.Unfinished(attempts.Unsubmitted.ToLocalisableString(@"N0"))}");
        }

        /// <summary>
        /// The closest attempt to a full combo, then the estimated gain for beating the best. <see langword="null"/> when there is neither.
        /// </summary>
        public static LocalisableString? FormatClosest(APIBeatmapAttempts? attempts, APIBeatmapGain? gain)
        {
            LocalisableString? closest = null;

            if (attempts?.Closest is APIBeatmapAttempts.ClosestAttempt attempt)
            {
                closest = attempt.FullCombo || attempt.Misses <= 0 && attempt.BeatmapMaxCombo > 0 && attempt.MaxCombo >= attempt.BeatmapMaxCombo
                    ? ScosuResultsStrings.ClosestAttemptFullCombo
                    : ScosuResultsStrings.ClosestAttempt(attempt.MaxCombo.ToLocalisableString(@"N0"), attempt.BeatmapMaxCombo.ToLocalisableString(@"N0"), attempt.Misses.ToLocalisableString(@"N0"));
            }

            LocalisableString? gainText = gain?.Gain is long amount && amount > 0
                ? ScosuResultsStrings.EstimatedGain(amount.ToLocalisableString(@"N0"))
                : (LocalisableString?)null;

            if (closest is not LocalisableString closestValue)
                return gainText;

            if (gainText is not LocalisableString gainValue)
                return closestValue;

            return LocalisableString.Interpolate($@"{closestValue} · {gainValue}");
        }
    }
}
