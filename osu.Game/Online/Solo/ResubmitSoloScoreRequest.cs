// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using osu.Framework.IO.Network;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;

namespace osu.Game.Online.Solo
{
    /// <summary>
    /// Sends a saved <see cref="PendingScoreSubmission"/> again, as <see cref="SubmitSoloScoreRequest"/> first did.
    /// The server answers with the saved score if an earlier attempt reached it.
    /// </summary>
    public class ResubmitSoloScoreRequest : APIRequest<MultiplayerScore>
    {
        public readonly PendingScoreSubmission Submission;

        public ResubmitSoloScoreRequest(PendingScoreSubmission submission)
        {
            Submission = submission;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.ContentType = "application/json";
            req.Method = HttpMethod.Put;
            req.Timeout = 30000;
            req.AddRaw(Submission.ScoreJson);

            return req;
        }

        protected override string Target => $@"beatmaps/{Submission.BeatmapId}/solo/scores/{Submission.Token}";
    }
}
