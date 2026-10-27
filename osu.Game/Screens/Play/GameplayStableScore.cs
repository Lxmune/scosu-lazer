// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Bindables;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// The osu!stable score of a play in progress, on servers that rank plays by it (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// Until such a server sends the beatmap's <see cref="StableScoreAttributes"/> (or, for a play being watched, until the player's client
    /// sends its score), this is the play's own score.
    /// </summary>
    public class GameplayStableScore
    {
        /// <summary>
        /// The current total of the play.
        /// </summary>
        public readonly BindableLong TotalScore = new BindableLong();

        private readonly ScoreProcessor scoreProcessor;
        private readonly int rulesetId;
        private readonly APIMod[] mods;

        private readonly StringBuilder judgementOrder = new StringBuilder();

        private StableScoreAttributes? attributes;
        private IBindable<long>? spectatedScore;

        public GameplayStableScore(ScoreProcessor scoreProcessor, Ruleset ruleset, IEnumerable<Mod> mods)
        {
            this.scoreProcessor = scoreProcessor;
            rulesetId = ruleset.RulesetInfo.OnlineID;
            this.mods = mods.Select(m => new APIMod(m)).ToArray();

            // Judgements are followed below, once the order includes them.
            scoreProcessor.TotalScore.BindValueChanged(_ =>
            {
                if (attributes == null)
                    update();
            }, true);
            scoreProcessor.NewJudgement += onNewJudgement;
            scoreProcessor.JudgementReverted += onJudgementReverted;
        }

        /// <summary>
        /// The order of the play's judgements that change the combo, which the server needs to compute the stable score exactly.
        /// </summary>
        public string JudgementOrder => judgementOrder.ToString();

        private void onNewJudgement(JudgementResult result)
        {
            if (countsTowardsScore(result) && StableScoreCalculator.EncodeJudgement(result.Type) is char code)
                judgementOrder.Append(code);

            // Misses can change the stable score without changing the play's own.
            update();
        }

        private void onJudgementReverted(JudgementResult result)
        {
            // Judgements are reverted latest first.
            if (countsTowardsScore(result) && StableScoreCalculator.EncodeJudgement(result.Type) != null)
                judgementOrder.Length--;

            update();
        }

        /// <summary>
        /// Whether the score processor counted <paramref name="result"/> (it doesn't count the ones after a fail).
        /// </summary>
        private bool countsTowardsScore(JudgementResult result) => !result.FailedAtJudgement || scoreProcessor.ApplyNewJudgementsWhenFailed;

        /// <summary>
        /// The beatmap's attributes, sent by the server with the score token. Must be set on the update thread.
        /// </summary>
        public StableScoreAttributes? Attributes
        {
            get => attributes;
            set
            {
                attributes = value;
                update();
            }
        }

        /// <summary>
        /// For a play being watched as it happens: follows the stable score the player's own client computes and sends.
        /// </summary>
        public void FollowSpectatedPlayer(IBindable<long> score)
        {
            spectatedScore = score.GetBoundCopy();
            spectatedScore.BindValueChanged(_ => update(), true);
        }

        /// <summary>
        /// Whether <see cref="TotalScore"/> is the stable score yet (rather than the play's own score).
        /// </summary>
        public bool IsKnown => attributes != null || spectatedScore != null;

        /// <summary>
        /// The score to display: the stable score once it is known, whatever <paramref name="mode"/> is.
        /// </summary>
        public long GetDisplayScore(ScoringMode mode) => IsKnown ? TotalScore.Value : scoreProcessor.GetDisplayScore(mode);

        private void update()
        {
            if (spectatedScore != null)
            {
                TotalScore.Value = spectatedScore.Value;
                return;
            }

            TotalScore.Value = attributes == null
                ? scoreProcessor.TotalScore.Value
                : StableScoreCalculator.Calculate(attributes, rulesetId, scoreProcessor.Statistics, scoreProcessor.MaximumStatistics, scoreProcessor.HighestCombo.Value, mods,
                    judgementOrder.ToString());
        }
    }
}
