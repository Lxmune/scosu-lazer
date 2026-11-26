// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: the three breaks that cost the play the most osu!stable score, each with a button to watch the replay from it.
    /// Until the costs are known (and on servers that don't rank by osu!stable score), the breaks that reset the longest combos.
    /// </summary>
    public partial class CostliestBreaks : CompositeDrawable
    {
        private const int shown = 3;

        private readonly ScoreInfo score;
        private readonly ComboBreakAnalysis analysis;
        private readonly Task costs;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private FillFlowContainer rows = null!;

        public CostliestBreaks(ScoreInfo score, ComboBreakAnalysis analysis, Task costs)
        {
            this.score = score;
            this.analysis = analysis;
            this.costs = costs;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = rows = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
            };

            if (analysis.Breaks.Count == 0)
            {
                rows.Add(new OsuSpriteText
                {
                    Text = ScosuOutcomeStrings.NothingBroke,
                    Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.Bold),
                    Colour = colours.Lime1,
                });
                return;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            show(false);

            costs.ContinueWith(_ => Schedule(() =>
            {
                if (analysis.HasCosts)
                    show(true);
            }), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void show(bool withCosts)
        {
            if (analysis.Breaks.Count == 0)
                return;

            IEnumerable<ComboBreakAnalysis.ComboBreak> ordered = withCosts
                ? analysis.Breaks.OrderByDescending(b => b.Cost ?? 0).ThenByDescending(b => b.ComboLost)
                : analysis.Breaks.OrderByDescending(b => b.ComboLost);

            rows.Clear();

            int place = 1;

            foreach (var comboBreak in ordered.Take(shown))
                rows.Add(new BreakRow(place++, comboBreak, score.Ruleset.OnlineID, withCosts, () => ComboBreakDisplay.Watch(game, score, comboBreak)));

            // Again once the costs are known: the order may change.
            ScosuStatisticRow.Cascade(rows);
        }

        private partial class BreakRow : CompositeDrawable
        {
            private readonly int place;
            private readonly ComboBreakAnalysis.ComboBreak comboBreak;
            private readonly int rulesetId;
            private readonly bool withCost;
            private readonly Action watch;

            public BreakRow(int place, ComboBreakAnalysis.ComboBreak comboBreak, int rulesetId, bool withCost, Action watch)
            {
                this.place = place;
                this.comboBreak = comboBreak;
                this.rulesetId = rulesetId;
                this.withCost = withCost;
                this.watch = watch;

                RelativeSizeAxes = Axes.X;
                Height = 30;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                var kindColour = ComboBreakDisplay.KindColour(comboBreak, rulesetId, colours);

                InternalChildren = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new CircularContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(20),
                                Masking = true,
                                BorderThickness = 1.5f,
                                BorderColour = kindColour,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = kindColour, Alpha = 0.15f },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = place.ToLocalisableString(),
                                        Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                                        Colour = kindColour,
                                    },
                                }
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = LocalisableString.Interpolate($@"{ComboBreakDisplay.FormatTime(comboBreak.Time)} · {ComboBreakDisplay.KindName(comboBreak, rulesetId)}"),
                                        Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.Bold),
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = ComboBreakDisplay.ComboText(comboBreak),
                                        Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE - 2),
                                        Colour = colours.GrayA,
                                    },
                                }
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new CostText(withCost && comboBreak.Cost is long cost
                                ? LocalisableString.Interpolate($@"−{cost.ToLocalisableString(@"N0")}")
                                : ScosuOutcomeStrings.ComboLost(comboBreak.ComboLost.ToLocalisableString(@"N0")), withCost ? ScosuOutcomeStrings.CostTooltip : ScosuOutcomeStrings.ComboLostTooltip)
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Colour = withCost ? colours.Red1 : colours.GrayA,
                            },
                            new WatchButton
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Action = watch,
                                TooltipText = ScosuOutcomeStrings.WatchFrom(ComboBreakDisplay.FormatTime(ComboBreakDisplay.WatchTime(comboBreak))),
                            },
                        }
                    },
                };
            }
        }

        private partial class CostText : OsuSpriteText, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public CostText(LocalisableString text, LocalisableString tooltip)
            {
                Text = text;
                TooltipText = tooltip;
                Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.Bold);
            }
        }

        private partial class WatchButton : OsuAnimatedButton
        {
            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Size = new Vector2(68, 22);

                AddRange(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Gray4,
                        Depth = float.MaxValue,
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Children = new Drawable[]
                        {
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(8),
                                Icon = FontAwesome.Solid.Play,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = ScosuOutcomeStrings.Watch,
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                            },
                        }
                    },
                });
            }
        }
    }
}
