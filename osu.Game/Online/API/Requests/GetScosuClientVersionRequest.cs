// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Asks whether this build may still submit plays (a scosu server endpoint).
    /// </summary>
    public class GetScosuClientVersionRequest : APIRequest<GetScosuClientVersionRequest.ClientVersion>
    {
        private readonly string versionHash;

        public GetScosuClientVersionRequest(string versionHash)
        {
            this.versionHash = versionHash;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.AddParameter(@"version_hash", versionHash);
            return req;
        }

        protected override string Target => @"client-version";

        public class ClientVersion
        {
            /// <summary>
            /// Whether this build is one of the newest releases: older ones can't submit plays.
            /// </summary>
            [JsonProperty("current")]
            public bool Current { get; set; }
        }
    }
}
