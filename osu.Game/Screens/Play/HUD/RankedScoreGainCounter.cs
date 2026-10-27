// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Play.HUD
{
    /// <summary>
    /// On servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>):
    /// the ranked score the local user would gain by finishing the play as it is, which is how far its stable score is above their best on the beatmap.
    /// </summary>
    public partial class RankedScoreGainCounter : RollingCounter<long>, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        protected override double RollingDuration => 500;

        protected override Easing RollingEasing => Easing.OutQuint;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private GameplayState? gameplayState { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private IBindable<long>? stableScore;

        /// <summary>
        /// The stable score of the local user's best on the beatmap (0 without one), once it is known.
        /// </summary>
        private long? best;

        private GetUserBestStableScoreRequest? bestRequest;

        [BackgroundDependencyLoader]
        private void load()
        {
            Colour = colours.Gray9;

            if (gameplayState == null || !givesRankedScore(gameplayState))
                return;

            var leaderboard = gameplayState.Ruleset.RulesetInfo.GetLeaderboardRuleset(gameplayState.Mods.Select(m => m.Acronym));

            bestRequest = new GetUserBestStableScoreRequest(gameplayState.Beatmap.BeatmapInfo, leaderboard);
            bestRequest.Success += _ => Schedule(() =>
            {
                best = bestRequest.BestStableScore ?? 0;
                updateGain();
            });
            api.Queue(bestRequest);

            stableScore = gameplayState.StableScore.TotalScore.GetBoundCopy();
            stableScore.BindValueChanged(_ => updateGain());
        }

        /// <summary>
        /// Whether finishing this play can add to the local user's ranked score.
        /// </summary>
        private bool givesRankedScore(GameplayState state)
        {
            var beatmap = state.Beatmap.BeatmapInfo;

            return api.Endpoints.RanksByStableScore
                   && api.IsLoggedIn
                   && state.Score.ScoreInfo.User.OnlineID == api.LocalUser.Value.OnlineID
                   && state.Ruleset.RulesetInfo.IsLegacyRuleset()
                   && beatmap.OnlineID > 0
                   && StableScoreCalculator.GivesRankedScore(beatmap.Status)
                   && StableScoreCalculator.GetModEffects(state.Ruleset, beatmap, state.Mods).Ranked;
        }

        private void updateGain()
        {
            // Until the server sends the beatmap's attributes, the play's score isn't its stable score.
            if (best is not long bestScore || gameplayState?.StableScore.IsKnown != true)
                return;

            Current.Value = Math.Max(0, gameplayState.StableScore.TotalScore.Value - bestScore);
            this.FadeColour(Current.Value > 0 ? colours.Lime1 : colours.Gray9, 200, Easing.OutQuint);
        }

        protected override LocalisableString FormatCount(long count) => LocalisableString.Interpolate($@"+{count.ToLocalisableString(@"N0")}");

        protected override IHasText CreateText() => new TextComponent();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            bestRequest?.Cancel();
        }

        private partial class TextComponent : CompositeDrawable, IHasText
        {
            public LocalisableString Text
            {
                get => text.Text;
                set => text.Text = value;
            }

            private readonly OsuSpriteText text;

            public TextComponent()
            {
                AutoSizeAxes = Axes.Both;

                InternalChild = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(4),
                    Children = new Drawable[]
                    {
                        text = new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Font = OsuFont.Numeric.With(size: 20),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Font = OsuFont.Numeric.With(size: 8),
                            Text = StableScoreStrings.RankedScoreGainLabel,
                            Padding = new MarginPadding { Bottom = 3f },
                        },
                    }
                };
            }
        }
    }
}
