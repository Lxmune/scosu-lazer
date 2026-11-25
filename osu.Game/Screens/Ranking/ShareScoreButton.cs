// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Ranking
{
    /// <summary>
    /// scosu: copies the selected score's page link. Pasted in Discord or elsewhere, the link shows the score's picture card
    /// (drawn by the server), so every shared play leads back to the website.
    /// </summary>
    public partial class ShareScoreButton : GrayButton
    {
        public readonly Bindable<ScoreInfo?> Score = new Bindable<ScoreInfo?>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        public ShareScoreButton()
            : base(FontAwesome.Solid.ShareAlt)
        {
            Size = new Vector2(75, 30);
            TooltipText = ScosuResultsStrings.ShareScore;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Action = share;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Scores without a website page (offline, or not from this server) can't be shared.
            Score.BindValueChanged(_ => Enabled.Value = !string.IsNullOrEmpty(api.Endpoints.WebsiteUrl), true);
        }

        private void share()
        {
            string? url = ScoreUrl(api.Endpoints.WebsiteUrl, Score.Value?.OnlineID ?? 0);

            if (url == null)
            {
                notifications?.Post(new SimpleNotification { Text = ScosuResultsStrings.ShareScoreNotSubmitted, Icon = FontAwesome.Solid.Hourglass });
                return;
            }

            clipboard.SetText(url);
            notifications?.Post(new SimpleNotification { Text = ScosuResultsStrings.ShareScoreCopied, Icon = FontAwesome.Solid.Check });
        }

        /// <summary>
        /// The page of score <paramref name="onlineId"/> on <paramref name="websiteUrl"/>, or <c>null</c> while the score has no online id
        /// (still being submitted, or never submitted).
        /// </summary>
        public static string? ScoreUrl(string websiteUrl, long onlineId)
            => onlineId > 0 && !string.IsNullOrEmpty(websiteUrl) ? $@"{websiteUrl.TrimEnd('/')}/scores/{onlineId}" : null;
    }
}
