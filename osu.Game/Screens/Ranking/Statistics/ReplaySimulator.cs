// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Timing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osu.Game.Screens.Play;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// scosu: plays a score's replay back out of sight, as fast as the game can judge it, to get the judgements (hit events)
    /// a score only has when it was just played or watched.
    /// </summary>
    /// <remarks>
    /// The replay runs through the ruleset's own playfield, as when it is watched, so the judgements are the game's.
    /// The playfield is never drawn and plays no samples. Its frame-stable clock catches up toward the end of the beatmap
    /// for up to 10 ms of each frame, and judges every replay frame on the way.
    /// It must be laid out on screen (not masked away), as the replay's cursor is placed on the playfield.
    /// </remarks>
    public partial class ReplaySimulator : CompositeDrawable, ISamplePlaybackDisabler
    {
        /// <summary>
        /// How long past the beatmap's last object to keep going for judgements still to come, at most.
        /// </summary>
        private const double end_margin = 5000;

        /// <summary>
        /// How long past a failed play's last replay frame a judgement is still the play's: the frame-stable step it failed in.
        /// </summary>
        private const double fail_margin = 1000.0 / 60;

        /// <summary>
        /// How many misses a replay played back here may differ from its score by: at least this many...
        /// </summary>
        private const int misses_allowed = 10;

        /// <summary>
        /// ...or this share of the play's objects.
        /// </summary>
        private const double misses_allowed_share = 0.02;

        /// <summary>
        /// How far into the beatmap the replay is, from 0 to 1.
        /// </summary>
        public IBindable<double> Progress => progress;

        /// <summary>
        /// Invoked once with the play's judgements, when the replay has been played out.
        /// </summary>
        public event Action<List<HitEvent>>? Completed;

        private readonly BindableDouble progress = new BindableDouble();

        private readonly Score replayScore;
        private readonly IBeatmap playableBeatmap;

        private readonly ManualClock referenceTime = new ManualClock { IsRunning = true, Rate = 1 };

        private readonly Ruleset ruleset;
        private readonly Mod[] mods;
        private readonly DrawableRuleset drawableRuleset;
        private readonly ScoreProcessor scoreProcessor;

        private readonly BindableBool samplePlaybackDisabled = new BindableBool(true);

        private readonly double startTime;
        private readonly double lastObjectTime;
        private readonly double lastFrameTime;

        private int framesRun;
        private bool completed;

        /// <param name="replayScore">The score with its replay.</param>
        /// <param name="playableBeatmap">The beatmap the score was set on, converted for its ruleset and mods.</param>
        public ReplaySimulator(Score replayScore, IBeatmap playableBeatmap)
        {
            this.replayScore = replayScore;
            this.playableBeatmap = playableBeatmap;

            RelativeSizeAxes = Axes.Both;

            // Never drawn, but still updated.
            Alpha = 0;
            AlwaysPresent = true;

            ruleset = replayScore.ScoreInfo.Ruleset.CreateInstance();

            // Mods hold state while they are played with (bindables, transforms), so the score's own aren't used.
            mods = replayScore.ScoreInfo.Mods.Select(m => m.DeepClone()).ToArray();

            drawableRuleset = ruleset.CreateDrawableRulesetWith(playableBeatmap, mods);
            drawableRuleset.Clock = new FramedClock(referenceTime);

            scoreProcessor = ruleset.CreateScoreProcessor();
            scoreProcessor.Mods.Value = mods;
            scoreProcessor.ApplyBeatmap(playableBeatmap);

            drawableRuleset.NewResult += scoreProcessor.ApplyResult;
            drawableRuleset.RevertResult += scoreProcessor.RevertResult;
            drawableRuleset.FrameStableComponents.Add(scoreProcessor);

            startTime = drawableRuleset.GameplayStartTime;
            lastObjectTime = playableBeatmap.HitObjects.Count > 0 ? playableBeatmap.HitObjects.Max(h => h.GetEndTime()) : startTime;
            lastFrameTime = replayScore.Replay.Frames.Count > 0 ? replayScore.Replay.Frames.Max(f => f.Time) : startTime;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            dependencies.CacheAs<ISamplePlaybackDisabler>(this);
            dependencies.CacheAs(scoreProcessor);
            dependencies.CacheAs(new GameplayState(playableBeatmap, ruleset, mods, replayScore, scoreProcessor));

            if (drawableRuleset is IDrawableScrollingRuleset scrollingRuleset)
                dependencies.CacheAs(scrollingRuleset.ScrollingInfo);

            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = drawableRuleset;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // As in a replay player: once the playfield is loaded, before it first updates.
            drawableRuleset.SetReplayScore(replayScore);
        }

        protected override void Update()
        {
            base.Update();

            if (completed)
                return;

            // The playfield seeks straight to the first time it is given, and steps frame by frame toward any later one.
            referenceTime.CurrentTime = framesRun++ == 0 ? startTime : lastObjectTime + end_margin;

            // A failed play's replay ends where it failed: what comes after it was never played.
            double playEnd = replayScore.ScoreInfo.Passed ? lastObjectTime : Math.Min(lastObjectTime, lastFrameTime + fail_margin);

            double time = drawableRuleset.FrameStableClock.CurrentTime;
            progress.Value = Math.Clamp((time - startTime) / Math.Max(1, playEnd - startTime), 0, 1);

            if (scoreProcessor.HasCompleted.Value || time >= (replayScore.ScoreInfo.Passed ? lastObjectTime + end_margin : playEnd))
                complete();
        }

        private void complete()
        {
            completed = true;
            progress.Value = 1;

            var hitEvents = scoreProcessor.HitEvents.ToList();

            // The playfield runs ahead of where the replay ended within a frame: what was judged past it was never played.
            if (!replayScore.ScoreInfo.Passed)
                hitEvents.RemoveAll(e => e.HitObject.GetEndTime() + e.TimeOffset > lastFrameTime + fail_margin);
            logDifferences();

            ClearInternal();
            Completed?.Invoke(hitEvents);
        }

        /// <summary>
        /// A replay set on another version of the game can be judged a little differently on this one. Logs where it was.
        /// </summary>
        private void logDifferences()
        {
            var scored = replayScore.ScoreInfo.Statistics;
            var replayed = scoreProcessor.Statistics;

            var differences = scored.Keys.Union(replayed.Keys)
                                    .Where(r => r.IsScorable() && scored.GetValueOrDefault(r) != replayed.GetValueOrDefault(r))
                                    .Select(r => $"{r} {scored.GetValueOrDefault(r)} → {replayed.GetValueOrDefault(r)}")
                                    .ToArray();

            if (differences.Length > 0)
                Logger.Log($"Replaying score {replayScore.ScoreInfo.OnlineID} judged it differently: {string.Join(", ", differences)}");
        }

        /// <summary>
        /// Whether a replay played back here missed about as often as its score says. Past a few misses either way,
        /// the playback went astray (desynced), and anything drawn from it would be made up.
        /// </summary>
        /// <returns>The misses counted by each, or null when they agree (or the score doesn't say).</returns>
        public static (int Scored, int Replayed)? MissesDisagree(ScoreInfo score, IReadOnlyList<HitEvent> hitEvents)
        {
            int judged = score.Statistics.Where(s => s.Key.IsBasic()).Sum(s => s.Value);

            if (judged == 0)
                return null;

            int scored = score.Statistics.GetValueOrDefault(HitResult.Miss);
            int replayed = hitEvents.Count(e => e.Result == HitResult.Miss);

            // Small differences are expected: a play set on another version of the game can be judged a little differently here.
            int allowed = Math.Max(misses_allowed, (int)(judged * misses_allowed_share));

            return Math.Abs(scored - replayed) > allowed ? (scored, replayed) : null;
        }

        IBindable<bool> ISamplePlaybackDisabler.SamplePlaybackDisabled => samplePlaybackDisabled;
    }
}
