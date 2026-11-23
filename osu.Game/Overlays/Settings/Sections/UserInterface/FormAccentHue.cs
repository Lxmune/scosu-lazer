// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Menu;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Settings.Sections.UserInterface
{
    /// <summary>
    /// Settings V2 control for choosing the client's accent hue (0..360°), with quick preset swatches and a full rainbow hue slider.
    /// </summary>
    public partial class FormAccentHue : CompositeDrawable, IHasCurrentValue<int>, IFormControl
    {
        private static readonly int[] preset_hues = { ScosuMenuStyle.DEFAULT_HUE, 335, 0, 35, 145, 190, 215, 238 };

        public Bindable<int> Current
        {
            get => current.Current;
            set
            {
                current.Current = value;
                sliderValue.Disabled = false;
                sliderValue.Default = current.Default;
                sliderValue.Disabled = current.Disabled;
            }
        }

        private readonly BindableNumberWithCurrent<int> current = new BindableNumberWithCurrent<int>(ScosuMenuStyle.DEFAULT_HUE)
        {
            MinValue = 0,
            MaxValue = 360,
            Default = ScosuMenuStyle.DEFAULT_HUE,
        };

        private readonly BindableInt sliderValue = new BindableInt(ScosuMenuStyle.DEFAULT_HUE)
        {
            MinValue = 0,
            MaxValue = 360,
            Default = ScosuMenuStyle.DEFAULT_HUE,
        };

        public LocalisableString Caption { get; init; }
        public LocalisableString HintText { get; init; }

        private FormControlBackground background = null!;
        private FormFieldCaption captionText = null!;
        private Box swatchFill = null!;
        private OsuSpriteText valueLabel = null!;
        private readonly List<PresetSwatch> presetSwatches = new List<PresetSwatch>();

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var swatchRow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(8, 6),
            };

            foreach (int hue in preset_hues)
            {
                var swatch = new PresetSwatch(hue, () =>
                {
                    if (!current.Disabled)
                        current.Value = hue;
                });
                presetSwatches.Add(swatch);
                swatchRow.Add(swatch);
            }

            InternalChildren = new Drawable[]
            {
                background = new FormControlBackground(),
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(9),
                    Spacing = new Vector2(0, 8),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                captionText = new FormFieldCaption
                                {
                                    Caption = Caption,
                                    TooltipText = HintText,
                                    Colour = colourProvider.Content2,
                                },
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(6, 0),
                                    Children = new Drawable[]
                                    {
                                        new CircularContainer
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Size = new Vector2(14),
                                            Masking = true,
                                            BorderThickness = 1.5f,
                                            BorderColour = Color4.White.Opacity(0.5f),
                                            Child = swatchFill = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                            },
                                        },
                                        valueLabel = new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Font = OsuFont.Style.Caption1,
                                            Colour = colourProvider.Content2,
                                        },
                                    },
                                },
                            },
                        },
                        new HueSpectrumSlider
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 20,
                            Current = { BindTarget = sliderValue },
                            OnCommit = () =>
                            {
                                if (!current.Disabled)
                                    current.Value = sliderValue.Value;
                            },
                        },
                        swatchRow,
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            current.BindValueChanged(e =>
            {
                sliderValue.Value = e.NewValue;
                updateDisplay(e.NewValue);
                ValueChanged?.Invoke();
            }, true);

            sliderValue.BindValueChanged(e => updateDisplay(e.NewValue), true);

            current.BindDisabledChanged(disabled =>
            {
                sliderValue.Disabled = disabled;
                updateVisualStyle();
            }, true);
        }

        private void updateDisplay(int hue)
        {
            int wrapped = ((hue % 360) + 360) % 360;
            swatchFill.Colour = wrapped == ScosuMenuStyle.DEFAULT_HUE
                ? Color4Extensions.FromHex(@"a68bff")
                : ScosuMenuStyle.HueColour(wrapped, 0.9f, 0.68f);

            valueLabel.Text = wrapped == ScosuMenuStyle.DEFAULT_HUE
                ? $@"Default ({wrapped}°)"
                : $@"{wrapped}°";
            valueLabel.Colour = colourProvider.Content2;
            captionText.Colour = current.Disabled ? colourProvider.Background1 : colourProvider.Content2;

            foreach (var swatch in presetSwatches)
                swatch.Selected = swatch.Hue == wrapped;
        }

        private void updateVisualStyle()
        {
            if (current.Disabled)
                background.VisualStyle = VisualStyle.Disabled;
            else if (IsHovered)
                background.VisualStyle = VisualStyle.Hovered;
            else
                background.VisualStyle = VisualStyle.Normal;
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateVisualStyle();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            updateVisualStyle();
        }

        public event Action? ValueChanged;

        public bool IsDefault => current.IsDefault;

        public void SetDefault() => current.SetDefault();

        public bool IsDisabled => current.Disabled;

        public float MainDrawHeight => DrawHeight;

        public IEnumerable<LocalisableString> FilterTerms => new[] { Caption, HintText };

        private partial class PresetSwatch : OsuClickableContainer
        {
            public readonly int Hue;

            private bool selected;

            public bool Selected
            {
                get => selected;
                set
                {
                    if (selected == value)
                        return;

                    selected = value;
                    BorderThickness = selected ? 2.5f : 1f;
                    BorderColour = selected ? Color4.White : Color4.White.Opacity(0.25f);
                }
            }

            public PresetSwatch(int hue, Action action)
            {
                Hue = hue;
                Action = action;

                Size = new Vector2(22);
                Masking = true;
                CornerRadius = 11;
                BorderThickness = 1f;
                BorderColour = Color4.White.Opacity(0.25f);

                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = hue == ScosuMenuStyle.DEFAULT_HUE
                        ? Color4Extensions.FromHex(@"8f70ff")
                        : ScosuMenuStyle.HueColour(hue, 0.85f, 0.65f),
                };
            }
        }

        private partial class HueSpectrumSlider : SliderBar<int>
        {
            public Action? OnCommit { get; init; }

            private readonly CircularContainer nub;
            private readonly Box nubFill;

            public HueSpectrumSlider()
            {
                KeyboardStep = 2;

                var stops = new[]
                {
                    Colour4.FromHSV(0f, 0.85f, 0.95f),
                    Colour4.FromHSV(1 / 6f, 0.85f, 0.95f),
                    Colour4.FromHSV(2 / 6f, 0.85f, 0.95f),
                    Colour4.FromHSV(3 / 6f, 0.85f, 0.95f),
                    Colour4.FromHSV(4 / 6f, 0.85f, 0.95f),
                    Colour4.FromHSV(5 / 6f, 0.85f, 0.95f),
                    Colour4.FromHSV(1f, 0.85f, 0.95f),
                };

                var track = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.X,
                    Height = 10,
                    Masking = true,
                    CornerRadius = 5,
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[0], stops[1]) },
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[1], stops[2]) },
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[2], stops[3]) },
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[3], stops[4]) },
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[4], stops[5]) },
                                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourInfo.GradientHorizontal(stops[5], stops[6]) },
                            },
                        },
                    },
                };

                Children = new Drawable[]
                {
                    track,
                    nub = new CircularContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        RelativePositionAxes = Axes.X,
                        Size = new Vector2(18),
                        Masking = true,
                        BorderThickness = 2.5f,
                        BorderColour = Color4.White,
                        Child = nubFill = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                };
            }

            protected override void UpdateValue(float value)
            {
                nub.MoveToX(value, 60, Easing.OutQuint);
                nubFill.Colour = ScosuMenuStyle.HueColour(Current.Value, 0.9f, 0.65f);
            }

            protected override bool Commit()
            {
                bool result = base.Commit();

                if (result)
                    OnCommit?.Invoke();

                return result;
            }
        }
    }
}
