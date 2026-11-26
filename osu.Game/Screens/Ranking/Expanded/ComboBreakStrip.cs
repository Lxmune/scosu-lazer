// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Expanded
{
    /// <summary>
    /// scosu: where a play's misses (red) and slider breaks (amber) fell over the beatmap's length.
    /// Hovering a mark tells when it was and the combo it reset. Needs the play's judgements (<see cref="ScoreInfo.HitEvents"/>).
    /// </summary>
    public partial class ComboBreakStrip : CompositeDrawable
    {
        private const float mark_height = 10;
        private const float track_height = 4;

        /// <summary>
        /// At most about this many marks are drawn; nearby breaks share one.
        /// </summary>
        private const int max_marks = 40;

        private readonly ScoreInfo score;
        private readonly ComboBreakAnalysis analysis;

        /// <summary>
        /// How long the played part takes to sweep across the track when the strip appears.
        /// </summary>
        private const double sweep_duration = 800;

        private Container<Mark> marks = null!;
        private Drawable played = null!;
        private float playedWidth;

        public ComboBreakStrip(ScoreInfo score, ComboBreakAnalysis analysis)
        {
            this.score = score;
            this.analysis = analysis;

            RelativeSizeAxes = Axes.X;
            Height = 16;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            int rulesetId = score.Ruleset.OnlineID;

            // Over the whole beatmap, so a failed play's breaks sit where they were in it.
            double start = analysis.StartTime;
            double end = Math.Max(analysis.EndTime, start + (score.BeatmapInfo?.Length ?? 0));
            double length = Math.Max(1, end - start);
            playedWidth = (float)Math.Clamp((analysis.EndTime - start) / length, 0, 1);

            InternalChildren = new Drawable[]
            {
                // A rail like the statistic headers under it...
                new Circle
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.X,
                    Height = track_height,
                    Colour = Color4Extensions.FromHex(@"222"),
                },
                // ...lit over what was played (a failed play stops before the end).
                played = new Circle
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.X,
                    // Swept to its width by Appear (which the panel always calls).
                    Width = 0,
                    Height = track_height,
                    Colour = Color4.White,
                    // Light enough to read over the rail and the panel alike.
                    Alpha = 0.3f,
                },
                marks = new Container<Mark>
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };

            // Nearby breaks share a mark: hundreds of them made long plays crawl.
            foreach (var group in analysis.GroupBreaks(max_marks))
            {
                var comboBreak = group.Shown;

                marks.Add(new Mark(ComboBreakDisplay.Tooltip(comboBreak, rulesetId, costsKnown: false, canWatch: false, nearby: group.Count - 1),
                    ComboBreakDisplay.KindColour(comboBreak, rulesetId, colours))
                {
                    X = (float)Math.Clamp((comboBreak.Time - start) / length, 0, 1),
                });
            }
        }

        /// <summary>
        /// Plays the strip back: the played part sweeps along the track, and each break drops in as the sweep reaches it.
        /// </summary>
        public void Appear()
        {
            // Linear, so the sweep reaches each mark when that mark appears.
            played.ResizeWidthTo(playedWidth, sweep_duration * playedWidth);

            foreach (var mark in marks)
            {
                using (BeginDelayedSequence(sweep_duration * mark.X))
                    mark.Appear();
            }
        }

        private partial class Mark : CompositeDrawable, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            private readonly Circle line;

            public Mark(LocalisableString tooltip, Color4 colour)
            {
                TooltipText = tooltip;

                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;
                // Wider than it looks, to be easy to point at.
                Width = 8;
                Origin = Anchor.TopCentre;
                Alpha = 0;

                InternalChild = line = new Circle
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 3,
                    Height = mark_height,
                    Colour = colour,
                };
            }

            public void Appear()
            {
                this.FadeIn(250, Easing.OutQuint);
                line.ResizeHeightTo(0).ResizeHeightTo(mark_height, 400, Easing.OutQuint);
            }

            protected override bool OnHover(HoverEvent e)
            {
                line.ResizeTo(new osuTK.Vector2(4, mark_height + 4), 100, Easing.OutQuint);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                line.ResizeTo(new osuTK.Vector2(3, mark_height), 200, Easing.OutQuint);
            }
        }
    }
}
