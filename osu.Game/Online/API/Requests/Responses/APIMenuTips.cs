// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    public class APIMenuTips
    {
        /// <summary>
        /// The server's own tips, shown in the main menu alongside the built-in ones.
        /// </summary>
        [JsonProperty(@"tips")]
        public string[] Tips { get; init; } = Array.Empty<string>();
    }
}
