// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Expanded.Accuracy;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Ranking.Expanded
{
    /// <summary>
    /// scosu: the expanded score panel's top bar, on either side of the avatar: whether a play just submitted counted (and why not, on hover),
    /// and its place on the beatmap with the places it went up. Other players' scores show their place only.
    /// </summary>
    public partial class ScorePanelStatusBar : CompositeDrawable
    {
        private const float side_margin = 14;

        /// <summary>
        /// The score's place on the leaderboard, for scores without an outcome (other players').
        /// </summary>
        public readonly Bindable<int?> ScorePosition = new Bindable<int?>();

        private readonly ScoreInfo score;
        private readonly bool withFlair;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private Container placeContainer = null!;

        public ScorePanelStatusBar(ScoreInfo score, bool withFlair)
        {
            this.score = score;
            this.withFlair = withFlair;

            RelativeSizeAxes = Axes.X;
            Height = ScorePanel.EXPANDED_TOP_LAYER_HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                placeContainer = new Container
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    AutoSizeAxes = Axes.Both,
                    X = -side_margin,
                },
            };

            if (score.ScosuOutcome is not APIPlayOutcome outcome)
                return;

            AddInternal(new Pill(outcome.Counted ? ScosuOutcomeStrings.Counted.ToUpper() : ScosuOutcomeStrings.NotCounted.ToUpper(),
                outcome.Counted ? FontAwesome.Solid.Check : FontAwesome.Solid.Times,
                outcome.Counted ? Color4.White : colours.Red1,
                outcome.Counted ? colours.Lime1 : colours.Red1,
                ScosuPlayOutcomeFormatter.StatusTooltip(outcome))
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                X = side_margin,
            });

            if (ScosuPlayOutcomeFormatter.MapPlace(outcome) is ScosuPlayOutcomeFormatter.Place place)
                placeContainer.Child = createPlace(place.Rank, place.Tooltip, place.PlacesGained);
        }

        /// <summary>
        /// "#3 ▲4", or a crowned "#1" in gold.
        /// </summary>
        private Pill createPlace(long rank, LocalisableString tooltip, long? gained = null)
        {
            bool first = rank == 1;
            var colour = first ? colours.Yellow : Color4.White;

            return new Pill(LocalisableString.Interpolate($@"#{rank.ToLocalisableString(@"N0")}"), first ? FontAwesome.Solid.Crown : null, colour, colour, tooltip, gained);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Other players' scores: their place on the leaderboard, as the contracted panel shows it.
            if (score.ScosuOutcome == null)
            {
                ScorePosition.BindValueChanged(position => placeContainer.Child = position.NewValue is int rank
                    ? createPlace(rank, default)
                    : Empty(), true);
            }

            // A play just set: once the score has counted up, like the rest of what it did.
            if (withFlair)
            {
                foreach (var pill in InternalChildren)
                {
                    pill.Alpha = 0;

                    using (BeginDelayedSequence(AccuracyCircle.ACCURACY_TRANSFORM_DELAY + AccuracyCircle.ACCURACY_TRANSFORM_DURATION))
                    {
                        pill.MoveToY(6).MoveToY(0, 400, Easing.OutQuint);
                        pill.FadeIn(300, Easing.OutQuint);
                    }
                }
            }
        }

        /// <summary>
        /// A capsule in the style of the panel's statistic headers, so the top bar reads as part of the panel.
        /// </summary>
        private partial class Pill : CompositeDrawable, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public Pill(LocalisableString text, IconUsage? icon, Color4 textColour, Color4 iconColour, LocalisableString tooltip, long? gained = null)
            {
                TooltipText = tooltip;
                AutoSizeAxes = Axes.Both;

                var flow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5, 0),
                    Padding = new MarginPadding { Horizontal = 10, Vertical = 4 },
                };

                if (icon is IconUsage usage)
                {
                    flow.Add(new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(10),
                        Icon = usage,
                        Colour = iconColour,
                    });
                }

                flow.Add(new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = text,
                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                    Colour = textColour,
                });

                if (gained is long places)
                {
                    flow.Add(new GainedPlaces(places)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    });
                }

                InternalChild = new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4Extensions.FromHex(@"222"),
                        },
                        flow,
                    }
                };
            }
        }

        private partial class GainedPlaces : FillFlowContainer
        {
            private readonly long places;

            public GainedPlaces(long places)
            {
                this.places = places;

                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(1, 0);
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Colour = colours.Green1;

                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(8),
                        Icon = FontAwesome.Solid.CaretUp,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = places.ToLocalisableString(@"N0"),
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                    },
                };
            }
        }
    }
}
