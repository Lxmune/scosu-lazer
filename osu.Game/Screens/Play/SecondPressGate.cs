// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// scosu: asks for a second press of the same action within <see cref="WINDOW"/> before it goes ahead (retry and quit late in
    /// a play, see <see cref="osu.Game.Configuration.OsuSetting.ScosuConfirmRetryQuit"/>). Times are in milliseconds of real time, not gameplay time, so a
    /// paused or stopped gameplay clock doesn't hold the window open.
    /// </summary>
    public class SecondPressGate
    {
        /// <summary>
        /// How long after the first press the second one counts.
        /// </summary>
        public const double WINDOW = 5000;

        /// <summary>
        /// How much of the map must have been played for retry and quit to ask twice.
        /// </summary>
        public const double MIN_PLAYED = 60_000;

        private object? armed;
        private double armedAt;

        /// <summary>
        /// A press of <paramref name="action"/> at <paramref name="now"/>: true if it goes ahead (the second press in time),
        /// false if it only armed the gate.
        /// </summary>
        public bool Press(object action, double now)
        {
            if (IsArmed(action, now))
            {
                armed = null;
                return true;
            }

            armed = action;
            armedAt = now;
            return false;
        }

        /// <summary>
        /// Whether a press of <paramref name="action"/> at <paramref name="now"/> would go ahead.
        /// </summary>
        public bool IsArmed(object action, double now) => armed != null && ReferenceEquals(armed, action) && now - armedAt <= WINDOW;

        public void Reset() => armed = null;
    }
}
