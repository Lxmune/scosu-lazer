// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Sections
{
    public partial class AboutSection : ProfileSection
    {
        public override LocalisableString Title => UsersStrings.ShowExtraMeTitle;

        public override string Identifier => @"me";

        // scosu: the player's "me!" page, from the Markdown the server sends (`page.raw`).
        public AboutSection()
        {
            Children = new Drawable[]
            {
                new PageSubsection(User),
            };
        }

        private partial class PageSubsection : ProfileSubsection
        {
            private OsuMarkdownContainer markdown = null!;

            public PageSubsection(Bindable<UserProfileData?> user)
                : base(user)
            {
            }

            protected override Drawable CreateContent() => new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Top = 10 },
                Child = markdown = new OsuMarkdownContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
            };

            protected override void LoadComplete()
            {
                base.LoadComplete();
                User.BindValueChanged(e =>
                {
                    string? raw = e.NewValue?.User.Page?.Raw;
                    HasContent.Value = !string.IsNullOrWhiteSpace(raw);
                    markdown.Text = raw ?? string.Empty;
                }, true);
            }
        }
    }
}
