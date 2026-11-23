// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;

namespace osu.Game.Screens.Play
{
    public partial class HotkeyRetryOverlay : HoldToConfirmOverlay, IKeyBindingHandler<GlobalAction>
    {
        /// <summary>
        /// scosu: whether the hotkey needs a second press right now (see <see cref="SecondPressGate"/>): the first press only
        /// says so, the second (within the gate's window) starts the hold.
        /// </summary>
        public Func<bool>? NeedsSecondPress { get; init; }

        private readonly SecondPressGate secondPress = new SecondPressGate();

        private OsuSpriteText hint = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(hint = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = ScosuDailyStrings.PressAgainToConfirm,
                Font = OsuFont.GetFont(size: 28, weight: FontWeight.Bold),
                Alpha = 0,
            });
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            if (e.Action != GlobalAction.QuickRetry) return false;

            if (NeedsSecondPress?.Invoke() == true && !secondPress.Press(this, Environment.TickCount64))
            {
                hint.ClearTransforms();
                hint.FadeIn(100);
                hint.Delay(SecondPressGate.WINDOW).FadeOut(300);
                return true;
            }

            hint.FadeOut(100);
            BeginConfirm();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
            if (e.Action != GlobalAction.QuickRetry) return;

            AbortConfirm();
        }

        protected override void Confirm()
        {
            base.Confirm();

            // Not removing immediately can lead to delays due to async disposal.
            // This is done here rather than in `Player` because it's simpler to handle.
            RemoveAudioAdjustments();
        }
    }
}
