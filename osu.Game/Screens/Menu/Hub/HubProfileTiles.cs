// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Scoring;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// A tile that shows a part of the local user's profile (see <see cref="HubProfileProvider"/>).
    /// </summary>
    public abstract partial class HubProfileTile : HubTile
    {
        /// <summary>
        /// The figure of a profile tile: smaller than the score tile's, which leads the hub.
        /// </summary>
        protected const float BIG_SIZE = 26;

        [Resolved]
        protected IAPIProvider Api { get; private set; } = null!;

        [Resolved]
        protected OsuGame? Game { get; private set; }

        protected HubProfileProvider Profile { get; }

        private readonly IconUsage icon;
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        protected HubProfileTile(HubProfileProvider profile, LocalisableString eyebrow, IconUsage icon)
        {
            Profile = profile;
            this.icon = icon;
            Eyebrow.Text = eyebrow.ToUpper();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            apiState.BindTo(Api.State);
            apiState.BindValueChanged(_ => Schedule(Refresh));
            BindSources();
            Refresh();
        }

        /// <summary>
        /// Calls <see cref="Refresh"/> whenever what the tile shows changes.
        /// </summary>
        protected virtual void BindSources() => Profile.User.BindValueChanged(_ => Refresh());

        protected void Refresh()
        {
            if (!ShouldShow())
            {
                ShowPlaceholder(icon, Placeholder());
                return;
            }

            ShowContent();
            Display();
        }

        protected virtual bool ShouldShow() => Profile.User.Value != null;

        protected virtual LocalisableString Placeholder() => Api.IsLoggedIn ? MenuHubStrings.ProfileUnavailable : MenuHubStrings.LogInForProfile;

        protected abstract void Display();

        protected APIUser User => Profile.User.Value!;

        protected void OpenProfile() => SetAction(() => Game?.ShowUser(Api.LocalUser.Value));

        protected static OsuSpriteText Big(string text) => new OsuSpriteText
        {
            Text = text,
            Font = ScosuMenuStyle.Display(BIG_SIZE),
        };

        protected static OsuSpriteText Sub(LocalisableString text) => new OsuSpriteText
        {
            Text = text,
            Font = ScosuMenuStyle.Body(12),
            Colour = ScosuMenuStyle.TEXT_DIM,
        };

        protected static Drawable Cover(APIBeatmapSet set) => new UpdateableOnlineBeatmapSetCover(timeBeforeLoad: 0, timeBeforeUnload: 600_000)
        {
            RelativeSizeAxes = Axes.Both,
            OnlineInfo = set,
        };

        protected static string RankLetter(ScoreRank rank) => rank switch
        {
            ScoreRank.XH or ScoreRank.X => @"SS",
            ScoreRank.SH or ScoreRank.S => @"S",
            _ => rank.ToString(),
        };
    }

    /// <summary>
    /// Total plays, with the plays of each month as bars behind.
    /// </summary>
    public partial class HubPlaysTile : HubProfileTile
    {
        public HubPlaysTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.PlayCount, FontAwesome.Solid.Play)
        {
        }

        protected override void Display()
        {
            var counts = (User.MonthlyPlayCounts ?? Array.Empty<APIUserHistoryCount>()).OrderBy(c => c.Date).TakeLast(24).ToArray();

            if (counts.Length > 0)
                SetBackground(HubGraphs.Bars(counts.Select(c => (float)c.Count).ToArray(), ScosuMenuStyle.VIOLET), 0);

            int total = User.Statistics?.PlayCount ?? 0;

            Body.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10, 0),
                Children = new Drawable[]
                {
                    Big(total.ToString(@"N0")).With(t => t.Anchor = t.Origin = Anchor.BottomLeft),
                    Sub(MenuHubStrings.PlaysTotal).With(t => t.Anchor = t.Origin = Anchor.BottomLeft),
                },
            });

            var now = DateTime.Now;
            var month = counts.LastOrDefault(c => c.Date.Year == now.Year && c.Date.Month == now.Month);

            if (month != null)
                SetFoot(FontAwesome.Solid.ChartBar, MenuHubStrings.ThisMonthPlays(month.Count.ToString(@"N0")), ScosuMenuStyle.VIOLET);

            OpenProfile();
        }
    }

    /// <summary>
    /// The user's accuracy: the figure, a bar split by their SS, S and A grades (with the counts under it), and a ring gauge
    /// with the grade that accuracy is worth in its middle. Opens the profile.
    /// </summary>
    public partial class HubAccuracyTile : HubProfileTile
    {
        private const float ring_size = 86;
        private const float ring_stroke = 7;

        public HubAccuracyTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.Accuracy, FontAwesome.Solid.Bullseye)
        {
        }

        protected override void Display()
        {
            double accuracy = User.Statistics?.Accuracy ?? 0;
            var grades = User.Statistics?.GradesCount ?? default;

            (string letter, int count, ScoreRank rank)[] parts =
            {
                (@"SS", grades.SS + (grades.SSPlus ?? 0), ScoreRank.X),
                (@"S", grades.S + (grades.SPlus ?? 0), ScoreRank.S),
                (@"A", grades.A, ScoreRank.A),
            };

            var tier = rankFor(accuracy);

            SetBackground(new Container
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Size = new Vector2(ring_size),
                X = -18,
                Children = new Drawable[]
                {
                    new CircularProgress
                    {
                        RelativeSizeAxes = Axes.Both,
                        InnerRadius = ring_stroke / ring_size,
                        Progress = 1,
                        Colour = Color4.White.Opacity(0.07f),
                    },
                    new CircularProgress
                    {
                        RelativeSizeAxes = Axes.Both,
                        InnerRadius = ring_stroke / ring_size,
                        RoundedCaps = true,
                        Progress = Math.Clamp(accuracy / 100, 0, 1),
                        Colour = ColourInfo.GradientVertical(ScosuMenuStyle.VIOLET, ScosuMenuStyle.ICE),
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = RankLetter(tier),
                                Font = ScosuMenuStyle.Display(26),
                                Colour = OsuColour.ForRank(tier),
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = MenuHubStrings.AverageGrade,
                                Font = ScosuMenuStyle.Body(9, FontWeight.SemiBold),
                                Colour = ScosuMenuStyle.TEXT_DIM,
                            },
                        },
                    },
                },
            }, 0);

            Body.Add(Big(accuracy.ToString(@"0.00", CultureInfo.InvariantCulture) + @"%"));

            int total = parts.Sum(p => p.count);

            // Clear of the ring on the right.
            var breakdown = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 7),
                Margin = new MarginPadding { Top = 4 },
                Padding = new MarginPadding { Right = ring_size + 36 },
            };

            var bar = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 5,
                Direction = FillDirection.Horizontal,
                Masking = true,
                CornerRadius = 2.5f,
            };

            if (total > 0)
            {
                foreach (var part in parts.Where(p => p.count > 0))
                {
                    bar.Add(new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = (float)part.count / total,
                        Colour = OsuColour.ForRank(part.rank),
                    });
                }
            }
            else
                bar.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White.Opacity(0.1f) });

            breakdown.Add(bar);
            breakdown.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(14, 0),
                ChildrenEnumerable = parts.Select(p => legend(p.letter, p.count, p.rank)),
            });

            Body.Add(breakdown);

            OpenProfile();
        }

        /// <summary>
        /// The grade a play with this accuracy would get (osu!'s thresholds, ignoring misses).
        /// </summary>
        private static ScoreRank rankFor(double accuracy) => accuracy switch
        {
            >= 100 => ScoreRank.X,
            > 95 => ScoreRank.S,
            > 90 => ScoreRank.A,
            > 80 => ScoreRank.B,
            > 70 => ScoreRank.C,
            _ => ScoreRank.D,
        };

        private static Drawable legend(string letter, int count, ScoreRank rank) => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(5, 0),
            Children = new Drawable[]
            {
                new Circle
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(6),
                    Colour = OsuColour.ForRank(rank),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = letter,
                    Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                    Colour = OsuColour.ForRank(rank),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = count.ToString(@"N0"),
                    Font = ScosuMenuStyle.Body(11, FontWeight.SemiBold),
                },
            },
        };
    }

    /// <summary>
    /// The rank in the user's country, with its flag, and how the global rank moved over 30 days.
    /// </summary>
    public partial class HubCountryTile : HubProfileTile
    {
        public HubCountryTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.CountryRank, FontAwesome.Solid.Flag)
        {
        }

        protected override bool ShouldShow() => base.ShouldShow() && User.Statistics?.CountryRank != null;

        protected override LocalisableString Placeholder() => Profile.User.Value != null ? MenuHubStrings.NoCountryRank : base.Placeholder();

        protected override void Display()
        {
            Body.Add(Big($@"#{User.Statistics!.CountryRank:N0}"));
            Body.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6, 0),
                Children = new Drawable[]
                {
                    new UpdateableFlag(User.CountryCode)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(18, 12),
                    },
                    Sub(User.CountryCode.GetDescription()).With(t =>
                    {
                        t.Anchor = Anchor.CentreLeft;
                        t.Origin = Anchor.CentreLeft;
                    }),
                },
            });

            var history = User.Statistics.RankHistory?.Data;

            // The history has a rank per day, the last one being today's.
            if (history != null && history.Length > 30 && history[^1] > 0 && history[^31] > 0)
            {
                int change = history[^31] - history[^1];

                if (change != 0)
                {
                    SetFoot(change > 0 ? FontAwesome.Solid.ArrowUp : FontAwesome.Solid.ArrowDown,
                        MenuHubStrings.RankChange(Math.Abs(change).ToString(@"N0")),
                        change > 0 ? ScosuMenuStyle.VIOLET : ScosuMenuStyle.TEXT_DIM);
                }
            }

            OpenProfile();
        }
    }

    /// <summary>
    /// How many medals the user has, and when the last one was earned.
    /// </summary>
    public partial class HubMedalsTile : HubProfileTile
    {
        public HubMedalsTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.Medals, FontAwesome.Solid.Award)
        {
        }

        protected override bool ShouldShow() => base.ShouldShow() && User.Achievements?.Length > 0;

        protected override LocalisableString Placeholder() => Profile.User.Value != null ? MenuHubStrings.NoMedals : base.Placeholder();

        protected override void Display()
        {
            Body.Add(Big(User.Achievements!.Length.ToString(@"N0")));
            Body.Add(Sub(MenuHubStrings.MedalsCount));

            var latest = User.Achievements.Max(a => a.AchievedAt);
            SetFoot(FontAwesome.Solid.Award, MenuHubStrings.LatestMedal(latest.ToLocalTime().ToString(@"d MMM yyyy", CultureInfo.CurrentCulture)));

            OpenProfile();
        }
    }

    /// <summary>
    /// The user's best play: its beatmap's cover behind, its pp or score, accuracy and mods. Opens the beatmap.
    /// </summary>
    public partial class HubBestTile : HubProfileTile
    {
        public HubBestTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.BestPlay, FontAwesome.Solid.Trophy)
        {
        }

        protected override void BindSources() => Profile.Best.BindValueChanged(_ => Refresh());

        protected override bool ShouldShow() => Profile.Best.Value?.Count > 0;

        protected override LocalisableString Placeholder() => Profile.User.Value != null || Profile.Best.Value != null ? MenuHubStrings.NoBest : base.Placeholder();

        protected override void Display()
        {
            var score = Profile.Best.Value![0];
            var set = score.Beatmap?.BeatmapSet;

            if (set != null)
                SetCoverBackground(Cover(set));

            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = set != null ? new RomanisableString(set.TitleUnicode, set.Title) : $@"#{score.BeatmapID}",
                Font = ScosuMenuStyle.Display(20),
            });
            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = score.Beatmap?.DifficultyName ?? string.Empty,
                Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold),
                Colour = ScosuMenuStyle.TEXT_DIM,
            });

            string value = score.PP is double pp && pp > 0 ? $@"{pp:N0}pp" : score.TotalScore.ToString(@"N0");
            string mods = score.Mods.Length > 0 ? @" · " + string.Join(@",", score.Mods.Select(m => m.Acronym)) : string.Empty;

            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = $@"{value} · {score.Accuracy * 100:0.00}%{mods}",
                Font = ScosuMenuStyle.Body(14, FontWeight.Bold),
                Colour = ScosuMenuStyle.VIOLET,
            });

            SetFoot(FontAwesome.Solid.Trophy, MenuHubStrings.OpenBeatmap);
            SetAction(() => Game?.ShowBeatmap(score.BeatmapID));
        }
    }

    /// <summary>
    /// The user's last plays, newest first: each with its grade, cover, beatmap, difficulty and mods, accuracy, pp (or score)
    /// and when it was set. Each opens its beatmap. As many as fit are shown, with their average accuracy at the foot.
    /// </summary>
    public partial class HubRecentTile : HubProfileTile
    {
        /// <summary>
        /// The shortest a row gets: the list shows as many rows as fit at this height, stretched to fill it exactly.
        /// </summary>
        private const float min_row_height = 30;

        /// <summary>
        /// Where the list starts: under the eyebrow.
        /// </summary>
        private const float list_top = 36;

        private readonly HubScrollContainer list;
        private readonly FillFlowContainer<RecentRow> rows;

        public HubRecentTile(HubProfileProvider profile)
            : base(profile, MenuHubStrings.RecentPlays, FontAwesome.Solid.History)
        {
            // Under the eyebrow, scrolled when the plays don't all fit. Its height is a whole number of rows (see UpdateAfterChildren).
            Add(list = new HubScrollContainer
            {
                RelativeSizeAxes = Axes.X,
                Y = list_top,
                // The rows' own padding (8) brings their text in line with the eyebrow.
                Padding = new MarginPadding { Horizontal = CONTENT_PADDING - 8 },
                Alpha = 0,
                Child = rows = new FillFlowContainer<RecentRow>
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    // Clear of the scrollbar.
                    Padding = new MarginPadding { Right = 8 },
                },
            });
        }

        protected override void BindSources() => Profile.Recent.BindValueChanged(_ => Refresh());

        protected override bool ShouldShow() => Profile.Recent.Value?.Count > 0;

        protected override LocalisableString Placeholder() => Profile.User.Value != null || Profile.Recent.Value != null ? MenuHubStrings.NoRecent : base.Placeholder();

        protected override void Display()
        {
            var scores = Profile.Recent.Value!.Take(HubProfileProvider.RECENT_COUNT).ToArray();

            // The latest play's cover, faint, behind the list.
            if (scores[0].Beatmap?.BeatmapSet is APIBeatmapSet cover)
                SetBackground(Cover(cover), 0.85f);

            rows.ChildrenEnumerable = scores.Select(score => new RecentRow(score, min_row_height) { Action = () => Game?.ShowBeatmap(score.BeatmapID) });
            list.ScrollToStart(false);
            list.Show();

            if (scores.Length > 1)
            {
                double average = scores.Average(s => s.Accuracy) * 100;
                SetEyebrowDetail(MenuHubStrings.RecentAverage($@"{average:0.00}%", scores.Length.ToString(CultureInfo.CurrentCulture)));
            }

            SetFoot(null, default);

            // The rows open their own beatmaps.
            SetAction(null);
        }

        protected override void OnPlaceholderShown()
        {
            list.Hide();
            rows.Clear();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // Only whole rows show, stretched to fill the list: a row cut by the tile's edge would look like it overlaps it,
            // and a gap under the last one like something hides the rest.
            float available = Math.Max(min_row_height, DrawHeight - list_top - 8);
            float rowHeight = available / Math.Max(1, (int)(available / min_row_height));

            list.Height = available;

            foreach (var row in rows)
            {
                if (row.Height != rowHeight)
                    row.Height = rowHeight;
            }
        }

        private partial class RecentRow : OsuClickableContainer
        {
            private readonly SoloScoreInfo score;
            private Box hover = null!;

            public RecentRow(SoloScoreInfo score, float height)
            {
                this.score = score;

                RelativeSizeAxes = Axes.X;
                Height = height;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                var beatmap = score.Beatmap;
                var set = beatmap?.BeatmapSet;

                var details = new List<string>();

                if (!string.IsNullOrEmpty(beatmap?.DifficultyName))
                    details.Add(beatmap.DifficultyName);
                if (beatmap?.StarRating > 0)
                    details.Add($@"{beatmap.StarRating:0.00}★");
                if (score.Mods.Length > 0)
                    details.Add(string.Join(@",", score.Mods.Select(m => m.Acronym)));

                string value = score.PP is double pp && pp > 0 ? $@"{pp:N0}pp" : ScosuMenuFormatting.CompactScore(score.TotalScore);

                Masking = true;
                CornerRadius = 8;

                Children = new Drawable[]
                {
                    hover = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 8 },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 36),
                            new Dimension(GridSizeMode.Absolute, 54),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(30, 15),
                                    Child = new DrawableRank(score.Rank),
                                },
                                new Container
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(44, 26),
                                    Masking = true,
                                    CornerRadius = 5,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ScosuMenuStyle.VIOLET.Opacity(0.25f),
                                        },
                                        set != null ? Cover(set) : Empty(),
                                    },
                                },
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Padding = new MarginPadding { Right = 12 },
                                    Children = new Drawable[]
                                    {
                                        new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = set != null ? new RomanisableString(set.TitleUnicode, set.Title) : $@"#{score.BeatmapID}",
                                            Font = ScosuMenuStyle.Body(13, FontWeight.SemiBold),
                                            // A failed play is dimmer.
                                            Colour = score.Rank != ScoreRank.F ? Color4.White : ScosuMenuStyle.TEXT_DIM,
                                        },
                                        new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = string.Join(@" · ", details),
                                            Font = ScosuMenuStyle.Body(11),
                                            Colour = ScosuMenuStyle.TEXT_DIM,
                                        },
                                    },
                                },
                                column($@"{score.Accuracy * 100:0.00}%", ScosuMenuStyle.Body(13, FontWeight.SemiBold), Color4.White, 62),
                                column(value, ScosuMenuStyle.Body(13, FontWeight.Bold), ScosuMenuStyle.VIOLET, 66),
                                new Container
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Width = 84,
                                    AutoSizeAxes = Axes.Y,
                                    Child = new DrawableDate(score.EndedAt, 11, false)
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Colour = ScosuMenuStyle.TEXT_DIM,
                                    },
                                },
                            },
                        },
                    },
                };
            }

            private static Drawable column(string text, FontUsage font, Color4 colour, float width) => new Container
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Width = width,
                AutoSizeAxes = Axes.Y,
                Child = new OsuSpriteText
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Text = text,
                    Font = font,
                    Colour = colour,
                },
            };

            protected override bool OnHover(HoverEvent e)
            {
                hover.FadeTo(0.07f, HOVER_DURATION, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.FadeOut(HOVER_DURATION, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
