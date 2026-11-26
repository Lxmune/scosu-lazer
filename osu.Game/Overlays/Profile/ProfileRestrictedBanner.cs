// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osuTK;

namespace osu.Game.Overlays.Profile
{
    /// <summary>
    /// scosu: a restricted player's profile stays visible, and says so first, in a red strip above the header.
    /// </summary>
    public partial class ProfileRestrictedBanner : CompositeDrawable
    {
        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private OsuSpriteText hint = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Alpha = 0;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colours.Red3,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(16, 0),
                    Padding = new MarginPadding { Vertical = 14, Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING },
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Icon = FontAwesome.Solid.Ban,
                            Size = new Vector2(34),
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = ScosuProfileStrings.Restricted.ToUpper(),
                                    Font = OsuFont.GetFont(size: 28, weight: FontWeight.Bold),
                                },
                                hint = new OsuSpriteText
                                {
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                    Alpha = 0.85f,
                                },
                            }
                        },
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(user =>
            {
                if (user.NewValue?.User is { IsRestricted: true } restricted)
                {
                    hint.Text = ScosuProfileStrings.RestrictedHint(restricted.Username);
                    Show();
                }
                else
                    Hide();
            }, true);
        }
    }
}
