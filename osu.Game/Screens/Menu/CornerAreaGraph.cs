// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: a soft area graph made of thin columns, fading towards its top. Meant to sit behind a card's text.
    /// Setting <see cref="SetValues"/> again makes the columns grow or shrink to the new shape.
    /// </summary>
    public partial class CornerAreaGraph : Container
    {
        private const int column_count = 48;

        private readonly Box[] columns;

        public CornerAreaGraph(Color4 colour)
        {
            columns = Enumerable.Range(0, column_count).Select(i => new Box
            {
                RelativePositionAxes = Axes.X,
                RelativeSizeAxes = Axes.Both,
                X = i / (float)column_count,
                Width = 1f / column_count + 0.002f,
                Height = 0,
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                Colour = ColourInfo.GradientVertical(colour.Opacity(0.04f), colour.Opacity(0.3f)),
            }).ToArray();

            AddRange(columns);
        }

        /// <summary>
        /// The values from left to right, linearly interpolated; the graph is as high as the largest of them.
        /// </summary>
        public void SetValues(IReadOnlyList<float> values)
        {
            float max = values.Count > 0 ? values.Max() : 0;

            for (int i = 0; i < columns.Length; i++)
            {
                float height = 0;

                if (max > 0)
                {
                    float position = values.Count > 1 ? i / (float)(columns.Length - 1) * (values.Count - 1) : 0;
                    int lower = Math.Min((int)position, values.Count - 1);
                    int upper = Math.Min(lower + 1, values.Count - 1);
                    float value = values[lower] + (values[upper] - values[lower]) * (position - lower);

                    height = value / max;
                }

                // A little stagger, so that the area sweeps in from the left.
                using (columns[i].BeginDelayedSequence(i * 8))
                    columns[i].ResizeHeightTo(height, 700, Easing.OutQuint);
            }
        }
    }
}
