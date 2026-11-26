// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Play.Leaderboards;
using osuTK;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: where a play just submitted put the player, as the server answered the submission (see <see cref="APIPlayOutcome"/>), in tiles:
    /// their place on the beatmap, in the ranking and among their friends, each with who is next. (The ranked score it added is the panel's.)
    /// For a play that didn't count, why not.
    /// </summary>
    public partial class PlayStanding : CompositeDrawable
    {
        private readonly ScoreInfo score;
        private readonly APIPlayOutcome outcome;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private ScosuTileGrid? tiles;
        private GetScoresRequest? friendsRequest;

        public PlayStanding(ScoreInfo score, APIPlayOutcome outcome)
        {
            this.score = score;
            this.outcome = outcome;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (!outcome.Counted)
            {
                InternalChild = createNotCounted();
                return;
            }

            InternalChild = tiles = new ScosuTileGrid(2);

            tiles.AddTile(createMapTile());

            if (outcome.GlobalRank is long globalRank)
                tiles.AddTile(createRankingTile(globalRank));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (tiles == null)
                return;

            ScosuStatisticRow.Cascade(tiles);

            // The friends' tile only means something with the play on the leaderboard.
            if (outcome.MapRank == null || !api.IsLoggedIn || score.BeatmapInfo?.OnlineID > 0 != true)
                return;

            var req = friendsRequest = new GetScoresRequest(score.BeatmapInfo, score.GetLeaderboardRuleset(), BeatmapLeaderboardScope.Friend);
            req.Success += friends => Schedule(() => showFriends(friends));
            api.Queue(req);
        }

        private ScosuStatTile createMapTile()
        {
            if (outcome.MapRank is not long mapRank)
                return new ScosuStatTile(ScosuOutcomeStrings.TileBeatmap, @"—", ScosuOutcomeStrings.NotOnLeaderboard) { Muted = true };

            bool first = mapRank == 1;

            LocalisableString detail = outcome.NextOnMap is APIPlayOutcome.Target next
                ? target(next.Username, next.ScoreNeeded)
                : outcome.TookFirstFrom is APIPlayOutcome.Player previous
                    ? ScosuOutcomeStrings.TookFirstFrom(previous.Username)
                    : default;

            return new ScosuStatTile(ScosuOutcomeStrings.TileBeatmap, rank(mapRank), detail)
            {
                PlacesGained = ScosuPlayOutcomeFormatter.PlacesGained(outcome.MapRankBefore, outcome.MapRank),
                Icon = first ? FontAwesome.Solid.Crown : null,
                ValueColour = first ? colours.Yellow : null,
                TooltipText = withExactTarget(ScosuPlayOutcomeFormatter.MapPlace(outcome)?.Tooltip ?? ScosuOutcomeStrings.MapRankTooltip, outcome.NextOnMap),
            };
        }

        private ScosuStatTile createRankingTile(long globalRank)
        {
            var leaderboard = ScosuPlayOutcomeFormatter.LeaderboardName(outcome.Mode);

            return new ScosuStatTile(leaderboard ?? ScosuOutcomeStrings.TileGlobal, rank(globalRank),
                outcome.NextInRanking is APIPlayOutcome.Target next ? target(next.Username, next.ScoreNeeded) : ScosuOutcomeStrings.TopOfRanking)
            {
                PlacesGained = ScosuPlayOutcomeFormatter.PlacesGained(outcome.GlobalRankBefore, outcome.GlobalRank),
                ValueColour = colours.Blue1,
                TooltipText = withExactTarget(ScosuOutcomeStrings.GlobalRankTooltip, outcome.NextInRanking),
            };
        }

        /// <summary>
        /// The player's place on the beatmap's leaderboard of them and their friends, and the friend just above.
        /// Nothing when no friend has a score there.
        /// </summary>
        private void showFriends(APIScoresCollection friends)
        {
            int ownId = api.LocalUser.Value.Id;
            int index = friends.Scores.FindIndex(s => s.UserID == ownId);
            int count = Math.Max(friends.ScoresCount, friends.Scores.Count);
            int? place = index >= 0 ? index + 1 : friends.UserScore?.Position;

            if (tiles == null || count <= 1 || place is not int friendPlace)
                return;

            LocalisableString detail = ScosuOutcomeStrings.TopAmongFriends;
            LocalisableString tooltip = default;

            if (friendPlace > 1 && index > 0)
            {
                // As the server tells the next player on the beatmap: their score, plus one to pass it.
                var above = friends.Scores[index - 1];
                long gap = Math.Max(1, above.TotalScore - friends.Scores[index].TotalScore + 1);
                string name = above.User?.Username ?? string.Empty;

                detail = target(name, gap);
                tooltip = ScosuOutcomeStrings.PlayerAndGap(name, gap.ToLocalisableString(@"N0"));
            }

            bool first = friendPlace == 1;

            tiles.AddTile(new ScosuStatTile(ScosuOutcomeStrings.TileFriends, rank(friendPlace), detail)
            {
                ValueSuffix = ScosuOutcomeStrings.OfCount(count.ToLocalisableString(@"N0")),
                Icon = first ? FontAwesome.Solid.Crown : null,
                ValueColour = first ? colours.Yellow : null,
                TooltipText = tooltip,
            });

            tiles.Children[^1].FadeInFromZero(300, Easing.OutQuint);
        }

        /// <summary>
        /// Why the play didn't count, in a callout in the miss colour.
        /// </summary>
        private Drawable createNotCounted()
        {
            var reasons = new OsuTextFlowContainer(s => s.Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ParagraphSpacing = 0.4f,
                Colour = colours.GrayC,
            };

            foreach (var reason in outcome.NotCounted)
                reasons.AddParagraph(ScosuPlayOutcomeFormatter.Reason(reason));

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Red1,
                        Alpha = 0.08f,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(12),
                        Spacing = new Vector2(0, 8),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(6, 0),
                                Colour = colours.Red1,
                                Children = new Drawable[]
                                {
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(11),
                                        Icon = FontAwesome.Solid.Times,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = ScosuOutcomeStrings.WhyNotCounted,
                                        Font = OsuFont.GetFont(size: StatisticItem.FONT_SIZE, weight: FontWeight.Bold),
                                    },
                                }
                            },
                            reasons,
                        }
                    },
                }
            };
        }

        private static LocalisableString rank(long value) => LocalisableString.Interpolate($@"#{value.ToLocalisableString(@"N0")}");

        /// <summary>
        /// "PlayerY (+3.48M)": short enough for a tile.
        /// </summary>
        private static LocalisableString target(string username, long scoreNeeded)
            => ScosuOutcomeStrings.PlayerAndGap(username, ScosuPlayOutcomeFormatter.Compact(scoreNeeded));

        /// <summary>
        /// A tile's tooltip, with the exact score needed to pass the next player.
        /// </summary>
        private static LocalisableString withExactTarget(LocalisableString tooltip, APIPlayOutcome.Target? next)
            => next == null ? tooltip : LocalisableString.Interpolate($"{tooltip}\n{ScosuPlayOutcomeFormatter.FormatTarget(next)}");

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            friendsRequest?.Cancel();
        }
    }
}
