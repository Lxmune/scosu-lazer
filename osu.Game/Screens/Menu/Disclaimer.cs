// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// What scosu is, before the intro of every launch (osu!lazer's old disclaimer screen, which upstream removed).
    /// </summary>
    public partial class Disclaimer : StartupScreen
    {
        private SpriteIcon icon = null!;
        private Color4 iconColour;
        private LinkFlowContainer textFlow = null!;
        private LinkFlowContainer headsUpFlow = null!;
        private FillFlowContainer fill = null!;

        private Drawable? warning;

        private const float icon_y = -85;
        private const float icon_size = 30;

        private readonly OsuScreen nextScreen;

        /// <summary>
        /// Text that disappears when the logo lands, so "the score-only osu! server" ends as "the score-only server".
        /// </summary>
        private readonly List<ITextPart> expendableText = new List<ITextPart>();

        public Disclaimer(OsuScreen nextScreen)
        {
            this.nextScreen = nextScreen;
            ValidForResume = false;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChildren = new Drawable[]
            {
                icon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = OsuIcon.Logo,
                    Size = new Vector2(icon_size),
                    Y = icon_y,
                },
                fill = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Y = icon_y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    Children = new Drawable[]
                    {
                        textFlow = new LinkFlowContainer
                        {
                            Width = 680,
                            AutoSizeAxes = Axes.Y,
                            TextAnchor = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Spacing = new Vector2(0, 2),
                        },
                    }
                },
                headsUpFlow = new LinkFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    TextAnchor = Anchor.BottomCentre,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Padding = new MarginPadding(20),
                    Alpha = 0,
                    Spacing = new Vector2(0, 2),
                },
            };

            static void formatRegular(SpriteText t) => t.Font = OsuFont.GetFont(size: 20, weight: FontWeight.Regular);
            static void formatSemiBold(SpriteText t) => t.Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold);

            textFlow.AddText("this is ", t => t.Font = t.Font.With(Typeface.Torus, 30, FontWeight.Regular));
            textFlow.AddText("scosu", t =>
            {
                t.Font = t.Font.With(Typeface.Torus, 30, FontWeight.Regular);
                t.Colour = colours.PinkLight;
            });

            textFlow.NewParagraph();

            textFlow.AddText("the ", formatRegular);
            textFlow.AddText("score-only", t =>
            {
                t.Font = t.Font.With(Typeface.Torus, 20, FontWeight.SemiBold);
                t.Colour = colours.Pink;
            });
            expendableText.Add(textFlow.AddText(" osu!", formatRegular));
            textFlow.AddText(" server.", formatRegular);

            textFlow.NewParagraph();
            textFlow.NewParagraph();

            textFlow.AddText("rankings and leaderboards go by score, not pp.", formatRegular);
            textFlow.NewParagraph();
            textFlow.AddText("relax and autopilot have leaderboards of their own.", formatRegular);

            iconColour = colours.Yellow;

            headsUpFlow.AddIcon(FontAwesome.Solid.ExclamationTriangle, t =>
            {
                warning = t;

                t.Padding = new MarginPadding { Right = 5, Top = 3 };
                t.Font = t.Font.With(size: 20);
                t.Colour = colours.Yellow;

                Schedule(() => warning?.FlashColour(Color4.White, 750, Easing.OutQuint).Loop());
            });
            headsUpFlow.AddText("heads up: ", t =>
            {
                formatSemiBold(t);
                t.Colour = colours.Yellow;
            });
            headsUpFlow.AddText("scosu is unofficial and not affiliated with ppy. sign in with a scosu account, not your osu! one.", formatSemiBold);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            LoadComponentAsync(nextScreen);
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            icon.RotateTo(10);
            icon.FadeOut();
            icon.ScaleTo(0.5f);

            icon.Delay(500).FadeIn(500).ScaleTo(1, 500, Easing.OutQuint);

            using (BeginDelayedSequence(3000))
            {
                icon.FadeColour(iconColour, 200, Easing.OutQuint);
                icon.MoveToY(icon_y * 1.3f, 500, Easing.OutCirc)
                    .RotateTo(-360, 520, Easing.OutQuint)
                    .Then()
                    .MoveToY(icon_y, 160, Easing.InQuart)
                    .FadeColour(Color4.White, 160);

                using (BeginDelayedSequence(520 + 160))
                {
                    fill.MoveToOffset(new Vector2(0, 15), 160, Easing.OutQuart);
                    Schedule(() => expendableText.SelectMany(t => t.Drawables).ForEach(t =>
                    {
                        t.FadeOut(100);
                        t.ScaleTo(new Vector2(0, 1), 100, Easing.OutQuart);
                    }));
                }
            }

            headsUpFlow.FadeOut().Delay(2000).FadeIn(500);
            double delay = 500;
            foreach (var c in textFlow.Children)
                c.FadeTo(0.001f).Delay(delay += 20).FadeIn(500);

            // Longer than osu!'s 5.5s: there is more to read.
            this
                .FadeInFromZero(500)
                .Then(7500)
                .FadeOut(250)
                .ScaleTo(0.9f, 250, Easing.InQuint)
                .Finally(_ => this.Push(nextScreen));
        }
    }
}
