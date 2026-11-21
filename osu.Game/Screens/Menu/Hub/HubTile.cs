// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// One tile of the hub: an eyebrow, a body the subclass fills in and a foot line, on a glass fill that lights up under the cursor.
    /// A tile without an action (see <see cref="SetAction"/>) is just information and doesn't react to the cursor.
    /// </summary>
    public abstract partial class HubTile : OsuClickableContainer
    {
        private const float horizontal_padding = 14;

        protected OsuSpriteText Eyebrow { get; }

        /// <summary>
        /// What the tile says, below the eyebrow.
        /// </summary>
        protected FillFlowContainer Body { get; }

        private readonly Box fill;
        private readonly Container hoverOutline;
        private readonly Drawable foot;
        private readonly SpriteIcon footIcon;
        private readonly OsuSpriteText footText;

        protected HubTile(Color4? accent = null)
        {
            RelativeSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = ScosuMenuStyle.CORNER_RADIUS;
            CornerExponent = 2.5f;
            BorderThickness = 1.5f;
            BorderColour = ScosuMenuStyle.OUTLINE;

            Children = new Drawable[]
            {
                fill = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = base_fill,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = horizontal_padding + (accent != null ? 4 : 0), Vertical = 12 },
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                            Children = new Drawable[]
                            {
                                Eyebrow = new OsuSpriteText
                                {
                                    Font = ScosuMenuStyle.Eyebrow,
                                    Colour = accent ?? ScosuMenuStyle.VIOLET,
                                    Spacing = new Vector2(1.2f, 0),
                                },
                                Body = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 4),
                                },
                            },
                        },
                        foot = new FillFlowContainer
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6, 0),
                            Alpha = 0,
                            Children = new Drawable[]
                            {
                                footIcon = new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(11),
                                },
                                footText = new TruncatingSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = ScosuMenuStyle.Body(12),
                                },
                            },
                        },
                    },
                },
                hoverOutline = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = ScosuMenuStyle.CORNER_RADIUS,
                    CornerExponent = 2.5f,
                    BorderThickness = 2,
                    BorderColour = ScosuMenuStyle.VIOLET,
                    Alpha = 0,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true,
                    },
                },
            };

            if (accent != null)
            {
                AddInternal(new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 4,
                    Colour = accent.Value,
                });
            }

            SetAction(null);
        }

        private const float base_fill = 0.045f;

        /// <summary>
        /// What clicking the tile does, or nothing for a tile that has nothing to open.
        /// </summary>
        protected void SetAction(Action? action)
        {
            Action = action;
            Enabled.Value = action != null;

            if (action == null)
                setHovered(false);
        }

        /// <summary>
        /// The line at the bottom of the tile; empty text hides it.
        /// </summary>
        protected void SetFoot(IconUsage? icon, LocalisableString text, Color4? colour = null)
        {
            footText.Text = text;
            foot.Alpha = string.IsNullOrEmpty(text.ToString()) ? 0 : 1;

            footIcon.Alpha = icon == null ? 0 : 1;

            if (icon != null)
                footIcon.Icon = icon.Value;

            var c = colour ?? ScosuMenuStyle.TEXT_DIM;
            footIcon.Colour = c;
            footText.Colour = c;
        }

        /// <summary>
        /// Replaces the body by a muted line, for when the tile has nothing to show.
        /// </summary>
        protected void ShowPlaceholder(LocalisableString text)
        {
            Body.Clear();
            Body.Add(new OsuTextFlowContainer(t =>
            {
                t.Font = ScosuMenuStyle.Body(14);
                t.Colour = ScosuMenuStyle.TEXT_DIM;
            })
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = text,
            });

            SetFoot(null, default);
            SetAction(null);
        }

        private void setHovered(bool hovered)
        {
            fill.FadeTo(hovered ? 0.1f : base_fill, 200, Easing.OutQuint);
            hoverOutline.FadeTo(hovered ? 1 : 0, 200, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (Enabled.Value)
                setHovered(true);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            setHovered(false);
            base.OnHoverLost(e);
        }
    }
}
