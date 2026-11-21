// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;

namespace osu.Game.Online.Rooms
{
    public abstract class SubmitScoreRequest : APIRequest<MultiplayerScore>
    {
        public readonly SoloScoreInfo Score;

        protected readonly long ScoreId;

        protected SubmitScoreRequest(ScoreInfo scoreInfo, long scoreId)
        {
            Score = SoloScoreInfo.ForSubmission(scoreInfo);
            ScoreId = scoreId;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.ContentType = "application/json";
            req.Method = HttpMethod.Put;
            req.Timeout = 30000;

            string body = SerializeScore();

            // scosu: release builds sign the body (see ScoreSignature); a late retry sends the same body, so it is signed the same.
            string? signature = ScoreSignature.Sign(Target, body);
            if (signature != null)
                req.AddHeader(ScoreSignature.HEADER, signature);

            req.AddRaw(Encoding.UTF8.GetBytes(body));

            return req;
        }

        /// <summary>
        /// The request's body. scosu: also saved by <see cref="Solo.ScoreSubmissionQueue"/>, so a retry sends exactly the same.
        /// </summary>
        public string SerializeScore() => JsonConvert.SerializeObject(Score, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }
}
