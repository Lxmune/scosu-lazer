// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Overlays.Dashboard.Friends
{
    /// <summary>
    /// A friend's rank, ranked score and the ranked score they gained today, in the top right corner of their card.
    /// </summary>
    public partial class FriendScoreLine : FillFlowContainer
    {
        private readonly APIFriendScores.Entry score;

        public FriendScoreLine(APIFriendScores.Entry score)
        {
            this.score = score;

            AutoSizeAxes = Axes.Both;
            Direction = FillDirection.Vertical;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            LocalisableString rankedScore = score.RankedScore.ToLocalisableString(@"N0");

            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                    Text = score.GlobalRank is int rank
                        ? LocalisableString.Interpolate($@"#{rank.ToLocalisableString(@"N0")} · {rankedScore}")
                        : rankedScore,
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                    Colour = score.TodayRankedScore > 0 ? colours.Lime1 : colours.Gray9,
                    Text = ScoreSummaryStrings.GainedToday(score.TodayRankedScore.ToLocalisableString(@"N0")),
                },
            };
        }
    }
}
