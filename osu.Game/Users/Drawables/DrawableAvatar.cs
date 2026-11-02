// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Users.Drawables
{
    [LongRunningLoad]
    public partial class DrawableAvatar : Sprite
    {
        private readonly IUser user;

        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        /// <summary>
        /// A simple, non-interactable avatar sprite for the specified user.
        /// </summary>
        /// <param name="user">The user. A null value will get a placeholder avatar.</param>
        public DrawableAvatar(IUser user = null)
        {
            this.user = user;

            RelativeSizeAxes = Axes.Both;
            FillMode = FillMode.Fit;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures, OnlineAssetCachingStore onlineTextures)
        {
            if (user != null && user.OnlineID > 1)
                // TODO: The fallback here should not need to exist. Users should be looked up and populated via UserLookupCache or otherwise
                // in remaining cases where this is required (chat tabs, local leaderboard), at which point this should be removed.
                Texture = onlineTextures.Get((user as APIUser)?.AvatarUrl ?? fallbackAvatarUrl());

            Texture ??= textures.Get(@"Online/avatar-guest");
        }

        // scosu: users known only by their id get their picture from this server. osu!'s picture of the same id
        // belongs to someone else. Players without a picture get a 404 there, and so the guest avatar.
        private string fallbackAvatarUrl() => api?.Endpoints.RanksByStableScore == true
            ? $@"{api.Endpoints.APIUrl}/images/avatar/{user.OnlineID}"
            : $@"https://a.ppy.sh/{user.OnlineID}";

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(300, Easing.OutQuint);
        }
    }
}
