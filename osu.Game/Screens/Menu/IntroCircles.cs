// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Screens;
using osu.Framework.Graphics;

namespace osu.Game.Screens.Menu
{
    public partial class IntroCircles : IntroScreen
    {
        protected override string BeatmapHash => "388d98736655b1b383a09d06f8a0ec8a8639eb8c39665035d22456a65b78f959";

        protected override string BeatmapFile => "circles.osz";

        public const double TRACK_START_DELAY = 600;

        private const double delay_for_menu = 2900;

        private Sample welcome;

        public IntroCircles([CanBeNull] Func<MainMenu> createNextScreen = null)
            : base(createNextScreen)
        {
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            if (MenuVoice.Value)
                welcome = audio.Samples.Get(@"Intro/welcome");
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            if (!resuming)
            {
                welcome?.Play();

                Scheduler.AddDelayed(delegate
                {
                    StartTrack();

                    PrepareMenuLoad();

                    Scheduler.AddDelayed(LoadMenu, delay_for_menu - TRACK_START_DELAY);
                }, TRACK_START_DELAY);

                logo.ScaleTo(1);
                logo.FadeIn();
                logo.PlayIntro();
            }
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            this.FadeOut(300);
            base.OnSuspending(e);
        }
    }
}
