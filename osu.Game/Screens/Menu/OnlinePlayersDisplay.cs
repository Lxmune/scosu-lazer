// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Metadata;
using osu.Game.Overlays;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The other players online, shown while the main menu is open and someone besides the local user is:
    /// a small "N online" chip, which opens the list of them (growing away from the screen's edge) when clicked.
    /// Each one opens their profile on click, and has the usual player menu (message, spectate, invite…) on right click.
    /// </summary>
    public partial class OnlinePlayersDisplay : CornerCard
    {
        private const float transition_duration = 500;
        private const float width = 280;

        private static readonly Color4 dot_colour = Color4Extensions.FromHex(@"6ee39a");

        /// <summary>
        /// How many players are listed; the others are counted under the list.
        /// </summary>
        private const int max_shown = 6;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MetadataClient metadataClient { get; set; } = null!;

        [Resolved]
        private UserLookupCache userLookupCache { get; set; } = null!;

        [Resolved]
        private DashboardOverlay? dashboard { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly IBindableDictionary<int, UserPresence> presences = new BindableDictionary<int, UserPresence>();
        private readonly IBindable<int> accentHue = ScosuMenuStyle.AccentHue.GetBoundCopy();

        /// <summary>
        /// The players online, in the order they came online, once their details have been looked up.
        /// </summary>
        private readonly List<APIUser> users = new List<APIUser>();

        private readonly HashSet<int> pendingLookups = new HashSet<int>();

        private IDisposable? presenceWatch;

        private FillFlowContainer content = null!;
        private Container panel = null!;
        private Drawable chip = null!;
        private Box chipBackground = null!;

        /// <summary>
        /// scosu: drawn without its pill (no background or outline, slightly larger text), for the hub's bar where it sits among other text.
        /// </summary>
        public bool Flat { get; init; }
        private OsuSpriteText chipText = null!;
        private OsuSpriteText countText = null!;
        private FillFlowContainer<PlayerRow> list = null!;
        private OsuSpriteText moreText = null!;

        private bool expanded;

        // Clicking anywhere else takes the focus away, which closes the list.
        public override bool AcceptsFocus => expanded;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = Margined = content = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Alpha = 0,
                Children = new[]
                {
                    panel = new Container
                    {
                        Width = width,
                        AutoSizeAxes = Axes.Y,
                        Alpha = 0,
                        Children = new Drawable[]
                        {
                            ScosuMenuStyle.CreateGlassBackground(),
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding(10),
                                Spacing = new Vector2(0, 4),
                                Children = new Drawable[]
                                {
                                    new ShowAllButton(showAll)
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(6, 0),
                                            Padding = new MarginPadding { Horizontal = 5, Bottom = 2 },
                                            Children = new Drawable[]
                                            {
                                                new Circle
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Size = new Vector2(7),
                                                    Colour = dot_colour,
                                                },
                                                countText = new OsuSpriteText
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Font = ScosuMenuStyle.Eyebrow,
                                                    Spacing = new Vector2(1.5f, 0),
                                                    Colour = dot_colour,
                                                },
                                            },
                                        },
                                    },
                                    list = new FillFlowContainer<PlayerRow>
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 4),
                                    },
                                    new ShowAllButton(showAll)
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = moreText = new OsuSpriteText
                                        {
                                            Margin = new MarginPadding { Horizontal = 5, Top = 2 },
                                            Font = ScosuMenuStyle.Body(12, FontWeight.SemiBold),
                                            Colour = ScosuMenuStyle.VIOLET_INK,
                                        },
                                    },
                                },
                            },
                        },
                    },
                    chip = new ChipButton(toggle, Flat)
                    {
                        Children = new Drawable[]
                        {
                            chipBackground = new Box
                            {
                                Alpha = Flat ? 0 : 1,
                                RelativeSizeAxes = Axes.Both,
                                Colour = ScosuMenuStyle.GLASS,
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(6, 0),
                                Padding = new MarginPadding { Horizontal = Flat ? 0 : 11, Vertical = 5 },
                                Children = new Drawable[]
                                {
                                    new Circle
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(7),
                                        Colour = dot_colour,
                                    },
                                    chipText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = ScosuMenuStyle.Body(Flat ? 14 : 12, FontWeight.SemiBold),
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override void OnAnchorApplied(Anchor anchor)
        {
            bool bottom = (anchor & Anchor.y2) != 0;
            bool left = (anchor & Anchor.x0) != 0;

            // The list opens away from the screen's edge, and lines up with the chip on the side of the corner.
            var side = left ? Anchor.TopLeft : Anchor.TopRight;
            panel.Anchor = panel.Origin = chip.Anchor = chip.Origin = side;

            content.SetLayoutPosition(panel, bottom ? -1 : 1);
            content.SetLayoutPosition(chip, 0);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentHue.BindValueChanged(_ =>
            {
                chipBackground.Colour = ScosuMenuStyle.GLASS;
                moreText.Colour = ScosuMenuStyle.VIOLET_INK;
            }, true);

            // Presence is only sent to clients that watch it, so watch it while the menu shows the list.
            State.BindValueChanged(state =>
            {
                if (state.NewValue == Visibility.Visible)
                    presenceWatch ??= metadataClient.BeginWatchingUserPresence();
                else
                {
                    presenceWatch?.Dispose();
                    presenceWatch = null;
                }
            }, true);

            presences.BindTo(metadataClient.UserPresences);
            presences.BindCollectionChanged(onPresencesChanged, true);
        }

        private void onPresencesChanged(object? sender, NotifyDictionaryChangedEventArgs<int, UserPresence> e)
        {
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Add:
                    foreach ((int userId, _) in e.NewItems!)
                    {
                        if (userId != api.LocalUser.Value.OnlineID && users.All(u => u.OnlineID != userId))
                            pendingLookups.Add(userId);
                    }

                    Scheduler.AddOnce(lookUpPending);
                    break;

                case NotifyDictionaryChangedAction.Remove:
                    foreach ((int userId, _) in e.OldItems!)
                    {
                        pendingLookups.Remove(userId);
                        users.RemoveAll(u => u.OnlineID == userId);
                    }

                    updateList();
                    break;

                // A changed status or activity is picked up by the rows themselves.
            }
        }

        private void lookUpPending()
        {
            if (pendingLookups.Count == 0)
                return;

            int[] ids = pendingLookups.ToArray();
            pendingLookups.Clear();

            userLookupCache.GetUsersAsync(ids).ContinueWith(task => Schedule(() =>
            {
                foreach (APIUser? user in task.GetResultSafely())
                {
                    // They may have gone offline while they were looked up.
                    if (user == null || metadataClient.GetPresence(user.OnlineID) == null || users.Any(u => u.OnlineID == user.OnlineID))
                        continue;

                    users.Add(user);
                }

                updateList();
            }));
        }

        private void updateList()
        {
            var shown = users.Take(max_shown).ToArray();

            foreach (var row in list.Where(r => !shown.Contains(r.User)).ToArray())
                list.Remove(row, true);

            foreach (var user in shown.Where(u => list.All(r => !r.User.Equals(u))))
                list.Add(new PlayerRow(user));

            for (int i = 0; i < shown.Length; i++)
                list.SetLayoutPosition(list.Single(r => r.User.Equals(shown[i])), i);

            countText.Text = OnlinePlayersStrings.OnlineCount(users.Count.ToLocalisableString(@"N0")).ToUpper();
            chipText.Text = OnlinePlayersStrings.OnlineCount(users.Count.ToLocalisableString(@"N0"));

            int hidden = users.Count - shown.Length;
            moreText.Text = hidden > 0 ? OnlinePlayersStrings.AndMore(hidden.ToLocalisableString(@"N0")) : OnlinePlayersStrings.ShowAll;

            if (users.Count > 0)
                content.FadeIn(transition_duration, Easing.OutQuint);
            else
            {
                expanded = false;
                content.FadeOut(transition_duration, Easing.OutQuint);
            }

            updatePanel();
        }

        private void toggle() => setExpanded(!expanded);

        private void setExpanded(bool value)
        {
            if (expanded == value)
                return;

            expanded = value;

            // Taking focus is what lets a click elsewhere close the list again.
            if (expanded)
                GetContainingFocusManager()?.ChangeFocus(this);

            updatePanel();
        }

        private void updatePanel()
        {
            if (expanded)
                panel.FadeIn(200, Easing.OutQuint);
            else
                panel.FadeOut(200, Easing.OutQuint);
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);
            setExpanded(false);
        }

        private void showAll() => dashboard?.ShowCurrentlyOnline();

        protected override void PopIn() => this.FadeIn(transition_duration, Easing.OutQuint);

        protected override void PopOut()
        {
            setExpanded(false);
            this.FadeOut(transition_duration, Easing.OutQuint);
        }

        protected override void Dispose(bool isDisposing)
        {
            presenceWatch?.Dispose();
            presenceWatch = null;

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// The header and footer of the list, which open the dashboard's list of everyone online.
        /// </summary>
        private partial class ShowAllButton : OsuClickableContainer
        {
            public ShowAllButton(Action action)
            {
                Action = action;
                TooltipText = OnlinePlayersStrings.ShowAll;
            }
        }

        /// <summary>
        /// The pill at rest: a green dot and the number of players online.
        /// </summary>
        private partial class ChipButton : OsuClickableContainer
        {
            private readonly bool flat;

            public ChipButton(Action action, bool flat)
            {
                this.flat = flat;
                Action = action;
                AutoSizeAxes = Axes.Both;
                Masking = !flat;
                BorderThickness = flat ? 0 : 1.5f;
                BorderColour = ScosuMenuStyle.OUTLINE;
            }

            protected override void Update()
            {
                base.Update();

                if (!flat)
                    CornerRadius = Math.Min(DrawWidth, DrawHeight) / 2f;
            }
        }

        /// <summary>
        /// One player: avatar, name and what they are doing.
        /// </summary>
        private partial class PlayerRow : ExtendedUserPanel
        {
            public PlayerRow(APIUser user)
                : base(user)
            {
                RelativeSizeAxes = Axes.X;
                Height = 36;
                CornerRadius = 8;
            }

            private const float text_width = width - 100;

            // The cover would be too busy at this size.
            protected override Drawable? CreateBackground() => null;

            protected override Drawable CreateLayout()
            {
                var status = CreateStatusMessage(false);

                foreach (var text in status.OfType<OsuSpriteText>())
                    text.Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold);

                return new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 2, Right = 10 },
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Children = new Drawable[]
                            {
                                CreateAvatar().With(avatar =>
                                {
                                    avatar.Anchor = Anchor.CentreLeft;
                                    avatar.Origin = Anchor.CentreLeft;
                                    avatar.Size = new Vector2(32);
                                    avatar.Masking = true;
                                    avatar.CornerRadius = 6;
                                }),
                                // Long names and activities are cut at the column's edge.
                                new Container
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Width = text_width,
                                    AutoSizeAxes = Axes.Y,
                                    Masking = true,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new TruncatingSpriteText
                                            {
                                                Text = User.Username,
                                                Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                                                MaxWidth = text_width,
                                            },
                                            status,
                                        },
                                    },
                                },
                            },
                        },
                        CreateStatusIcon().With(icon =>
                        {
                            icon.Anchor = Anchor.CentreRight;
                            icon.Origin = Anchor.CentreRight;
                            icon.Size = new Vector2(10);
                        }),
                    },
                };
            }
        }
    }
}
