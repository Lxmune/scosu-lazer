// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;

namespace osu.Game.Tests.Visual.Online
{
    [HeadlessTest]
    public partial class TestSceneScoreSubmissionQueue : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        [Resolved]
        private Storage storage { get; set; } = null!;

        private ScoreSubmissionQueue queue = null!;

        private readonly List<APIRequest> requests = new List<APIRequest>();

        /// <summary>
        /// How the fake server answers each resubmission, in order. Once used up, it accepts them.
        /// </summary>
        private readonly Queue<Exception> resubmitFailures = new Queue<Exception>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set up queue", () =>
            {
                storage.DeleteDirectory(ScoreSubmissionQueue.DIRECTORY);
                requests.Clear();
                resubmitFailures.Clear();

                dummyAPI.HandleRequest = request =>
                {
                    lock (requests)
                        requests.Add(request);

                    switch (request)
                    {
                        case ResubmitSoloScoreRequest resubmit:
                            if (resubmitFailures.TryDequeue(out var failure))
                                resubmit.TriggerFailure(failure);
                            else
                                resubmit.TriggerSuccess(new MultiplayerScore { ID = 5000 + resubmit.Submission.Token });
                            return true;

                        case UploadReplayRequest upload:
                            upload.TriggerSuccess();
                            return true;
                    }

                    return false;
                };

                Child = queue = new ScoreSubmissionQueue();
            });
        }

        [Test]
        public void TestNetworkFailureIsRetried()
        {
            AddStep("lose connection once", () => resubmitFailures.Enqueue(new HttpRequestException("connection refused")));
            AddStep("save submission", () => queue.Add(createSubmission(1)));

            AddStep("process", () => queue.ProcessAsync());
            AddUntilStep("retry counted", () => queue.GetPending().SingleOrDefault()?.Attempts, () => Is.EqualTo(1));
            AddAssert("not due yet", () => queue.GetPending().Single().NextAttemptAt > DateTimeOffset.Now);

            AddStep("process again", () => queue.ProcessAsync());
            AddWaitStep("wait", 5);
            AddAssert("no early retry", () => resubmissions(), () => Is.EqualTo(1));

            AddStep("back online", () => queue.ProcessAsync(retryNow: true));
            AddUntilStep("submission gone", () => queue.GetPending(), () => Is.Empty);
            AddAssert("submitted twice", () => resubmissions(), () => Is.EqualTo(2));
            AddUntilStep("replay uploaded for the score", () => uploads().SingleOrDefault()?.ScoreId, () => Is.EqualTo(5001));
        }

        [Test]
        public void TestRefusalIsNotRetried()
        {
            AddStep("refuse", () => resubmitFailures.Enqueue(new APIException("score token expired", null, HttpStatusCode.BadRequest)));
            AddStep("save submission", () => queue.Add(createSubmission(2)));

            AddStep("process", () => queue.ProcessAsync());
            AddUntilStep("submission gone", () => queue.GetPending(), () => Is.Empty);
            AddAssert("sent once", () => resubmissions(), () => Is.EqualTo(1));
            AddAssert("no replay upload", () => uploads(), () => Is.Empty);
        }

        [Test]
        public void TestFirstAttemptIsLeftToThePlayer()
        {
            AddStep("save fresh submission", () =>
            {
                var submission = createSubmission(3);
                submission.CreatedAt = DateTimeOffset.Now;
                queue.Add(submission);
            });

            AddStep("process", () => queue.ProcessAsync(retryNow: true));
            AddWaitStep("wait", 5);
            AddAssert("nothing sent", () => resubmissions(), () => Is.EqualTo(0));

            AddStep("player's attempt succeeds", () => queue.Remove(3));
            AddAssert("submission gone", () => queue.GetPending(), () => Is.Empty);
        }

        [Test]
        public void TestOtherPlayersSubmissionsWait()
        {
            AddStep("save another player's submission", () =>
            {
                var submission = createSubmission(4);
                submission.UserId = dummyAPI.LocalUser.Value.OnlineID + 1;
                queue.Add(submission);
            });

            AddStep("process", () => queue.ProcessAsync());
            AddWaitStep("wait", 5);
            AddAssert("nothing sent", () => resubmissions(), () => Is.EqualTo(0));
            AddAssert("still saved", () => queue.GetPending(), () => Has.Count.EqualTo(1));
        }

        [Test]
        public void TestOldSubmissionsAreDropped()
        {
            AddStep("save old submission", () =>
            {
                var submission = createSubmission(5);
                submission.CreatedAt = DateTimeOffset.Now - ScoreSubmissionQueue.GIVE_UP_AFTER - TimeSpan.FromMinutes(1);
                queue.Add(submission);
            });

            AddStep("process", () => queue.ProcessAsync());
            AddUntilStep("submission gone", () => queue.GetPending(), () => Is.Empty);
            AddAssert("nothing sent", () => resubmissions(), () => Is.EqualTo(0));
        }

        [Test]
        public void TestWhichFailuresArePermanent()
        {
            AddAssert("bad request", () => ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.BadRequest)));
            AddAssert("forbidden", () => ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.Forbidden)));
            AddAssert("not found", () => ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.NotFound)));
            AddAssert("unauthorized is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.Unauthorized)));
            AddAssert("rate limit is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.TooManyRequests)));
            AddAssert("server error is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null, HttpStatusCode.BadGateway)));
            AddAssert("no status is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new APIException("x", null)));
            AddAssert("network error is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new HttpRequestException("x")));
            AddAssert("flushed request is not", () => !ScoreSubmissionQueue.IsPermanentFailure(new WebException("x")));
        }

        private int resubmissions()
        {
            lock (requests)
                return requests.OfType<ResubmitSoloScoreRequest>().Count();
        }

        private List<UploadReplayRequest> uploads()
        {
            lock (requests)
                return requests.OfType<UploadReplayRequest>().ToList();
        }

        /// <summary>
        /// A submission whose first attempt is over (made two minutes ago).
        /// </summary>
        private PendingScoreSubmission createSubmission(long token) => new PendingScoreSubmission
        {
            Token = token,
            UserId = dummyAPI.LocalUser.Value.OnlineID,
            BeatmapId = 75,
            BeatmapTitle = "Test map",
            ScoreJson = @"{""total_score"":1000}",
            ReplayJson = @"{""frames"":[[0,256,192,0]]}",
            CreatedAt = DateTimeOffset.Now - TimeSpan.FromMinutes(2),
        };
    }
}
