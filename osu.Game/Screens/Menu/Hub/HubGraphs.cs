// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The graphs the hub's tiles have behind their text, bottom-aligned and faint.
    /// </summary>
    public static class HubGraphs
    {
        /// <summary>
        /// An area chart in the lower part of the tile: the area under the line fades out downward, and a glowing dot marks the
        /// latest value. Higher values are drawn higher.
        /// </summary>
        public static Drawable Area(IEnumerable<float> values, Color4 colour)
        {
            const float height = 0.62f;

            // The lowest value still has a sliver of area under it.
            const float floor = 0.08f;

            float[] v = values.ToArray();
            var container = new Container { RelativeSizeAxes = Axes.Both };

            if (v.Length < 2)
                return container;

            float min = v.Min();
            float range = v.Max() - min;
            float level(float value) => floor + (1 - floor) * (range > 0 ? (value - min) / range : 0.5f);

            var graph = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                Height = height,
                // Inset like the tile's text (and leaving room for the dot at the end).
                Padding = new MarginPadding { Horizontal = HubTile.CONTENT_PADDING },
            };

            // The area under the line, as one shape: opaque columns drawn into a buffer, which is then tinted with a single
            // gradient (semi-transparent columns would show seams where they meet).
            var area = new BufferedContainer(cachedFrameBuffer: true)
            {
                RelativeSizeAxes = Axes.Both,
                Colour = ColourInfo.GradientVertical(colour.Opacity(0.3f), colour.Opacity(0)),
            };

            // Many thin columns, following the line between its points.
            const int columns = 160;

            for (int c = 0; c < columns; c++)
            {
                float position = (float)c / (columns - 1) * (v.Length - 1);
                int index = Math.Min((int)position, v.Length - 2);
                float value = v[index] + (v[index + 1] - v[index]) * (position - index);

                area.Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.X,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomCentre,
                    X = (float)c / (columns - 1),
                    // Overlapping their neighbours, so that no gap shows between them.
                    Width = 1.5f / (columns - 1),
                    Height = level(value),
                    Colour = Color4.White,
                });
            }

            float last = level(v[^1]);

            graph.AddRange(new Drawable[]
            {
                area,
                // The line spans from the floor to the top, as the strips do.
                new LineGraph
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Y,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Height = 1 - floor,
                    Y = -floor,
                    MinValue = min,
                    MaxValue = min + range,
                    LineColour = colour.Opacity(0.85f),
                    Values = v,
                },
                new Circle
                {
                    RelativePositionAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.Centre,
                    X = 1,
                    Y = -last,
                    Size = new Vector2(7),
                    Colour = Color4.White,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Glow,
                        Colour = colour.Opacity(0.8f),
                        Radius = 8,
                    },
                },
            });

            container.Add(graph);
            return container;
        }

        /// <summary>
        /// One bar per value, the highest reaching up to 70% of the height.
        /// </summary>
        public static Drawable Bars(IReadOnlyList<float> values, Color4 colour)
        {
            // Inset like the tile's text.
            var container = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Horizontal = HubTile.CONTENT_PADDING },
            };

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
