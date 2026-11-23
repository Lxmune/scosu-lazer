// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Footer;

namespace osu.Game.Screens.OnlinePlay
{
    /// <summary>
    /// scosu: picks the win condition of a multiplayer playlist item. Each click moves to the next one (score, accuracy, combo).
    /// </summary>
    public partial class FooterButtonWinCondition : ScreenFooterButton
    {
        public readonly Bindable<WinCondition> Condition = new Bindable<WinCondition>();

        public new Action Action
        {
            set => throw new NotSupportedException("The click action is handled by the button itself.");
        }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public FooterButtonWinCondition()
        {
            base.Action = () => Condition.Value = next(Condition.Value);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Icon = FontAwesome.Solid.Trophy;
            AccentColour = colours.Orange1;

            TooltipText = ScosuMultiplayerStrings.WinConditionTooltip;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Condition.BindValueChanged(condition =>
            {
                Text = condition.NewValue.GetLocalisableDescription();
                // Lit up when the item doesn't play by score, like the freestyle button when freestyle is on.
                OverlayState.Value = condition.NewValue == WinCondition.Score ? Visibility.Hidden : Visibility.Visible;
            }, true);
        }

        private static WinCondition next(WinCondition condition)
        {
            switch (condition)
            {
                case WinCondition.Score:
                    return WinCondition.Accuracy;

                case WinCondition.Accuracy:
                    return WinCondition.Combo;

                default:
                    return WinCondition.Score;
            }
        }
    }
}
