// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;

namespace osu.Game.Graphics
{
    /// <summary>
    /// Makes the "Unlimited" frame limiter (<see cref="FrameSync.Unlimited"/>) uncapped, like osu!stable's.
    /// osu!framework otherwise caps it at 1000 fps, unless <see cref="GameHost.AllowBenchmarkUnlimitedFrames"/> is set.
    /// </summary>
    public partial class UnlimitedFrameLimiter : Component
    {
        /// <summary>
        /// osu!framework's cap when <see cref="GameHost.AllowBenchmarkUnlimitedFrames"/> isn't set.
        /// </summary>
        public const double CAPPED_HZ = 1000;

        [Resolved]
        private GameHost host { get; set; } = null!;

        private readonly Bindable<FrameSync> frameSync = new Bindable<FrameSync>();

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.FrameSync, frameSync);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            frameSync.BindValueChanged(f =>
            {
                bool unlimited = f.NewValue == FrameSync.Unlimited;

                if (host.AllowBenchmarkUnlimitedFrames == unlimited)
                    return;

                // Later frame limiter changes (of the display mode, or of the threading mode) take this into account.
                host.AllowBenchmarkUnlimitedFrames = unlimited;

                // Hosts without a window (such as headless ones) don't apply a frame limiter.
                if (host.Window == null)
                    return;

                // But the host has already applied this one, before this was set: correct it the way the host would have.
                if (unlimited)
                {
                    host.MaximumDrawHz = int.MaxValue;
                    host.MaximumUpdateHz = int.MaxValue;
                }
                else
                {
                    host.MaximumDrawHz = Math.Min(host.MaximumDrawHz, CAPPED_HZ);
                    host.MaximumUpdateHz = Math.Min(host.MaximumUpdateHz, CAPPED_HZ);
                }
            }, true);
        }
    }
}
