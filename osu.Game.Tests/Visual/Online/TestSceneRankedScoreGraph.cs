// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Header.Components;
using osuTK;

namespace osu.Game.Tests.Visual.Online
{
    [TestFixture]
    public partial class TestSceneRankedScoreGraph : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Pink);

        private RankedScoreGraph graph = null!;

        private const int history_length = 90;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create graph", () => Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300, 150),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(0.2f)
                    },
                    graph = new RankedScoreGraph
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                }
            });
        }

        [Test]
        public void TestNoHistory()
        {
            AddStep("no history", () => graph.History.Value = null);
            AddUntilStep("line graph hidden", () => this.ChildrenOfType<LineGraph>().All(g => g.Alpha == 0));
        }

        [Test]
        public void TestNeverHadRankedScore()
        {
            AddStep("only zeros", () => graph.History.Value = new APIRankedScoreHistory { Mode = @"osu", Data = new long[history_length] });
            AddUntilStep("line graph hidden", () => this.ChildrenOfType<LineGraph>().All(g => g.Alpha == 0));
        }

        [Test]
        public void TestRisingRankedScore()
        {
            // Billions, like a long-time player's ranked score.
            long[] data = Enumerable.Range(0, history_length).Select(day => 2_000_000_000L + day * 25_000_000L).ToArray();

            AddStep("rising history", () => graph.History.Value = new APIRankedScoreHistory { Mode = @"osu", Data = data });
            AddUntilStep("line graph shown", () => this.ChildrenOfType<LineGraph>().All(g => g.Alpha == 1));
            AddUntilStep("gain bars shown", () => this.ChildrenOfType<BarGraph>().Single().Alpha, () => Is.EqualTo(1));
            AddAssert("each day gained its difference", () => RankedScoreGraph.GainOn(data, 10), () => Is.EqualTo(25_000_000));
            AddAssert("nothing known before the first day", () => RankedScoreGraph.GainOn(data, 0), () => Is.Zero);

            AddStep("back to no history", () => graph.History.Value = null);
            AddUntilStep("line graph hidden", () => this.ChildrenOfType<LineGraph>().All(g => g.Alpha == 0));
            AddUntilStep("gain bars hidden", () => this.ChildrenOfType<BarGraph>().Single().Alpha, () => Is.Zero);
        }

        [Test]
        public void TestFirstRankedScoreRecently()
        {
            long[] data = new long[history_length];
            for (int day = history_length - 5; day < history_length; day++)
                data[day] = (day - history_length + 6) * 150_000L;

            AddStep("ranked score in the last days only", () => graph.History.Value = new APIRankedScoreHistory { Mode = @"osu", Data = data });
            AddUntilStep("line graph shown", () => this.ChildrenOfType<LineGraph>().All(g => g.Alpha == 1));
        }
    }
}
