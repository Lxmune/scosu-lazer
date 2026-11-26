// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: one of today's daily tasks: a tick, the task in words and its progress bar on the right
    /// (in the hub's tasks tile and the login briefing).
    /// </summary>
    public partial class DailyTaskRow : Container
    {
        private const float row_height = 14;
        private const float bar_width = 64;

        public DailyTaskRow(APIDailyTasks.DailyTask task)
        {
            RelativeSizeAxes = Axes.X;
            Height = row_height;

            Children = new Drawable[]
            {
                new SpriteIcon
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Icon = FontAwesome.Solid.Check,
                    Size = new Vector2(9),
                    Colour = task.Completed ? ScosuMenuStyle.VIOLET : ScosuMenuStyle.TEXT_DIM,
                    Alpha = task.Completed ? 1 : 0.25f,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 16, Right = bar_width + 10 },
                    Child = new TruncatingSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        Text = DailyTaskFormatting.Describe(task),
                        Font = ScosuMenuStyle.Body(12),
                        Colour = task.Completed ? ScosuMenuStyle.TEXT_DIM : Color4.White,
                    },
                },
                new Container
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Width = bar_width,
                    Height = 4,
                    Masking = true,
                    CornerRadius = 2,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ScosuMenuStyle.FILL,
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = DailyTaskFormatting.Progress(task),
                            Colour = task.Completed ? ScosuMenuStyle.VIOLET_INK : ScosuMenuStyle.VIOLET,
                        },
                    },
                },
            };
        }
    }
}
