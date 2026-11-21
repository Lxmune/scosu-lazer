// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using osu.Framework.IO.Network;
using osu.Game.Online.API;

namespace osu.Game.Online.Solo
{
    /// <summary>
    /// scosu: sends the replay of a score whose submission had to be retried. The spectator server
    /// only keeps a play's frames for a short while, so a late score would otherwise have no replay.
    /// </summary>
    public class UploadReplayRequest : APIRequest
    {
        public readonly long ScoreId;

        private readonly string framesJson;

        public UploadReplayRequest(long scoreId, string framesJson)
        {
            ScoreId = scoreId;
            this.framesJson = framesJson;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.ContentType = "application/json";
            req.Method = HttpMethod.Put;
            req.Timeout = 60000;
            req.AddRaw(framesJson);

            return req;
        }

        protected override string Target => $@"scores/{ScoreId}/replay";
    }
}
