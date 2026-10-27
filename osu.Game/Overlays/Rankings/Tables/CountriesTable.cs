// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System;
using osu.Game.Users;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Rankings.Tables
{
    public partial class CountriesTable : RankingsTable<CountryStatistics>
    {
        /// <summary>
        /// Whether countries are ranked by ranked score, without performance points (on servers ranking by osu!stable score).
        /// </summary>
        private readonly bool rankedByScore;

        public CountriesTable(int page, IReadOnlyList<CountryStatistics> rankings, bool rankedByScore = false)
            : base(page, rankings)
        {
            this.rankedByScore = rankedByScore;
        }

        protected override RankingsTableColumn[] CreateAdditionalHeaders()
        {
            var headers = new List<RankingsTableColumn>
            {
                new RankingsTableColumn(RankingsStrings.StatActiveUsers, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
                new RankingsTableColumn(RankingsStrings.StatPlayCount, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
                new RankingsTableColumn(RankingsStrings.StatRankedScore, Anchor.Centre, new Dimension(GridSizeMode.AutoSize), rankedByScore),
                new RankingsTableColumn(RankingsStrings.StatAverageScore, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)),
            };

            if (!rankedByScore)
            {
                headers.Add(new RankingsTableColumn(RankingsStrings.StatPerformance, Anchor.Centre, new Dimension(GridSizeMode.AutoSize), true));
                headers.Add(new RankingsTableColumn(RankingsStrings.StatAveragePerformance, Anchor.Centre, new Dimension(GridSizeMode.AutoSize)));
            }

            return headers.ToArray();
        }

        protected override CountryCode GetCountryCode(CountryStatistics item) => item.Code;

        protected override Drawable[] CreateFlagContent(CountryStatistics item) => [new CountryName(item.Code)];

        protected override Drawable[] CreateAdditionalContent(CountryStatistics item)
        {
            var content = new List<Drawable>
            {
                new ColouredRowText
                {
                    Text = item.ActiveUsers.ToLocalisableString(@"N0")
                },
                new ColouredRowText
                {
                    Text = item.PlayCount.ToLocalisableString(@"N0")
                },
                rankedByScore
                    ? new RowText { Text = item.RankedScore.ToLocalisableString(@"N0") }
                    : new ColouredRowText { Text = item.RankedScore.ToLocalisableString(@"N0") },
                new ColouredRowText
                {
                    Text = (item.RankedScore / Math.Max(item.ActiveUsers, 1)).ToLocalisableString(@"N0")
                },
            };

            if (!rankedByScore)
            {
                content.Add(new RowText
                {
                    Text = item.Performance.ToLocalisableString(@"N0")
                });
                content.Add(new ColouredRowText
                {
                    Text = (item.Performance / Math.Max(item.ActiveUsers, 1)).ToLocalisableString(@"N0")
                });
            }

            return content.ToArray();
        }

        private partial class CountryName : LinkFlowContainer
        {
            [Resolved]
            private RankingsOverlay? rankings { get; set; }

            public CountryName(CountryCode countryCode)
                : base(t => t.Font = OsuFont.GetFont(size: 12))
            {
                AutoSizeAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;
                TextAnchor = Anchor.CentreLeft;

                if (countryCode != CountryCode.Unknown)
                    AddLink(countryCode.GetDescription(), () => rankings?.ShowCountry(countryCode));
            }
        }
    }
}
