// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Online.Solo
{
    /// <summary>
    /// scosu: keeps solo score submissions on disk until the server has them, and retries those that
    /// failed for network reasons (lazer itself submits once and gives up).
    /// </summary>
    /// <remarks>
    /// Every submission is saved before its first attempt, so it survives a crash or the game closing.
    /// Retrying is safe: the server answers a second submission with the same token with the score the
    /// first one saved, and score tokens expire after a day. A rejection that a retry can't fix (a 4xx
    /// from the server, such as a bad token or a restricted account) drops the submission.
    /// </remarks>
    public partial class ScoreSubmissionQueue : Component
    {
        public const string DIRECTORY = @"pending-scores";

        /// <summary>
        /// The player screen makes the first attempt; the queue only steps in once that has had time to finish
        /// (requests time out after 30 seconds).
        /// </summary>
        public static readonly TimeSpan FIRST_ATTEMPT_GRACE = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Past this the server would refuse the token anyway, so the submission is dropped.
        /// </summary>
        public static readonly TimeSpan GIVE_UP_AFTER = TimeSpan.FromHours(48);

        private static readonly TimeSpan max_retry_delay = TimeSpan.FromMinutes(30);

        private const double check_interval = 15000;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        private Storage pending = null!;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        /// <summary>
        /// Guards the files: the player screen and the queue can both touch one.
        /// </summary>
        private readonly object fileLock = new object();

        /// <summary>
        /// One run at a time; a run asked for meanwhile waits its turn, so a reconnection is never missed.
        /// </summary>
        private readonly SemaphoreSlim processing = new SemaphoreSlim(1, 1);

        [BackgroundDependencyLoader]
        private void load()
        {
            pending = storage.GetStorageForDirectory(DIRECTORY);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            apiState.BindTo(api.State);
            apiState.BindValueChanged(state =>
            {
                // Back online: retry now rather than wait out the delay.
                if (state.NewValue == APIState.Online)
                    ProcessAsync(retryNow: true).FireAndForget();
            }, true);

            Scheduler.AddDelayed(() => ProcessAsync().FireAndForget(), check_interval, true);
        }

        /// <summary>
        /// Saves a submission before its first attempt. Returns whether it could be saved.
        /// </summary>
        public bool Add(PendingScoreSubmission submission)
        {
            submission.NextAttemptAt = submission.CreatedAt + FIRST_ATTEMPT_GRACE;

            try
            {
                lock (fileLock)
                    write(submission);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Could not save score submission {submission.Token} for retrying");
                return false;
            }
        }

        /// <summary>
        /// Forgets a submission: the server has it, or refused it for good.
        /// </summary>
        public void Remove(long token)
        {
            try
            {
                lock (fileLock)
                    pending.Delete(fileName(token));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Could not remove saved score submission {token}");
            }
        }

        /// <summary>
        /// The submissions waiting, oldest first.
        /// </summary>
        public IReadOnlyList<PendingScoreSubmission> GetPending()
        {
            var submissions = new List<PendingScoreSubmission>();

            lock (fileLock)
            {
                foreach (string file in pending.GetFiles(string.Empty, @"*.json"))
                {
                    try
                    {
                        using (var stream = pending.GetStream(file))
                        using (var reader = new StreamReader(stream!))
                        {
                            var submission = JsonConvert.DeserializeObject<PendingScoreSubmission>(reader.ReadToEnd());
                            if (submission != null)
                                submissions.Add(submission);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Saved score submission {file} is unreadable and was removed");
                        pending.Delete(file);
                    }
                }
            }

            return submissions.OrderBy(s => s.CreatedAt).ToList();
        }

        /// <summary>
        /// Sends the submissions that are due, one at a time, until one fails for network reasons.
        /// </summary>
        /// <param name="retryNow">Ignore the retry delay of submissions that were already retried.</param>
        public Task ProcessAsync(bool retryNow = false) => Task.Run(async () =>
        {
            await processing.WaitAsync().ConfigureAwait(false);

            try
            {
                foreach (var submission in GetPending())
                {
                    if (api.State.Value != APIState.Online)
                        return;

                    // Another player's play waits for their own login.
                    if (submission.UserId != api.LocalUser.Value.OnlineID)
                        continue;

                    var now = DateTimeOffset.Now;

                    if (now - submission.CreatedAt > GIVE_UP_AFTER)
                    {
                        Remove(submission.Token);
                        notifyFailure(submission, "The play is too old to be submitted.");
                        continue;
                    }

                    bool due = now >= submission.NextAttemptAt || (retryNow && submission.Attempts > 0);
                    if (!due)
                        continue;

                    if (!await attempt(submission).ConfigureAwait(false))
                        return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Retrying score submissions failed");
            }
            finally
            {
                processing.Release();
            }
        });

        /// <summary>
        /// Whether a submission that failed with <paramref name="exception"/> should never be retried: the server
        /// looked at it and refused it. Network problems, timeouts, server errors and logins that need renewing
        /// can all go away.
        /// </summary>
        public static bool IsPermanentFailure(Exception exception)
        {
            if (exception is not APIException { StatusCode: HttpStatusCode status })
                return false;

            int code = (int)status;
            return code >= 400 && code < 500
                                && status != HttpStatusCode.Unauthorized
                                && status != HttpStatusCode.RequestTimeout
                                && status != HttpStatusCode.TooManyRequests;
        }

        /// <summary>
        /// Returns false if the submission failed for network reasons (the queue then stops for now).
        /// </summary>
        private async Task<bool> attempt(PendingScoreSubmission submission)
        {
            Logger.Log($"Retrying score submission (token:{submission.Token}, attempt {submission.Attempts + 1})");

            var request = new ResubmitSoloScoreRequest(submission);
            var result = new TaskCompletionSource<(MultiplayerScore? score, Exception? error)>(TaskCreationOptions.RunContinuationsAsynchronously);
            request.Success += score => result.TrySetResult((score, null));
            request.Failure += e => result.TrySetResult((null, e));
            api.Queue(request);

            var (score, error) = await result.Task.ConfigureAwait(false);

            if (score != null)
            {
                Logger.Log($"Score submission completed on retry! (token:{submission.Token} id:{score.ID})");

                if (submission.ReplayJson != null)
                    await uploadReplay(score.ID, submission.ReplayJson).ConfigureAwait(false);

                Remove(submission.Token);
                Schedule(() => notifications?.Post(new SimpleNotification
                {
                    Text = $"Your play on {submission.BeatmapTitle} was submitted.",
                    Icon = FontAwesome.Solid.Check,
                }));
                return true;
            }

            if (error != null && IsPermanentFailure(error))
            {
                Logger.Log($"Score submission refused on retry (token:{submission.Token}): {error.Message}");
                Remove(submission.Token);
                notifyFailure(submission, error.Message);
                return true;
            }

            submission.Attempts++;
            submission.NextAttemptAt = DateTimeOffset.Now + retryDelay(submission.Attempts);
            Logger.Log($"Score submission retry failed (token:{submission.Token}), next attempt at {submission.NextAttemptAt}: {error?.Message}");

            lock (fileLock)
            {
                // The player screen may have dropped it in the meantime.
                if (pending.Exists(fileName(submission.Token)))
                    write(submission);
            }

            return false;
        }

        /// <summary>
        /// The replay is a bonus: the score stands without it, so a failure here is only logged.
        /// </summary>
        private async Task uploadReplay(long scoreId, string replayJson)
        {
            var request = new UploadReplayRequest(scoreId, replayJson);
            var result = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
            request.Success += () => result.TrySetResult(null);
            request.Failure += e => result.TrySetResult(e);
            api.Queue(request);

            var error = await result.Task.ConfigureAwait(false);

            if (error == null)
                Logger.Log($"Replay uploaded for score {scoreId}");
            else
                Logger.Log($"Replay upload for score {scoreId} failed: {error.Message}");
        }

        /// <summary>
        /// 30 seconds, doubling with each attempt, up to <see cref="max_retry_delay"/>.
        /// </summary>
        private static TimeSpan retryDelay(int attempts) =>
            TimeSpan.FromSeconds(Math.Min(30 * Math.Pow(2, attempts - 1), max_retry_delay.TotalSeconds));

        private void notifyFailure(PendingScoreSubmission submission, string reason) =>
            Schedule(() => notifications?.Post(new ScoreSubmissionFailureNotification($"Play on {submission.BeatmapTitle} not submitted", reason)));

        private void write(PendingScoreSubmission submission)
        {
            using (var stream = pending.CreateFileSafely(fileName(submission.Token)))
            using (var writer = new StreamWriter(stream))
                writer.Write(JsonConvert.SerializeObject(submission));
        }

        private static string fileName(long token) => $@"{token}.json";
    }
}
