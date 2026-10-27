// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Overlays.Rankings;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// The players who gained the most ranked score this week or month,
    /// on servers that rank plays by osu!stable score (see <see cref="EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public class GetGainedRankingsRequest : GetRankingsRequest<GetGainedRankingsResponse>
    {
        public readonly GainedRankingsPeriod Period;

        /// <summary>
        /// Everyone, or the local user and their friends.
        /// </summary>
        public readonly RankingsSortCriteria Filter;

        public GetGainedRankingsRequest(RulesetInfo ruleset, GainedRankingsPeriod period, RankingsSortCriteria filter = RankingsSortCriteria.All, int page = 1)
            : base(ruleset, page)
        {
            Period = period;
            Filter = filter;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter("period", Period == GainedRankingsPeriod.Month ? "month" : "week");
            req.AddParameter("filter", Filter == RankingsSortCriteria.Friends ? "friends" : "all");

            return req;
        }

        protected override string TargetPostfix() => "gained";
    }

    public enum GainedRankingsPeriod
    {
        /// <summary>
        /// Since Monday 00:00 UTC.
        /// </summary>
        [LocalisableDescription(typeof(GainedRankingsStrings), nameof(GainedRankingsStrings.ThisWeek))]
        Week,

        /// <summary>
        /// Since the 1st of the month, 00:00 UTC.
        /// </summary>
        [LocalisableDescription(typeof(GainedRankingsStrings), nameof(GainedRankingsStrings.ThisMonth))]
        Month,
    }
}
