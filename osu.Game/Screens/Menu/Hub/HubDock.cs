// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// scosu: the band, docked as a slim row along the top while the menu is scrolled down to its hub:
    /// a small logo (back up), the destinations on the left, and back up, settings and exit on the right.
    /// </summary>
    public partial class HubDock : VisibilityContainer
    {
        public const float HEIGHT = 56;

        /// <summary>
        /// One of the dock's buttons.
        /// </summary>
        public record Item(LocalisableString Text, IconUsage Icon, Action Action, bool Primary = false);

        /// <summary>
        /// Called when the small logo is clicked.
        /// </summary>
        public Action? OnLogo;

        private readonly IReadOnlyList<Item> leading;
        private readonly IReadOnlyList<Item> trailing;

        private Container content = null!;
        private Box dockBackground = null!;
        private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

        public HubDock(IReadOnlyList<Item> leading, IReadOnlyList<Item> trailing)
        {
            this.leading = leading;
            this.trailing = trailing;

            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = content = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // Clicks between the buttons stay on the dock.
                    new InputBlocker
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            dockBackground = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ScosuMenuStyle.BAND.Opacity(0.94f),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 1,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Colour = Color4.White.Opacity(0.08f),
                            },
                        },
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 14 },
                        Children = new Drawable[]
                        {
                            new DockLogo
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Margin = new MarginPadding { Right = 8 },
                                Action = () => OnLogo?.Invoke(),
                            },
                        }.Concat(leading.Select(createButton)).ToArray(),
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Margin = new MarginPadding { Right = 14 },
                        Children = trailing.Select(createButton).ToArray(),
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentHue.BindValueChanged(_ => dockBackground.Colour = ScosuMenuStyle.BAND.Opacity(0.94f), true);
        }

        private static Drawable createButton(Item item) => new DockButton(item)
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
        };

        protected override void PopIn()
        {
            // Docks as the band scrolls away.
            ClearTransforms();
            content.ClearTransforms();
            this.FadeIn(200, Easing.OutQuint);
            content.MoveToY(-8).MoveToY(0, 250, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            ClearTransforms();
            content.ClearTransforms();
            this.FadeOut(200, Easing.OutQuint);
            content.MoveToY(-12, 200, Easing.OutQuint);
        }

        private partial class InputBlocker : Container
        {
            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;
        }

        /// <summary>
        /// The scosu logo, small.
        /// </summary>
        private partial class DockLogo : OsuClickableContainer
        {
            private Sprite sprite = null!;

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Size = new Vector2(40);

                Child = sprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    FillMode = FillMode.Fit,
                    Texture = textures.Get(@"Menu/scosu-logo"),
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                sprite.ScaleTo(1.1f, 300, Easing.OutElasticHalf);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                sprite.ScaleTo(1, 300, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        /// <summary>
        /// A pill with an icon and a name: violet for the primary one, faint otherwise.
        /// </summary>
        public partial class DockButton : OsuClickableContainer
        {
            public readonly Item Item;

            private Box fill = null!;
            private Box hover = null!;
            private Container pill = null!;
            private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

            public DockButton(Item item)
            {
                Item = item;
                Action = item.Action;

                // Before load: OsuClickableContainer sets its content up for auto-sizing there.
                AutoSizeAxes = Axes.X;
                Height = 36;
            }

            [BackgroundDependencyLoader]
            private void load()
            {

                Child = pill = new Container
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 18,
                    Children = new Drawable[]
                    {
                        fill = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Item.Primary ? ScosuMenuStyle.PRIMARY : Color4.White.Opacity(0.07f),
                        },
                        hover = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                            Alpha = 0,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Padding = new MarginPadding { Horizontal = 15 },
                            Children = new Drawable[]
                            {
                                new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = Item.Icon,
                                    Size = new Vector2(15),
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Text = Item.Text,
                                    Font = OsuFont.GetFont(size: 15, weight: Item.Primary ? FontWeight.Bold : FontWeight.SemiBold),
                                },
                            },
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                if (Item.Primary)
                    accentHue.BindValueChanged(_ => fill.Colour = ScosuMenuStyle.PRIMARY, true);
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.FadeTo(0.12f, HubTile.HOVER_DURATION, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.FadeOut(HubTile.HOVER_DURATION, Easing.OutQuint);
                base.OnHoverLost(e);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                pill.ScaleTo(0.95f, 400, Easing.OutQuint);
                return base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                pill.ScaleTo(1, 400, Easing.OutBack);
                base.OnMouseUp(e);
            }
        }
    }
}
