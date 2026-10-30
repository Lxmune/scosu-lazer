// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Gets the scosu! event currently shown, if any (a scosu server endpoint).
    /// </summary>
    public class GetScosuEventRequest : APIRequest<GetScosuEventRequest.CurrentEvent>
    {
        protected override string Target => @"event";

        public class CurrentEvent
        {
            /// <summary>
            /// Null when there is no event.
            /// </summary>
            [JsonProperty("event")]
            public APIScosuEvent? Event { get; set; }
        }
    }
}
