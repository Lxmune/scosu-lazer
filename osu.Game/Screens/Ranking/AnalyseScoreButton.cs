// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Ranking
{
    /// <summary>
    /// scosu: opens the selected score's statistics, which play its replay back first where it has no judgements yet
    /// (see <see cref="Statistics.ReplayAnalysisView"/>).
    /// </summary>
    public partial class AnalyseScoreButton : GrayButton
    {
        public readonly Bindable<ScoreInfo?> Score = new Bindable<ScoreInfo?>();

        public AnalyseScoreButton()
            : base(FontAwesome.Solid.ChartLine)
        {
            Size = new Vector2(75, 30);
            TooltipText = ScosuOutcomeStrings.Analyse;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Score.BindValueChanged(score =>
            {
                Enabled.Value = score.NewValue != null;
                // A play without its judgements says, before it is clicked, that it'll be a moment.
                TooltipText = score.NewValue?.HitEvents.Count > 0 ? ScosuOutcomeStrings.Analyse : ScosuOutcomeStrings.AnalyseByReplaying;
            }, true);
        }
    }
}
