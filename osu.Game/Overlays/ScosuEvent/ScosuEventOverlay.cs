// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Select;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.ScosuEvent
{
    /// <summary>
    /// The scosu! event's page: its countdown, the local user's standing and objectives, the ranking by ranked score gained
    /// and the profile titles it gives (scosu-rust's services/events/).
    /// </summary>
    public partial class ScosuEventOverlay : OnlineOverlay<ScosuEventHeader>
    {
        private const float horizontal_padding = 40;

        /// <summary>
        /// Rows under the podium; the local user's row is added below them when they rank lower.
        /// </summary>
        private const int rows_shown = 5;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        private GetScosuEventRequest? request;
        private APIScosuEvent? shown;
        private ScheduledDelegate? ticker;

        private readonly Container body;

        private readonly List<OsuSpriteText> countdownValues = new List<OsuSpriteText>();

        public ScosuEventOverlay()
            : base(OverlayColourScheme.Purple)
        {
            Child = body = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };
        }

        protected override ScosuEventHeader CreateHeader() => new ScosuEventHeader();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The API's state changes on its own thread.
            apiState.BindTo(API.State);
            apiState.BindValueChanged(_ => Schedule(() =>
            {
                if (State.Value == Visibility.Visible)
                    fetch();
            }));
        }

        protected override void PopIn()
        {
            base.PopIn();
            fetch();
        }

        protected override void PopOutComplete()
        {
            base.PopOutComplete();

            request?.Cancel();
            request = null;
        }

        private void fetch()
        {
            request?.Cancel();
            request = null;

            if (!API.Endpoints.RanksByStableScore || apiState.Value != APIState.Online)
                return;

            // Only the first load covers the page; a refresh keeps the previous one meanwhile.
            if (shown == null)
                Loading.Show();

            var req = request = new GetScosuEventRequest();

            req.Success += response =>
            {
                if (req != request)
                    return;

                Loading.Hide();
                display(response.Event);
            };
            req.Failure += _ =>
            {
                if (req == request)
                    Loading.Hide();
            };

            API.Queue(req);
        }

        private void display(APIScosuEvent? ev)
        {
            ticker?.Cancel();
            ticker = null;
            countdownValues.Clear();
            shown = ev;

            if (ev == null)
            {
                body.Child = new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = ScosuEventStrings.None,
                    Font = OsuFont.GetFont(size: 18),
                    Margin = new MarginPadding { Vertical = 60 },
                };
                return;
            }

            var accent = EventDisplay.Accent(ev);
            var accentText = EventDisplay.Lifted(accent);

            body.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new[]
                {
                    createBanner(ev, accent, accentText),
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 3,
                        Colour = accent,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = horizontal_padding, Vertical = 16 },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Relative, 0.575f),
                            new Dimension(GridSizeMode.Absolute, 18),
                            new Dimension(),
                        },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            new[]
                            {
                                column(
                                    createStanding(ev, accent),
                                    createObjectives(ev, accent, accentText),
                                    createAbout(ev, accentText)),
                                Empty(),
                                column(
                                    createRanking(ev, accent, accentText),
                                    createRewards(ev, accentText)),
                            },
                        },
                    },
                },
            };

            updateCountdown();
            ticker = Scheduler.AddDelayed(updateCountdown, 1000, true);
        }

        #region The banner

        private Drawable createBanner(APIScosuEvent ev, Color4 accent, Color4 accentText)
        {
            var banner = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 230,
                Masking = true,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourProvider.Background4,
                },
            };

            if (ev.Appearance.BannerUrl is string url)
                banner.Add(EventDisplay.Picture(url, ev.Appearance.BannerDim));

            var chips = new List<string> { $@"{EventDisplay.Day(ev.StartsAt)} – {EventDisplay.Day(ev.EndsAt)}" };
            chips.AddRange(ev.Rules);

            banner.Add(new GridContainer
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Horizontal = horizontal_padding, Bottom = 24 },
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 40),
                    new Dimension(GridSizeMode.Absolute, 330),
                },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Children = new[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = ScosuEventStrings.Label.ToUpper(),
                                            Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold),
                                            Colour = accentText,
                                        },
                                        EventDisplay.StatusPill(ev).With(d =>
                                        {
                                            d.Anchor = Anchor.CentreLeft;
                                            d.Origin = Anchor.CentreLeft;
                                        }),
                                    },
                                },
                                new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 46, weight: FontWeight.SemiBold))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = ev.Title,
                                },
                                ev.Tagline is string tagline
                                    ? new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 15))
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Text = tagline,
                                        Colour = colours.GrayE,
                                    }
                                    : Empty(),
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Full,
                                    Spacing = new Vector2(8),
                                    Margin = new MarginPadding { Top = 6 },
                                    ChildrenEnumerable = chips
                                                         .Select(text => (LocalisableString)text)
                                                         .Append(ScosuEventStrings.Players(ev.PlayerCount.ToLocalisableString(@"N0")))
                                                         .Select(text => chip(text, 12, Color4.Black.Opacity(0.45f), 8)),
                                },
                            },
                        },
                        Empty(),
                        createCountdown(ev, accent),
                    },
                },
            });

            return banner;
        }

        private Drawable createCountdown(APIScosuEvent ev, Color4 accent)
        {
            if (EventDisplay.CountdownTarget(ev) is not DateTimeOffset target)
            {
                return new OsuSpriteText
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Text = ScosuEventStrings.EndedOn(EventDisplay.DayAndTime(ev.EndsAt)),
                    Font = OsuFont.GetFont(size: 15),
                };
            }

            var units = new[] { ScosuEventStrings.Days, ScosuEventStrings.Hours, ScosuEventStrings.Minutes, ScosuEventStrings.Seconds };

            var flow = new FillFlowContainer
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new[]
                        {
                            heading(EventDisplay.CountdownLabel(ev).ToUpper(), colours.Yellow, 12),
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Text = EventDisplay.DayAndTime(target),
                                Font = OsuFont.GetFont(size: 12),
                                Colour = colours.GrayD,
                            },
                        },
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            units.Select(unit => (Drawable)new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding { Horizontal = 4 },
                                Child = new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Masking = true,
                                    CornerRadius = 10,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Black,
                                            Alpha = 0.5f,
                                        },
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Padding = new MarginPadding { Vertical = 8 },
                                            Children = new Drawable[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    Font = OsuFont.GetFont(size: 30, weight: FontWeight.Light, fixedWidth: true),
                                                }.With(countdownValues.Add),
                                                new OsuSpriteText
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    Text = unit.ToUpper(),
                                                    Font = OsuFont.GetFont(size: 10, weight: FontWeight.Bold),
                                                    Colour = colours.GrayC,
                                                },
                                            },
                                        },
                                    },
                                },
                            }).ToArray(),
                        },
                    },
                },
            };

            if (ev.Status == ScosuEventStatus.Live)
            {
                double length = (ev.EndsAt - ev.StartsAt).TotalSeconds;
                double elapsed = (DateTimeOffset.Now - ev.StartsAt).TotalSeconds;
                int days = Math.Max(1, (int)Math.Ceiling(length / 86400));
                int day = Math.Clamp((int)(elapsed / 86400) + 1, 1, days);

                flow.Add(new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 4),
                    Children = new[]
                    {
                        EventDisplay.ProgressBar(accent, elapsed / length, 6),
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = ScosuEventStrings.Started(EventDisplay.Day(ev.StartsAt)),
                                    Font = OsuFont.GetFont(size: 11),
                                    Colour = colours.GrayD,
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Text = ScosuEventStrings.DayOf(day, days),
                                    Font = OsuFont.GetFont(size: 11, weight: FontWeight.SemiBold),
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    Text = ScosuEventStrings.Ends(EventDisplay.Day(ev.EndsAt)),
                                    Font = OsuFont.GetFont(size: 11),
                                    Colour = colours.GrayD,
                                },
                            },
                        },
                    },
                });
            }

            return flow;
        }

        private void updateCountdown()
        {
            if (shown == null || EventDisplay.CountdownTarget(shown) is not DateTimeOffset target || countdownValues.Count != 4)
                return;

            var left = target - DateTimeOffset.Now;

            if (left < TimeSpan.Zero)
                left = TimeSpan.Zero;

            countdownValues[0].Text = ((int)left.TotalDays).ToString(@"00");
            countdownValues[1].Text = left.Hours.ToString(@"00");
            countdownValues[2].Text = left.Minutes.ToString(@"00");
            countdownValues[3].Text = left.Seconds.ToString(@"00");

            // The event just started or ended: the server knows what comes next.
            if (left == TimeSpan.Zero)
            {
                ticker?.Cancel();
                Scheduler.AddDelayed(fetch, 5000);
            }
        }

        #endregion

        #region The left column

        private Drawable createStanding(APIScosuEvent ev, Color4 accent)
        {
            if (ev.Me is not APIScosuEvent.Standing me)
                return Empty();

            Drawable rank = me.Rank is int position
                ? new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = LocalisableString.Interpolate($@"#{position.ToLocalisableString(@"N0")}"),
                            Font = OsuFont.GetFont(size: 28, weight: FontWeight.Light),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = ScosuEventStrings.Of(ev.PlayerCount.ToLocalisableString(@"N0")),
                            Font = OsuFont.GetFont(size: 14),
                            Colour = colours.GrayC,
                            Margin = new MarginPadding { Bottom = 4 },
                        },
                    },
                }
                : new OsuSpriteText
                {
                    Text = ScosuEventStrings.NotRanked,
                    Font = OsuFont.GetFont(size: 20, weight: FontWeight.Light),
                    Margin = new MarginPadding { Top = 6 },
                };

            Drawable next;

            if (me.Next is APIScoreSummary.NextRank toPass)
            {
                double fraction = (double)me.RankedScore / (me.RankedScore + toPass.RankedScoreNeeded);

                next = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 4),
                    Children = new[]
                    {
                        heading(ScosuEventStrings.ToPass(toPass.Username, toPass.Rank.ToLocalisableString(@"N0")).ToUpper(), colours.Yellow),
                        new OsuSpriteText
                        {
                            Text = ScosuEventStrings.More(toPass.RankedScoreNeeded.ToLocalisableString(@"N0")),
                            Font = OsuFont.GetFont(size: 16, fixedWidth: true),
                        },
                        EventDisplay.ProgressBar(accent, fraction),
                    },
                };
            }
            else
            {
                next = new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 14))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = me.Rank == 1 ? ScosuEventStrings.Leading : ScosuEventStrings.NotRankedHint,
                    Colour = me.Rank == 1 ? Color4.White : colours.GrayC,
                };
            }

            return card(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Absolute, 45),
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Absolute, 45),
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, 22),
                    new Dimension(GridSizeMode.AutoSize),
                },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    new[]
                    {
                        stat(ScosuEventStrings.YourRank.ToUpper(), rank),
                        separator(),
                        stat(ScosuEventStrings.ScoreGained.ToUpper(), new OsuSpriteText
                        {
                            Text = me.RankedScore.ToLocalisableString(@"N0"),
                            Font = OsuFont.GetFont(size: 28, weight: FontWeight.Light, fixedWidth: true),
                        }),
                        separator(),
                        next.With(d =>
                        {
                            d.Anchor = Anchor.CentreLeft;
                            d.Origin = Anchor.CentreLeft;
                        }),
                        Empty(),
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                            Children = new Drawable[]
                            {
                                new RoundedButton
                                {
                                    Width = 140,
                                    Height = 32,
                                    Text = ScosuEventStrings.PlayNow,
                                    BackgroundColour = accent,
                                    Action = playNow,
                                },
                                new RoundedButton
                                {
                                    Width = 140,
                                    Height = 28,
                                    Text = ScosuEventStrings.OnTheWebsite,
                                    BackgroundColour = ColourProvider.Background3,
                                    Action = openWebsite,
                                },
                            },
                        },
                    },
                },
            });

            Drawable separator() => new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 1,
                Height = 48,
                Colour = Color4.White,
                Alpha = 0.1f,
            };
        }

        private Drawable createObjectives(APIScosuEvent ev, Color4 accent, Color4 accentText)
        {
            if (ev.Objectives.Count == 0)
                return Empty();

            var progress = ev.Me?.Objectives;

            return card(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new[]
                {
                    titleRow(ScosuEventStrings.Objectives, accentText,
                        ev.Me != null ? ScosuEventStrings.Completed(ev.Me.Completed, ev.Objectives.Count) : default, Color4.White, FontWeight.SemiBold),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Spacing = new Vector2(0, 10),
                        // Two by two: each half pads its inner side, so the gap between the cards is 10.
                        ChildrenEnumerable = ev.Objectives.Select((objective, index) => new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Width = 0.5f,
                            AutoSizeAxes = Axes.Y,
                            Padding = index % 2 == 0 ? new MarginPadding { Right = 5 } : new MarginPadding { Left = 5 },
                            Child = createObjective(objective, progress?.ElementAtOrDefault(index), accent, accentText),
                        }),
                    },
                },
            });
        }

        private Drawable createObjective(APIScosuEvent.Objective objective, APIScosuEvent.Progress? mine, Color4 accent, Color4 accentText)
        {
            bool done = mine?.Done == true;
            double fraction = mine != null && objective.Target > 0 ? (double)mine.Value / objective.Target : 0;

            var amount = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5, 0),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Text = mine?.Text ?? objective.TargetText,
                        Font = OsuFont.GetFont(size: 18, weight: FontWeight.Light),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Text = mine != null ? $@"/ {objective.TargetText}" : string.Empty,
                        Font = OsuFont.GetFont(size: 12),
                        Colour = colours.GrayC,
                        Margin = new MarginPadding { Bottom = 2 },
                    },
                },
            };

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 12,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0.05f,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 8),
                        Padding = new MarginPadding { Horizontal = 14, Vertical = 12 },
                        Children = new[]
                        {
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute, 44),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize),
                                },
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize, minSize: 34) },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        new Container
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Size = new Vector2(34),
                                            Masking = true,
                                            CornerRadius = 9,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = done ? accent.Opacity(0.25f) : Color4.White.Opacity(0.08f),
                                                },
                                                new SpriteIcon
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Size = new Vector2(15),
                                                    Icon = iconOf(objective.Kind),
                                                    Colour = done ? accentText : colours.GrayD,
                                                },
                                            },
                                        },
                                        new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 13, weight: FontWeight.Medium))
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Text = objective.Label,
                                        },
                                        done
                                            ? new CircularContainer
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Size = new Vector2(20),
                                                Masking = true,
                                                Margin = new MarginPadding { Left = 8 },
                                                Children = new Drawable[]
                                                {
                                                    new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Colour = accent,
                                                    },
                                                    new SpriteIcon
                                                    {
                                                        Anchor = Anchor.Centre,
                                                        Origin = Anchor.Centre,
                                                        Size = new Vector2(10),
                                                        Icon = FontAwesome.Solid.Check,
                                                    },
                                                },
                                            }
                                            : Empty(),
                                    },
                                },
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    amount,
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.BottomRight,
                                        Origin = Anchor.BottomRight,
                                        Text = mine == null ? string.Empty : done ? ScosuEventStrings.Done : $@"{Math.Floor(Math.Clamp(fraction, 0, 1) * 100)}%",
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                        Colour = done ? accentText : colours.GrayD,
                                        Margin = new MarginPadding { Bottom = 2 },
                                    },
                                },
                            },
                            mine != null ? EventDisplay.ProgressBar(accent, fraction) : Empty(),
                        },
                    },
                },
            };
        }

        private static IconUsage iconOf(string kind) => kind switch
        {
            @"ranked_score_gained" or @"total_score_gained" => FontAwesome.Solid.ChartLine,
            @"plays" => FontAwesome.Solid.Play,
            @"playtime" => FontAwesome.Regular.Clock,
            @"full_combos" => FontAwesome.Solid.Star,
            @"ss_ranks" or @"s_ranks" => FontAwesome.Solid.Award,
            @"distinct_beatmaps" => FontAwesome.Solid.LayerGroup,
            _ => FontAwesome.Solid.Flag,
        };

        private Drawable createAbout(APIScosuEvent ev, Color4 accentText)
        {
            if (string.IsNullOrWhiteSpace(ev.Description) && ev.Rules.Count == 0)
                return Empty();

            return card(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Children = new[]
                {
                    heading(ScosuEventStrings.About.ToUpper(), accentText, 12),
                    string.IsNullOrWhiteSpace(ev.Description)
                        ? Empty()
                        : new OsuMarkdownContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = ev.Description,
                        },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Spacing = new Vector2(6),
                        ChildrenEnumerable = ev.Rules.Select(rule => chip(rule, 11, Color4.White.Opacity(0.07f), 7)),
                    },
                },
            });
        }

        #endregion

        #region The right column

        private Drawable createRanking(APIScosuEvent ev, Color4 accent, Color4 accentText)
        {
            var content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Child = titleRow(ScosuEventStrings.Ranking, accentText, ScosuEventStrings.RankingHint, colours.GrayC, FontWeight.Regular),
            };

            if (ev.Ranking.Count == 0)
            {
                content.Add(new OsuSpriteText
                {
                    Text = ScosuEventStrings.NoPlayers,
                    Font = OsuFont.GetFont(size: 14),
                    Colour = colours.GrayC,
                    Margin = new MarginPadding { Vertical = 8 },
                });
                return card(content);
            }

            // Second, first, third: the winner in the middle.
            var podium = new[] { (place: 2, height: 44f, colour: Color4Extensions.FromHex(@"d8dce6")), (place: 1, height: 60f, colour: colours.Yellow), (place: 3, height: 32f, colour: Color4Extensions.FromHex(@"e8b08a")) };

            content.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Margin = new MarginPadding { Top = 10 },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                Content = new[]
                {
                    podium.Select(spot => ev.Ranking.ElementAtOrDefault(spot.place - 1) is APIScosuEvent.Rank row
                        ? createPodiumSpot(row, spot.place, spot.height, spot.colour)
                        : Empty()).ToArray(),
                },
            });

            content.Add(new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 1,
                Colour = Color4.White,
                Alpha = 0.1f,
                Margin = new MarginPadding { Bottom = 4 },
            });

            int me = API.LocalUser.Value.Id;
            var rows = ev.Ranking.Skip(3).Take(rows_shown).ToList();

            if (ev.Ranking.FirstOrDefault(r => r.User.Id == me) is APIScosuEvent.Rank mine && mine.Position > 3 + rows_shown)
                rows.Add(mine);

            content.AddRange(rows.Select(row => createRow(row, row.User.Id == me, accent)));

            if (ev.PlayerCount > 3 + rows_shown)
            {
                content.Add(new OsuHoverContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Top = 6 },
                    IdleColour = accentText,
                    Action = openWebsite,
                    Child = new OsuSpriteText
                    {
                        Text = ScosuEventStrings.AllPlayers(ev.PlayerCount.ToLocalisableString(@"N0")),
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                    },
                });
            }

            return card(content);
        }

        private Drawable createPodiumSpot(APIScosuEvent.Rank row, int place, float height, Color4 colour)
        {
            float avatarSize = place == 1 ? 52 : 44;

            return new FillFlowContainer
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
                Padding = new MarginPadding { Horizontal = 5 },
                Children = new Drawable[]
                {
                    new UpdateableAvatar(row.User, showUserPanelOnHover: true)
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(avatarSize),
                        Masking = true,
                        CornerRadius = 10,
                    },
                    new TruncatingSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = row.User.Username,
                        Font = OsuFont.GetFont(size: place == 1 ? 14 : 13, weight: FontWeight.SemiBold),
                        MaxWidth = 140,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = row.RankedScore.ToLocalisableString(@"N0"),
                        Font = OsuFont.GetFont(size: 12, fixedWidth: true),
                        Colour = colours.GrayD,
                    },
                    // Rounded at the top only: the block's rounded bottom is cut off.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = height,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = height + 10,
                                Masking = true,
                                CornerRadius = 10,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colour.Opacity(0.17f),
                                },
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = place.ToString(),
                                Font = OsuFont.GetFont(size: place == 1 ? 24 : 20, weight: FontWeight.Bold),
                                Colour = colour,
                            },
                        },
                    },
                },
            };
        }

        private Drawable createRow(APIScosuEvent.Rank row, bool isMe, Color4 accent) => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 30,
            Masking = true,
            CornerRadius = 8,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = accent,
                    Alpha = isMe ? 0.28f : 0,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 10 },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 44),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 80),
                        new Dimension(GridSizeMode.Absolute, 110),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = LocalisableString.Interpolate($@"#{row.Position.ToLocalisableString(@"N0")}"),
                                Font = OsuFont.GetFont(size: 13, weight: FontWeight.SemiBold),
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8, 0),
                                Children = new Drawable[]
                                {
                                    new UpdateableAvatar(row.User, showUserPanelOnHover: true)
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(20),
                                        Masking = true,
                                        CornerRadius = 5,
                                    },
                                    new TruncatingSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = row.User.Username,
                                        Font = OsuFont.GetFont(size: 13, weight: isMe ? FontWeight.SemiBold : FontWeight.Regular),
                                        MaxWidth = 160,
                                    },
                                },
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = ScosuEventStrings.Plays(row.PlayCount.ToLocalisableString(@"N0")),
                                Font = OsuFont.GetFont(size: 13),
                                Colour = colours.GrayC,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = row.RankedScore.ToLocalisableString(@"N0"),
                                Font = OsuFont.GetFont(size: 13, fixedWidth: true),
                            },
                        },
                    },
                },
            },
        };

        private Drawable createRewards(APIScosuEvent ev, Color4 accentText)
        {
            if (ev.Rewards.Count == 0)
                return Empty();

            var user = API.LocalUser.Value;

            return card(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new[]
                {
                    titleRow(ScosuEventStrings.Rewards, accentText, ScosuEventStrings.RewardsHint, colours.GrayC, FontWeight.Regular),
                }.Concat(ev.Rewards.Select((reward, index) =>
                {
                    var status = ev.Me?.Rewards.ElementAtOrDefault(index);
                    bool given = status?.Given == true;
                    var statusText = status == null
                        ? default
                        : given
                            ? ScosuEventStrings.Earned
                            : status.Met && ev.Status == ScosuEventStatus.Live
                                ? LocalisableString.Interpolate($@"{ScosuEventStrings.OnTrack} · {status.StatusText}")
                                : status.StatusText;

                    // How the title would look on the user's profile.
                    return (Drawable)new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 46),
                            new Dimension(GridSizeMode.AutoSize, minSize: 130),
                            new Dimension(GridSizeMode.Absolute, 12),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            new[]
                            {
                                new UpdateableAvatar(user, isInteractive: false)
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(34),
                                    Masking = true,
                                    CornerRadius = 8,
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
                                            Text = user.Username,
                                            Font = OsuFont.GetFont(size: 13, weight: FontWeight.SemiBold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = reward.Title,
                                            Font = OsuFont.GetFont(size: 12, italics: true),
                                            Colour = EventDisplay.Lifted(EventDisplay.RewardColour(ev, reward), 0.3f),
                                        },
                                    },
                                },
                                Empty(),
                                new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 12))
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = reward.Condition,
                                    Colour = colours.GrayD,
                                },
                                status == null
                                    ? Empty()
                                    : chip(statusText, 11, Color4.White.Opacity(0.08f), 8, FontWeight.SemiBold).With(d =>
                                    {
                                        d.Anchor = Anchor.CentreRight;
                                        d.Origin = Anchor.CentreRight;
                                        d.Margin = new MarginPadding { Left = 8 };
                                        d.Colour = given ? colours.Lime1 : Color4.White;
                                    }),
                            },
                        },
                    };
                })).ToArray(),
            });
        }

        #endregion

        #region Shared pieces

        private static Drawable column(params Drawable[] cards) => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 14),
            Children = cards,
        };

        private static Drawable card(Drawable content) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Masking = true,
            CornerRadius = 15,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.3f,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 18, Vertical = 14 },
                    Child = content,
                },
            },
        };

        private Drawable titleRow(LocalisableString title, Color4 titleColour, LocalisableString aside, Color4 asideColour, FontWeight asideWeight) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                heading(title.ToUpper(), titleColour, 12),
                new OsuSpriteText
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Text = aside,
                    Font = OsuFont.GetFont(size: 12, weight: asideWeight),
                    Colour = asideColour,
                },
            },
        };

        private Drawable stat(LocalisableString title, Drawable value) => new FillFlowContainer
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Children = new Drawable[] { heading(title, colours.Yellow), value },
        };

        private static OsuSpriteText heading(LocalisableString text, Color4 colour, float size = 11) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.GetFont(size: size, weight: FontWeight.Bold),
            Colour = colour,
        };

        private static Drawable chip(LocalisableString text, float size, Color4 background, float radius, FontWeight weight = FontWeight.Medium) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = radius,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = background,
                },
                new OsuSpriteText
                {
                    Text = text,
                    Font = OsuFont.GetFont(size: size, weight: weight),
                    Margin = new MarginPadding { Horizontal = 10, Vertical = 4 },
                },
            },
        };

        #endregion

        private void playNow()
        {
            Hide();
            game?.PerformFromScreen(screen => screen.Push(new SoloSongSelect()), new[] { typeof(MainMenu) });
        }

        private void openWebsite() => game?.OpenUrlExternally(@"/event");

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            request = null;

            base.Dispose(isDisposing);
        }
    }
}
