// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.UserInterface;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The graphs the hub's tiles have behind their text, bottom-aligned and faint.
    /// </summary>
    public static class HubGraphs
    {
        /// <summary>
        /// A line with a soft gradient under it. Higher values are drawn higher.
        /// </summary>
        public static Drawable Area(IEnumerable<float> values, Color4 colour) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Height = 0.7f,
                    Colour = ColourInfo.GradientVertical(colour.Opacity(0), colour.Opacity(0.16f)),
                },
                new LineGraph
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Height = 0.7f,
                    LineColour = colour.Opacity(0.7f),
                    Values = values,
                },
            },
        };

        /// <summary>
        /// One bar per value, the highest reaching up to 70% of the height.
        /// </summary>
        public static Drawable Bars(IReadOnlyList<float> values, Color4 colour)
        {
            var container = new Container { RelativeSizeAxes = Axes.Both };

            if (values.Count == 0)
                return container;

            float max = values.Max();

            if (max <= 0)
                return container;

            for (int i = 0; i < values.Count; i++)
            {
                container.Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.X,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    X = (float)i / values.Count,
                    Width = 0.78f / values.Count,
                    Height = 0.7f * values[i] / max,
                    Colour = colour.Opacity(0.22f),
                });
            }

            return container;
        }
    }
}
