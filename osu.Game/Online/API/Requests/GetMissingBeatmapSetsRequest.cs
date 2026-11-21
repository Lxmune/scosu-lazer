// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// scosu: the ranked beatmap sets holding maps the user hasn't played (or passed, full-combo'd, got S on), the most maximum score first.
    /// The server lists at most 1000; the game leaves out the ones it has.
    /// </summary>
    public class GetMissingBeatmapSetsRequest : ScosuWebAPIRequest<GetMissingBeatmapSetsRequest.Response>
    {
        private readonly long userId;
        private readonly IRulesetInfo ruleset;
        private readonly MissingBeatmapsKind kind;
        private readonly double minStars;

        public GetMissingBeatmapSetsRequest(long userId, IRulesetInfo ruleset, MissingBeatmapsKind kind, double minStars)
        {
            this.userId = userId;
            this.ruleset = ruleset;
            this.kind = kind;
            this.minStars = minStars;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.AddParameter(@"mode", ruleset.ShortName);
            req.AddParameter(@"kind", kind switch
            {
                MissingBeatmapsKind.NotPassed => @"unpassed",
                MissingBeatmapsKind.NoFullCombo => @"no_fc",
                MissingBeatmapsKind.NoSRank => @"no_s",
                _ => @"unplayed",
            });
            req.AddParameter(@"min_stars", minStars.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return req;
        }

        protected override string Target => $@"users/{userId}/progress/missing/sets";

        public new class Response
        {
            [JsonProperty(@"beatmapsets")]
            public List<APIBeatmapSet> BeatmapSets { get; set; } = new List<APIBeatmapSet>();
        }
    }

    public enum MissingBeatmapsKind
    {
        [LocalisableDescription(typeof(ScosuMissingBeatmapsStrings), nameof(ScosuMissingBeatmapsStrings.NotPlayed))]
        NotPlayed,

        [LocalisableDescription(typeof(ScosuMissingBeatmapsStrings), nameof(ScosuMissingBeatmapsStrings.NotPassed))]
        NotPassed,

        [LocalisableDescription(typeof(ScosuMissingBeatmapsStrings), nameof(ScosuMissingBeatmapsStrings.NoFullCombo))]
        NoFullCombo,

        [LocalisableDescription(typeof(ScosuMissingBeatmapsStrings), nameof(ScosuMissingBeatmapsStrings.NoSRank))]
        NoSRank,
    }
}
