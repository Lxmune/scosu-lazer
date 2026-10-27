// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Online.API.Requests;
using osu.Framework.Bindables;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.API;
using System.Collections.Generic;

namespace osu.Game.Overlays.Profile.Sections.Kudosu
{
    public partial class PaginatedKudosuHistoryContainer : PaginatedProfileSubsection<APIKudosuHistory>
    {
        public PaginatedKudosuHistoryContainer(Bindable<UserProfileData?> user)
            : base(user)
        {
        }

        protected override APIRequest<List<APIKudosuHistory>> CreateRequest(UserProfileData user, PaginationParameters pagination)
            => new GetUserKudosuHistoryRequest(user.User.Id, pagination);

        protected override Drawable CreateDrawableItem(APIKudosuHistory item) => new DrawableKudosuHistoryItem(item);
    }
}
