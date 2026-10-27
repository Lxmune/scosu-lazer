// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Rankings.Tables;
using osu.Game.Rulesets;
using osuTK;

namespace osu.Game.Overlays.Rankings
{
    /// <summary>
    /// The players who gained the most ranked score this week or month,
    /// on servers that rank plays by osu!stable score (see <see cref="Online.EndpointConfiguration.RanksByStableScore"/>).
    /// </summary>
    public partial class GainedRankingsLayout : CompositeDrawable
    {
        public readonly Bindable<RulesetInfo> Ruleset = new Bindable<RulesetInfo>();

        private readonly Bindable<GainedRankingsPeriod> period = new Bindable<GainedRankingsPeriod>();
        private readonly Bindable<RankingsSortCriteria> filter = new Bindable<RankingsSortCriteria>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private GetGainedRankingsRequest? request;
        private CancellationTokenSource? cancellationToken;

        private OsuSpriteText description = null!;
        private Container content = null!;
        private LoadingLayer loading = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background5,
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding { Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING, Vertical = 15 },
                                Spacing = new Vector2(0, 5),
                                Children = new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Children = new Drawable[]
                                        {
                                            new OverlaySortTabControl<GainedRankingsPeriod>
                                            {
                                                Title = GainedRankingsStrings.Period.ToUpper(),
                                                Current = period,
                                            },
                                            new RankingsSortTabControl
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Current = filter,
                                            },
                                        }
                                    },
                                    description = new OsuSpriteText
                                    {
                                        Font = OsuFont.GetFont(size: 12),
                                        Colour = colourProvider.Foreground1,
                                    },
                                }
                            },
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            content = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Margin = new MarginPadding { Vertical = 10 }
                            },
                            loading = new LoadingLayer(true)
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            period.BindValueChanged(_ => Scheduler.AddOnce(fetchRankings));
            filter.BindValueChanged(_ => Scheduler.AddOnce(fetchRankings));
            Ruleset.BindValueChanged(_ => Scheduler.AddOnce(fetchRankings));

            fetchRankings();
        }

        private void fetchRankings()
        {
            description.Text = period.Value == GainedRankingsPeriod.Month ? GainedRankingsStrings.MonthDescription : GainedRankingsStrings.WeekDescription;

            loading.Show();

            cancellationToken?.Cancel();
            request?.Cancel();

            request = new GetGainedRankingsRequest(Ruleset.Value, period.Value, filter.Value);
            request.Success += response => Schedule(() => onSuccess(response));
            request.Failure += _ => Schedule(() =>
            {
                content.Clear();
                loading.Hide();
            });
            api.Queue(request);
        }

        private void onSuccess(GetGainedRankingsResponse response)
        {
            Drawable display = response.Ranking.Count == 0
                ? new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Margin = new MarginPadding { Vertical = 20 },
                    Text = GainedRankingsStrings.NoneYet,
                }
                : new GainedTable(1, response.Ranking);

            LoadComponentAsync(display, loaded =>
            {
                content.Clear();
                content.Add(loaded);

                loading.Hide();
            }, (cancellationToken = new CancellationTokenSource()).Token);
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            cancellationToken?.Cancel();

            base.Dispose(isDisposing);
        }
    }
}
