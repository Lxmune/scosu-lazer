// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;

namespace osu.Game.Tests.Visual.Ranking
{
    /// <summary>
    /// scosu: a score's replay played back off-screen gives the judgements the play had.
    /// </summary>
    public partial class TestSceneReplaySimulator : OsuTestScene
    {
        private List<HitEvent>? hitEvents;
        private IBeatmap beatmap = null!;
        private readonly Stopwatch stopwatch = new Stopwatch();

        [Test]
        public void TestAutoplayIsJudgedPerfect()
        {
            simulate(b => new OsuModAutoplay().CreateReplayData(b, Array.Empty<Mod>()).Replay, true);

            AddAssert("every object judged", () => hitEvents!.Count(e => beatmap.HitObjects.Contains(e.HitObject)), () => Is.EqualTo(beatmap.HitObjects.Count));
            AddAssert("no misses", () => hitEvents!.Count(e => e.Result.BreaksCombo()), () => Is.Zero);
            AddAssert("circles hit on time", () => hitEvents!.Where(e => e.HitObject.GetType() == typeof(HitCircle)).All(e => e.Result == HitResult.Great && Math.Abs(e.TimeOffset) < 1));
        }

        /// <summary>
        /// Relax sets itself up when a <see cref="Screens.Play.Player"/> starts, and there is none here (it crashed the game).
        /// </summary>
        [Test]
        public void TestRelaxScoreIsReplayed()
        {
            simulate(b => new OsuModAutoplay().CreateReplayData(b, Array.Empty<Mod>()).Replay, true, new OsuModRelax());

            AddAssert("every object judged", () => hitEvents!.Count(e => beatmap.HitObjects.Contains(e.HitObject)), () => Is.EqualTo(beatmap.HitObjects.Count));
        }

        [Test]
        public void TestIdleReplayMissesEverything()
        {
            simulate(_ => new Replay { Frames = { new OsuReplayFrame(0, new osuTK.Vector2(256, 192)) } }, true);

            AddAssert("every object judged", () => hitEvents!.Count(e => beatmap.HitObjects.Contains(e.HitObject)), () => Is.EqualTo(beatmap.HitObjects.Count));
            AddAssert("nothing hit", () => hitEvents!.Where(e => e.Result.IsHit()), () => Is.Empty);
        }

        [Test]
        public void TestFailedPlayStopsWhereItsReplayEnds()
        {
            double endTime = 0;

            simulate(b =>
            {
                var replay = new OsuModAutoplay().CreateReplayData(b, Array.Empty<Mod>()).Replay;
                endTime = b.HitObjects[b.HitObjects.Count / 2].StartTime;
                replay.Frames.RemoveAll(f => f.Time > endTime);
                return replay;
            }, false);

            AddAssert("no judgements past the replay", () => hitEvents!.Where(e => e.HitObject.GetEndTime() + e.TimeOffset > endTime + 50), () => Is.Empty);
            AddAssert("everything before it hit", () => hitEvents!.Where(e => e.HitObject.GetEndTime() < endTime - 1000 && !e.Result.IsHit() && e.Result != HitResult.IgnoreMiss),
                () => Is.Empty);
        }

        private void simulate(Func<IBeatmap, Replay> createReplay, bool passed, params Mod[] mods)
        {
            AddStep("play the replay back", () =>
            {
                hitEvents = null;

                RulesetInfo ruleset = new OsuRuleset().RulesetInfo;
                beatmap = CreateWorkingBeatmap(ruleset).GetPlayableBeatmap(ruleset, Array.Empty<Mod>());

                var score = new Score
                {
                    Replay = createReplay(beatmap),
                    ScoreInfo = new ScoreInfo
                    {
                        Ruleset = ruleset,
                        BeatmapInfo = beatmap.BeatmapInfo,
                        Passed = passed,
                        Mods = mods,
                    },
                };

                var simulator = new ReplaySimulator(score, beatmap);
                simulator.Completed += e =>
                {
                    stopwatch.Stop();
                    hitEvents = e;
                };

                stopwatch.Restart();
                Child = simulator;
            });

            AddUntilStep("played out", () => hitEvents != null);
            AddStep("log how long it took", () => Console.WriteLine($"replayed {beatmap.HitObjects.Count} objects in {stopwatch.ElapsedMilliseconds} ms"));
        }
    }
}
