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
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.ScosuEvent;
using osuTK;

namespace osu.Game.Screens.Menu.Hub
{
    /// <summary>
    /// The scosu event: its name, a countdown to its start or end and what to do first. Opens the event overlay.
    /// </summary>
    public partial class HubEventTile : HubTile
    {
        [Resolved]
        private ScosuEventProvider? provider { get; set; }

        [Resolved]
        private ScosuEventOverlay? eventOverlay { get; set; }

        private readonly IBindable<APIScosuEvent?> currentEvent = new Bindable<APIScosuEvent?>();

        private APIScosuEvent? shown;
        private CountBox[] boxes = Array.Empty<CountBox>();

        public HubEventTile()
            : base(ScosuMenuStyle.ICE)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (provider != null)
                currentEvent.BindTo(provider.Event);

            currentEvent.BindValueChanged(e => refresh(e.NewValue), true);

            Scheduler.AddDelayed(tick, 1000, true);
        }

        private void refresh(APIScosuEvent? ev)
        {
            shown = ev;
            boxes = Array.Empty<CountBox>();

            if (ev == null)
            {
                Eyebrow.Text = ScosuEventStrings.Label.ToUpper();
                ShowPlaceholder(ScosuEventStrings.None);
                return;
            }

            Eyebrow.Text = LocalisableString.Interpolate($@"{ScosuEventStrings.Label} · {EventDisplay.StatusText(ev.Status)}").ToUpper();

            string description = string.IsNullOrWhiteSpace(ev.Tagline) ? ev.Description : ev.Tagline;

            Body.Clear();
            Body.Add(new TruncatingSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = ev.Title,
                Font = ScosuMenuStyle.Display(20),
            });
            Body.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 48,
                Masking = true,
                Child = new OsuTextFlowContainer(t =>
                {
                    t.Font = ScosuMenuStyle.Body(12);
                    t.Colour = ScosuMenuStyle.TEXT_DIM;
                })
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = description,
                },
            });

            if (EventDisplay.CountdownTarget(ev) != null)
            {
                boxes = new[] { new CountBox(), new CountBox(), new CountBox() };

                Body.Add(new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Margin = new MarginPadding { Top = 6 },
                    Children = boxes,
                });
            }

            SetFoot(FontAwesome.Solid.Flag, goal(ev));
            SetAction(() => eventOverlay?.Show());
            tick();
        }

        /// <summary>
        /// The objective to work on: the next one that isn't done when the user takes part, else the first.
        /// </summary>
        private static LocalisableString goal(APIScosuEvent ev)
        {
            if (ev.Objectives.Count == 0)
                return default;

            if (ev.Me != null && ev.Me.Objectives.Count == ev.Objectives.Count)
            {
                int next = ev.Me.Objectives.FindIndex(p => !p.Done);

                return next < 0
                    ? ScosuEventStrings.AllObjectivesDone
                    : ScosuEventStrings.NextObjective(ev.Objectives[next].Label, $@"{ev.Me.Objectives[next].Text} / {ev.Objectives[next].TargetText}");
            }

            return MenuHubStrings.FirstGoal(ev.Objectives[0].Label);
        }

        private void tick()
        {
            if (shown == null || boxes.Length == 0 || EventDisplay.CountdownTarget(shown) is not DateTimeOffset target)
                return;

            var left = target - DateTimeOffset.Now;

            if (left < TimeSpan.Zero)
                left = TimeSpan.Zero;

            // Days are only worth counting while there are some.
            if (left.TotalDays >= 1)
            {
                boxes[0].Set((int)left.TotalDays, ScosuEventStrings.Days);
                boxes[1].Set(left.Hours, ScosuEventStrings.Hours);
                boxes[2].Set(left.Minutes, ScosuEventStrings.Minutes);
            }
            else
            {
                boxes[0].Set(left.Hours, ScosuEventStrings.Hours);
                boxes[1].Set(left.Minutes, ScosuEventStrings.Minutes);
                boxes[2].Set(left.Seconds, ScosuEventStrings.Seconds);
            }
        }

        private partial class CountBox : CompositeDrawable
        {
            private readonly OsuSpriteText value;
            private readonly OsuSpriteText unit;

            public CountBox()
            {
                Size = new Vector2(54, 46);
                Masking = true;
                CornerRadius = 8;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ScosuMenuStyle.FILL,
                    },
                    value = new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 4,
                        Font = ScosuMenuStyle.Display(18),
                    },
                    unit = new OsuSpriteText
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Y = -5,
                        Font = OsuFont.GetFont(size: 10, weight: FontWeight.SemiBold),
                        Colour = ScosuMenuStyle.TEXT_DIM,
                    },
                };
            }

            public void Set(int number, LocalisableString label)
            {
                value.Text = number.ToString(@"00");
                unit.Text = label.ToUpper();
            }
        }
    }
}
