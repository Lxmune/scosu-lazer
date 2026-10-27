// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Overlays.Rankings.Tables
{
    /// <summary>
    /// The players who gained the most ranked score this week or month (see <see cref="GainedRankingsLayout"/>).
    /// </summary>
    public partial class GainedTable : RankingsTable<APIGainedRanking>
    {
        public GainedTable(int page, IReadOnlyList<APIGainedRanking> rankings)
            : base(page, rankings)
        {
        }

        protected override Drawable CreateRowBackground(APIGainedRanking item)
        {
            var background = base.CreateRowBackground(item);

            // see: https://github.com/ppy/osu-web/blob/9de00a0b874c56893d98261d558d78d76259d81b/resources/views/multiplayer/rooms/_rankings_table.blade.php#L23
            if (!item.User.Active)
                background.Alpha = 0.5f;

            return background;
        }

        protected override Drawable[] CreateRowContent(int index, APIGainedRanking item)
        {
            var content = base.CreateRowContent(index, item);

            if (!item.User.Active)
            {
                foreach (var d in content)
                    d.Alpha = 0.5f;
            }

            return content;
        }

        protected override RankingsTableColumn[] CreateAdditionalHeaders() => new[]
        {
            new RankingsTableColumn(RankingsStrings.StatPlayCount, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
            new RankingsTableColumn(GainedRankingsStrings.ScoreGained, Anchor.Centre, new Dimension(GridSizeMode.AutoSize), true),
        };

        protected override Drawable[] CreateAdditionalContent(APIGainedRanking item) => new Drawable[]
        {
            new ColouredRowText { Text = item.PlayCount.ToLocalisableString(@"N0") },
            new RowText { Text = item.RankedScore.ToLocalisableString(@"N0") },
        };

        protected override CountryCode GetCountryCode(APIGainedRanking item) => item.User.CountryCode;

        protected override Drawable[] CreateFlagContent(APIGainedRanking item)
        {
            var username = new LinkFlowContainer(t => t.Font = OsuFont.GetFont(size: TEXT_SIZE, italics: true))
            {
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                TextAnchor = Anchor.CentreLeft
            };
            username.AddUserLink(item.User);
            return [new UpdateableTeamFlag(item.User.Team) { Size = new Vector2(40, 20) }, username];
        }
    }
}
