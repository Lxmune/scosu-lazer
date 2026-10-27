// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osuTK.Graphics;

namespace osu.Game.Overlays.Profile.Header.Components
{
    /// <summary>
    /// A player's ranked score over the last 90 days, on servers that rank plays by osu!stable score.
    /// The line is the ranked score at the end of each day, the bars under it what each day added.
    /// </summary>
    public partial class RankedScoreGraph : UserGraph<int, long>
    {
        public readonly Bindable<APIRankedScoreHistory?> History = new Bindable<APIRankedScoreHistory?>();

        private readonly BarGraph gains;

        public RankedScoreGraph()
        {
            Add(gains = new BarGraph
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                RelativeSizeAxes = Axes.Both,
                // Short enough to leave the line readable above it.
                Height = 0.6f,
                Alpha = 0,
                Depth = 1,
            });
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            var colour = GetAccentColour(colours);
            gains.Colour = ColourInfo.GradientVertical(colour.Opacity(0.5f), colour.Opacity(0.15f));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            History.BindValueChanged(history =>
            {
                long[]? data = HasRankedScore(history.NewValue) ? history.NewValue!.Data : null;

                Data = data?.Select((score, index) => new KeyValuePair<int, long>(index, score)).ToArray();
                gains.Values = data == null ? Enumerable.Empty<float>() : data.Select((_, index) => (float)GainOn(data, index));
            }, true);
        }

        protected override Color4 GetAccentColour(OsuColour colours) => colours.Blue1;

        /// <summary>
        /// Whether <paramref name="history"/> has anything to show: a player who never had ranked score has nothing.
        /// </summary>
        public static bool HasRankedScore(APIRankedScoreHistory? history) => history?.Data.Any(score => score > 0) == true;

        /// <summary>
        /// What day <paramref name="index"/> of <paramref name="data"/> added to the ranked score (nothing known for the first day).
        /// </summary>
        public static long GainOn(long[] data, int index) => index == 0 ? 0 : Math.Max(0, data[index] - data[index - 1]);

        protected override float GetDataPointHeight(long score) => score;

        protected override void ShowGraph()
        {
            base.ShowGraph();
            gains.FadeIn(FADE_DURATION, Easing.Out);
        }

        protected override void HideGraph()
        {
            base.HideGraph();
            gains.FadeOut(FADE_DURATION, Easing.Out);
        }

        protected override UserGraphTooltipContent GetTooltipContent(int index, long score)
        {
            long[] data = History.Value?.Data ?? Array.Empty<long>();
            int days = data.Length - 1 - index;
            LocalisableString time = days == 0 ? "now" : $"{"day".ToQuantity(days)} ago";
            long gain = index < data.Length ? GainOn(data, index) : 0;

            return new UserGraphTooltipContent
            {
                Name = UsersStrings.ShowStatsRankedScore,
                Count = score.ToLocalisableString(@"N0"),
                Time = gain > 0 ? LocalisableString.Interpolate($"{time} (+{gain.ToLocalisableString(@"N0")})") : time,
            };
        }
    }
}
