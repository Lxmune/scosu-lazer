// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Overlays;
using osu.Game.Overlays.Dashboard;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// How many of the user's friends are online, and some of them. Opens the friends list.
    /// </summary>
    public partial class HubFriendsTile : HubTile
    {
        private const int max_avatars = 5;
        private const float avatar_size = 40;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MetadataClient? metadataClient { get; set; }

        [Resolved]
        private DashboardOverlay? dashboard { get; set; }

        private readonly IBindableList<APIRelation> friends = new BindableList<APIRelation>();
        private readonly IBindableDictionary<int, UserPresence> presences = new BindableDictionary<int, UserPresence>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        public HubFriendsTile()
        {
            Eyebrow.Text = MenuHubStrings.Friends.ToUpper();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            friends.BindTo(api.LocalUserState.Friends);
            friends.BindCollectionChanged((_, _) => Scheduler.AddOnce(refresh));

            // Friends' presence is sent without asking for it, unlike everyone's.
            if (metadataClient != null)
            {
                presences.BindTo(metadataClient.FriendPresences);
                presences.BindCollectionChanged((_, _) => Scheduler.AddOnce(refresh));
            }

            apiState.BindTo(api.State);
            apiState.BindValueChanged(_ => Schedule(refresh));

            refresh();
        }

        protected override void OnAccentHueChanged()
        {
            if (IsLoaded)
                refresh();
        }

        private void refresh()
        {
            if (!api.IsLoggedIn)
            {
                ShowPlaceholder(FontAwesome.Solid.UserFriends, MenuHubStrings.LogInForFriends);
                return;
            }

            if (friends.Count == 0)
            {
                ShowPlaceholder(FontAwesome.Solid.UserFriends, MenuHubStrings.NoFriends);
                return;
            }

            var online = friends.Where(f => presences.ContainsKey(f.TargetID)).ToArray();

            ShowContent();
            Body.Add(new OsuSpriteText
            {
                Text = MenuHubStrings.FriendsOnline(online.Length.ToString(@"N0"), friends.Count.ToString(@"N0")),
                Font = ScosuMenuStyle.Display(20),
            });

            var avatars = online.Where(f => f.TargetUser != null).Take(max_avatars).Select(f => f.TargetUser!).ToArray();

            if (avatars.Length > 0)
            {
                Body.Add(new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    // Overlapping, like a stack of faces.
                    Spacing = new Vector2(-10, 0),
                    Margin = new MarginPadding { Top = 4 },
                    ChildrenEnumerable = avatars.Select(u => new CircularContainer
                    {
                        Size = new Vector2(avatar_size),
                        Masking = true,
                        BorderThickness = 2,
                        BorderColour = ScosuMenuStyle.BAND,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ScosuMenuStyle.FILL,
                            },
                            new UpdateableAvatar(u, false)
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                        },
                    }),
                });
            }

            // Who they are, or where to find them when nobody is on.
            string names = string.Join(@", ", avatars.Take(3).Select(u => u.Username));
            int more = online.Length - Math.Min(avatars.Length, 3);

            if (names.Length > 0 && more > 0)
                names += $@" +{more}";

            SetFoot(FontAwesome.Solid.Comments, names.Length > 0 ? names : MenuHubStrings.OpenFriends);
            SetAction(openFriends);
        }

        private void openFriends()
        {
            if (dashboard == null)
                return;

            dashboard.Header.Current.Value = DashboardOverlayTabs.Friends;
            dashboard.Show();
        }
    }
}
