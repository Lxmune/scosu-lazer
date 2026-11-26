// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Scoring
{
    /// <summary>
    /// scosu: corrects the judgements of a play replayed on this client with the ones it got when it was played,
    /// as the server keeps them (see <see cref="Online.API.Requests.Responses.SoloScoreInfo.JudgementOrder"/>).
    /// </summary>
    /// <remarks>
    /// A replay keeps the cursor about 60 times a second and its times to the millisecond, so replaying a play judges a few close calls
    /// differently from the play itself. The order of the play's judgements that change the combo has no times, but the replay's have:
    /// together they make the play's judgements exactly, which <see cref="ComboBreakAnalysis"/> needs to score them.
    /// </remarks>
    public static class JudgementOrderMerge
    {
        /// <summary>
        /// <paramref name="replayed"/> with each judgement that changes the combo replaced by the one at its place in <paramref name="order"/>.
        /// </summary>
        /// <param name="replayed">The judgements of the replayed play, in the order they happened.</param>
        /// <param name="order">The play's judgements that change the combo, in the order they happened, by code (see <see cref="StableScoreCalculator.EncodeJudgement"/>).</param>
        /// <param name="outcome">What was done, or why nothing was, for the logs.</param>
        /// <returns>The corrected judgements, or <c>null</c> when <paramref name="order"/> doesn't line up with <paramref name="replayed"/>:
        /// a judgement the object it falls on can't be given, or judgements left over on either side. A slider's end is the one object
        /// that may have none (missing it doesn't change the combo), and is taken as missed then.</returns>
        public static List<HitEvent>? Merge(IReadOnlyList<HitEvent> replayed, string order, out string outcome)
        {
            var merged = replayed.ToList();
            int next = 0;
            int corrected = 0;

            for (int i = 0; i < replayed.Count; i++)
            {
                var hitEvent = replayed[i];
                var judgement = hitEvent.HitObject.Judgement;

                bool canChangeCombo = judgement.MaxResult.AffectsCombo() || judgement.MinResult.AffectsCombo();

                if (!canChangeCombo)
                    continue;

                HitResult? played = next < order.Length ? StableScoreCalculator.DecodeJudgement(order[next]) : null;
                HitResult result;

                if (played is HitResult fitting && fitting.AffectsCombo() && fitting.IsValidHitResult(judgement.MinResult, judgement.MaxResult))
                {
                    result = fitting;
                    next++;
                }
                else if (!judgement.MinResult.AffectsCombo())
                {
                    // An object that leaves nothing in the order when missed (a slider's end): the play missed it.
                    result = judgement.MinResult;
                }
                else
                {
                    outcome = next < order.Length
                        ? $"the play's judgement {next + 1:N0} ('{order[next]}') can't be the replay's {hitEvent.HitObject.GetType().Name}'s"
                        : $"the play has fewer judgements than the replay ({order.Length:N0})";
                    return null;
                }

                if (result == hitEvent.Result)
                    continue;

                // The replay's timing is kept: it's the best known (a miss made a hit has the offset the replay missed it at).
                merged[i] = new HitEvent(hitEvent.TimeOffset, hitEvent.GameplayRate, result, hitEvent.HitObject, hitEvent.LastHitObject, hitEvent.Position);
                corrected++;
            }

            if (next < order.Length)
            {
                outcome = $"the play has more judgements than the replay ({order.Length - next:N0} left over of {order.Length:N0})";
                return null;
            }

            outcome = corrected == 0 ? "the replay judged the play as it was played" : $"corrected {corrected:N0} of the replay's judgements with the play's";
            return merged;
        }
    }
}
