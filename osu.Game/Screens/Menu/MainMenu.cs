// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;
using osu.Game.Input.Bindings;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Matchmaking;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Overlays.Volume;
using osu.Game.Rulesets;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit;
using osu.Game.Screens.OnlinePlay.DailyChallenge;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.OnlinePlay.Playlists;
using osu.Game.Screens.Select;
using osu.Game.Seasonal;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Menu
{
    public partial class MainMenu : OsuScreen, IHandlePresentBeatmap, IKeyBindingHandler<GlobalAction>, ISamplePlaybackDisabler
    {
        public const float FADE_IN_DURATION = 300;

        public const float FADE_OUT_DURATION = 400;

        public override bool HideOverlaysOnEnter => Buttons == null || Buttons.State == ButtonSystemState.Initial;

        public override bool AllowUserExit => false;

        public override bool AllowExternalScreenChange => true;

        public override bool? AllowGlobalTrackControl => true;

        private MenuSideFlashes sideFlashes;

        protected ButtonSystem Buttons;

        [Resolved]
        private GameHost host { get; set; }

        [Resolved(canBeNull: true)]
        private SessionSummaryNotifier sessionSummary { get; set; }

        [Resolved]
        private INotificationOverlay notifications { get; set; }

        [Resolved]
        private MusicController musicController { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private Storage storage { get; set; }

        [Resolved(canBeNull: true)]
        private LoginOverlay login { get; set; }

        [Resolved(canBeNull: true)]
        private IDialogOverlay dialogOverlay { get; set; }

        // used to stop kiai fountain samples when navigating to other screens
        IBindable<bool> ISamplePlaybackDisabler.SamplePlaybackDisabled => samplePlaybackDisabled;
        private readonly Bindable<bool> samplePlaybackDisabled = new Bindable<bool>();

        protected override BackgroundScreen CreateBackground() => new BackgroundScreenDefault();

        protected override bool PlayExitSound => false;

        private Bindable<double> holdDelay;
        private Bindable<bool> loginDisplayed;
        private Bindable<bool> showMobileDisclaimer;

        private HoldToExitGameOverlay holdToExitGameOverlay;

        private bool exitConfirmedViaDialog;
        private bool exitConfirmedViaHoldOrClick;

        /// <summary>
        /// How far the band rises (as a share of the screen's height) while the hub is open, to stay clear of its sheet.
        /// </summary>
        private const float hub_band_rise = 0.22f;

        private const float hub_transition_duration = 600;

        private ParallaxContainer buttonsContainer;
        // scosu: the song playing, top right. It stays on screen (the SongTicker faded after a few seconds) and makes way for the hub.
        private NowPlayingMenuCard nowPlayingCard;
        private MenuHubSheet hubSheet;
        private bool hubOpen;
        private Container logoTarget;
        private OnlineMenuBanner onlineMenuBanner;
        private MenuTipDisplay menuTipDisplay;
        private FillFlowContainer bottomElementsFlow;
        // scosu: osu!supporter can't be bought on scosu, so its prompt is disabled.
        // private SupporterDisplay supporterDisplay;
        private ScoreSummaryDisplay scoreSummaryDisplay;
        private EventMenuCard eventMenuCard;
        private WeeklyReportMenuCard weeklyReportCard;
        private FillFlowContainer leftDisplays;
        private FillFlowContainer rightDisplays;
        private OnlinePlayersDisplay onlinePlayersDisplay;

        private Sample reappearSampleSwoosh;

        [Resolved(canBeNull: true)]
        private SkinEditorOverlay skinEditor { get; set; }

        [CanBeNull]
        private IDisposable logoProxy;

        [BackgroundDependencyLoader(true)]
        private void load(BeatmapListingOverlay beatmapListing, SettingsOverlay settings, OsuConfigManager config, SessionStatics statics, AudioManager audio)
        {
            holdDelay = config.GetBindable<double>(OsuSetting.UIHoldActivationDelay);
            loginDisplayed = statics.GetBindable<bool>(Static.LoginOverlayDisplayed);
            showMobileDisclaimer = config.GetBindable<bool>(OsuSetting.ShowMobileDisclaimer);

            if (host.CanExit)
            {
                AddInternal(holdToExitGameOverlay = new HoldToExitGameOverlay
                {
                    Action = () =>
                    {
                        exitConfirmedViaHoldOrClick = holdDelay.Value > 0;
                        this.Exit();
                    }
                });
            }

            AddRangeInternal(new[]
            {
                SeasonalUIConfig.ENABLED ? new MainMenuSeasonalLighting() : Empty(),
                new ScrollAdjustsVolume(),
                buttonsContainer = new ParallaxContainer
                {
                    ParallaxAmount = 0.01f,
                    // The band rises by a share of the screen while the hub is open (see setHub()).
                    RelativePositionAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        Buttons = new ButtonSystem
                        {
                            OnEditBeatmap = () =>
                            {
                                Beatmap.SetDefault();
                                this.Push(new EditorLoader());
                            },
                            OnEditSkin = () =>
                            {
                                skinEditor?.Show();
                            },
                            OnSolo = loadSongSelect,
                            OnMultiplayer = () => this.Push(new Multiplayer()),
                            OnQuickPlay = loadQuickPlay,
                            OnRankedPlay = loadRankedPlay,
                            OnPlaylists = () => this.Push(new Playlists()),
                            OnDailyChallenge = room =>
                            {
                                if (statics.Get<bool>(Static.DailyChallengeIntroPlayed))
                                    this.Push(new DailyChallenge(room));
                                else
                                    this.Push(new DailyChallengeIntro(room));
                            },
                            OnExit = e =>
                            {
                                exitConfirmedViaHoldOrClick = e is MouseEvent;
                                this.Exit();
                            },
                            OnHub = () => setHub(!hubOpen),
                            OnBack = () =>
                            {
                                if (!hubOpen)
                                    return false;

                                setHub(false);
                                return true;
                            },
                        }
                    }
                },
                logoTarget = new Container { RelativeSizeAxes = Axes.Both, },
                sideFlashes = SeasonalUIConfig.ENABLED ? new SeasonalMenuSideFlashes() : new MenuSideFlashes(),
                // For now, this is too much alongside the seasonal lighting.
                SeasonalUIConfig.ENABLED ? Empty() : new KiaiMenuFountains(),
                bottomElementsFlow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Spacing = new Vector2(5),
                    // Above the hub's handle.
                    Margin = new MarginPadding { Bottom = MenuHubSheet.HANDLE_HEIGHT + 10 },
                    Children = new Drawable[]
                    {
                        menuTipDisplay = new MenuTipDisplay
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                        },
                        onlineMenuBanner = new OnlineMenuBanner
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                        }
                    }
                },
                // scosu: osu!supporter can't be bought on scosu, so its prompt is disabled.
                // supporterDisplay = new SupporterDisplay
                // {
                //     Margin = new MarginPadding(5),
                //     Anchor = Anchor.TopLeft,
                //     Origin = Anchor.TopLeft,
                // },
                // scosu: in its place, the scosu event's card (when there is one, here or on the right, see placeEventCard()),
                // the ranked score gained today and this week, then the other players online (whose rows have a right-click menu,
                // hence the menu container).
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        leftDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Children = new Drawable[]
                            {
                                eventMenuCard = new EventMenuCard(),
                                scoreSummaryDisplay = new ScoreSummaryDisplay
                                {
                                    Margin = new MarginPadding(10),
                                },
                                onlinePlayersDisplay = new OnlinePlayersDisplay
                                {
                                    Margin = new MarginPadding { Horizontal = 10 },
                                },
                            },
                        },
                        rightDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            // The song playing first, then the event's card when it asks to be here (see placeEventCard()),
                            // then the weekly report.
                            Children = new Drawable[]
                            {
                                nowPlayingCard = new NowPlayingMenuCard
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                },
                                weeklyReportCard = new WeeklyReportMenuCard
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                },
                            },
                        },
                        // The hub: the cards move into it while it's open (see setHub()).
                        hubSheet = new MenuHubSheet
                        {
                            RequestState = setHub,
                        },
                    },
                },
                holdToExitGameOverlay?.CreateProxy() ?? Empty()
            });

            float baseDim = SeasonalUIConfig.ENABLED ? 0.84f : 1;

            Buttons.StateChanged += state =>
            {
                // The hub closes with the band (back to the logo, or leaving the menu).
                if (!bandIsOut)
                    setHub(false);
                hubSheet.Available = bandIsOut;

                switch (state)
                {
                    case ButtonSystemState.Initial:
                    case ButtonSystemState.Exit:
                        ApplyToBackground(b => b.FadeColour(OsuColour.Gray(baseDim), 500, Easing.OutSine));
                        onlineMenuBanner.State.Value = Visibility.Hidden;
                        nowPlayingCard.Hide();
                        eventMenuCard.Hide();
                        weeklyReportCard.Hide();
                        scoreSummaryDisplay.Hide();
                        onlinePlayersDisplay.Hide();
                        break;

                    default:
                        ApplyToBackground(b => b.FadeColour(OsuColour.Gray(baseDim * 0.8f), 500, Easing.OutSine));
                        onlineMenuBanner.State.Value = Visibility.Visible;
                        if (!hubOpen)
                            nowPlayingCard.Show();
                        eventMenuCard.Show();
                        weeklyReportCard.Show();
                        scoreSummaryDisplay.Show();
                        onlinePlayersDisplay.Show();
                        break;
                }
            };

            Buttons.OnSettings = () => settings?.ToggleVisibility();
            Buttons.OnBeatmapListing = () => beatmapListing?.ToggleVisibility();

            reappearSampleSwoosh = audio.Samples.Get(@"Menu/reappear-swoosh");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            GetContainingInputManager();

            // The song playing stays first on the right, above the event's card when it's there.
            rightDisplays.SetLayoutPosition(nowPlayingCard, -2);

            eventMenuCard.MenuPosition.BindValueChanged(position => placeEventCard(position.NewValue), true);
        }

        /// <summary>
        /// Moves the event's card to the side its event asks for: above the score summary, or on its own on the right.
        /// </summary>
        private void placeEventCard(ScosuEventMenuPosition position)
        {
            // In the hub, the card has its own column; it goes back to its side when the hub closes.
            if (hubOpen)
                return;

            // Above the score summary on the left; on the right, below the song playing.
            moveCard(eventMenuCard, position == ScosuEventMenuPosition.TopRight ? rightDisplays : leftDisplays, -1);
        }

        /// <summary>
        /// Whether the band is out (not on the logo, nor leaving the menu): the hub can only be open then.
        /// </summary>
        private bool bandIsOut => Buttons.State is ButtonSystemState.TopLevel or ButtonSystemState.Play or ButtonSystemState.Multi or ButtonSystemState.Edit;

        /// <summary>
        /// scosu: opens or closes the hub. The band rises to make room for its sheet, the song playing (top right) makes way,
        /// and the cards move from the corners into the sheet's columns, and back.
        /// </summary>
        private void setHub(bool open)
        {
            if (open && !bandIsOut)
                return;

            if (hubOpen == open)
                return;

            hubOpen = open;

            if (open)
            {
                nowPlayingCard.Hide();
                bottomElementsFlow.FadeOut(hub_transition_duration / 2, Easing.OutQuint);
                buttonsContainer.MoveToY(-hub_band_rise, hub_transition_duration, Easing.OutQuint);

                moveCard(scoreSummaryDisplay, hubSheet.Columns[0], 0);
                moveCard(onlinePlayersDisplay, hubSheet.Columns[0], 1);
                moveCard(eventMenuCard, hubSheet.Columns[1], 0);
                moveCard(weeklyReportCard, hubSheet.Columns[2], 0);

                hubSheet.Show();
            }
            else
            {
                hubSheet.Hide();

                buttonsContainer.MoveToY(0, hub_transition_duration, Easing.OutQuint);
                bottomElementsFlow.FadeIn(hub_transition_duration, Easing.OutQuint);

                moveCard(scoreSummaryDisplay, leftDisplays, 0);
                moveCard(onlinePlayersDisplay, leftDisplays, 1);
                moveCard(weeklyReportCard, rightDisplays, 1);
                placeEventCard(eventMenuCard.MenuPosition.Value);

                // Corner cards come back with a short fade rather than jumping out of the closing sheet.
                leftDisplays.FadeInFromZero(hub_transition_duration, Easing.OutQuint);
                rightDisplays.FadeInFromZero(hub_transition_duration, Easing.OutQuint);

                if (bandIsOut)
                    nowPlayingCard.Show();
            }
        }

        /// <summary>
        /// Moves a card into a column (the flows lay out by position: lowest first), facing the column's side of the screen.
        /// </summary>
        private void moveCard(Drawable card, FillFlowContainer target, float layoutPosition)
        {
            if (card.Parent != target)
            {
                ((FillFlowContainer)card.Parent)?.Remove(card, false);

                card.Anchor = card.Origin = target == rightDisplays ? Anchor.TopRight : Anchor.TopLeft;
                target.Add(card);
            }

            target.SetLayoutPosition(card, layoutPosition);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            // scosu: Tab opens and closes the hub (Esc also closes it, see ButtonSystem.OnBack).
            if (!e.Repeat && e.Key == Key.Tab && !e.ControlPressed && !e.AltPressed && !e.ShiftPressed && !e.SuperPressed && bandIsOut)
            {
                setHub(!hubOpen);
                return true;
            }

            return base.OnKeyDown(e);
        }

        public void ReturnToOsuLogo() => Buttons.State = ButtonSystemState.Initial;

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            Buttons.FadeInFromZero(500);

            if (e.Last is IntroScreen && musicController.TrackLoaded)
            {
                var track = musicController.CurrentTrack;

                // presume the track is the current beatmap's track. not sure how correct this assumption is but it has worked until now.
                if (!track.IsRunning)
                {
                    Beatmap.Value.PrepareTrackForPreview(false);
                    track.Restart();
                }
            }

            if (storage is OsuStorage osuStorage && osuStorage.Error != OsuStorageError.None)
                dialogOverlay?.Push(new StorageErrorDialog(osuStorage, osuStorage.Error));
        }

        [CanBeNull]
        private ScheduledDelegate mobileDisclaimerSchedule;

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            Buttons.SetOsuLogo(logo);

            logo.FadeColour(Color4.White, 100, Easing.OutQuint);
            logo.FadeIn(100, Easing.OutQuint);

            logoProxy = logo.ProxyToContainer(logoTarget);

            if (resuming)
            {
                Buttons.State = ButtonSystemState.TopLevel;

                this.FadeIn(FADE_IN_DURATION, Easing.OutQuint);
                buttonsContainer.MoveTo(new Vector2(0, 0), FADE_IN_DURATION, Easing.OutQuint);

                sideFlashes.Delay(FADE_IN_DURATION).FadeIn(64, Easing.InQuint);
            }
            else
            {
                // copy out old action to avoid accidentally capturing logo.Action in closure, causing a self-reference loop.
                var previousAction = logo.Action;

                // we want to hook into logo.Action to display certain overlays, but also preserve the return value of the old action.
                // therefore pass the old action to displayLogin, so that it can return that value.
                // this ensures that the OsuLogo sample does not play when it is not desired.
                logo.Action = () => onLogoClick(previousAction);
            }
        }

        private bool onLogoClick(Func<bool> originalAction)
        {
            if (showMobileDisclaimer.Value)
            {
                mobileDisclaimerSchedule?.Cancel();
                mobileDisclaimerSchedule = Scheduler.AddDelayed(() =>
                {
                    dialogOverlay.Push(new MobileDisclaimerDialog(() =>
                    {
                        showMobileDisclaimer.Value = false;
                        displayLoginIfApplicable();
                    }));
                }, 500);
            }
            else
                displayLoginIfApplicable();

            return originalAction.Invoke();
        }

        private void displayLoginIfApplicable()
        {
            if (loginDisplayed.Value) return;

            if (!api.IsLoggedIn || api.State.Value == APIState.RequiresSecondFactorAuth)
            {
                Scheduler.AddDelayed(() => login?.Show(), 500);
                loginDisplayed.Value = true;
            }
        }

        protected override void LogoSuspending(OsuLogo logo)
        {
            var seq = logo.FadeOut(300, Easing.InSine)
                          .ScaleTo(0.2f, 300, Easing.InSine);

            logoProxy?.Dispose();
            logoProxy = null;

            seq.OnComplete(_ => Buttons.SetOsuLogo(null));
            seq.OnAbort(_ => Buttons.SetOsuLogo(null));
        }

        protected override void LogoExiting(OsuLogo logo)
        {
            base.LogoExiting(logo);

            logoProxy?.Dispose();
            logoProxy = null;
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            Buttons.State = ButtonSystemState.EnteringMode;

            this.FadeOut(FADE_OUT_DURATION, Easing.InSine);
            buttonsContainer.MoveTo(new Vector2(-800, 0), FADE_OUT_DURATION, Easing.InSine);

            sideFlashes.FadeOut(64, Easing.OutQuint);

            bottomElementsFlow
                .ScaleTo(0.9f, 1000, Easing.OutQuint)
                .FadeOut(500, Easing.OutQuint);

            // supporterDisplay
            //     .FadeOut(500, Easing.OutQuint);

            nowPlayingCard.Hide();
            eventMenuCard.Hide();
            scoreSummaryDisplay.Hide();
            onlinePlayersDisplay.Hide();

            samplePlaybackDisabled.Value = true;
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            // Ensures any playing `ButtonSystem` samples are stopped when returning to MainMenu (as to not overlap with the 'back' sample)
            Buttons.StopSamplePlayback();
            reappearSampleSwoosh?.Play();

            ApplyToBackground(b => (b as BackgroundScreenDefault)?.Next());

            musicController.EnsurePlayingSomething();

            // scosu: a summary of the session so far, after a while of playing.
            sessionSummary?.MenuResumed();

            // Cycle tip on resuming
            menuTipDisplay.ShowNextTip();

            bottomElementsFlow
                .ScaleTo(1, 1000, Easing.OutQuint)
                .FadeIn(1000, Easing.OutQuint);

            // After a play, the score gained (and the event's standing) has likely changed.
            eventMenuCard.Refresh();
            eventMenuCard.Show();
            weeklyReportCard.Refresh();
            weeklyReportCard.Show();
            scoreSummaryDisplay.Refresh();
            scoreSummaryDisplay.Show();
            onlinePlayersDisplay.Show();
            nowPlayingCard.Show();

            samplePlaybackDisabled.Value = false;
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            bool requiresConfirmation =
                // we need to have a dialog overlay to confirm in the first place.
                dialogOverlay != null
                // if the dialog has already displayed and been accepted by the user, we are good.
                && !exitConfirmedViaDialog
                // Only require confirmation if there is either an ongoing operation or the user exited via a non-hold escape press.
                && (notifications.HasOngoingOperations || !exitConfirmedViaHoldOrClick);

            if (requiresConfirmation)
            {
                if (dialogOverlay.CurrentDialog is ConfirmExitDialog exitDialog)
                {
                    if (exitDialog.Buttons.OfType<PopupDialogOkButton>().FirstOrDefault() != null)
                        exitDialog.PerformOkAction();
                    else
                        exitDialog.Flash();
                }
                else
                {
                    dialogOverlay.Push(new ConfirmExitDialog(() =>
                    {
                        exitConfirmedViaDialog = true;
                        this.Exit();
                    }, () =>
                    {
                        holdToExitGameOverlay.Abort();
                        Game.CancelRestartOnExit();
                    }));
                }

                return true;
            }

            Buttons.State = ButtonSystemState.Exit;
            OverlayActivationMode.Value = OverlayActivation.Disabled;

            nowPlayingCard.Hide();

            this.FadeOut(3000);

            bottomElementsFlow
                .FadeOut(500, Easing.OutQuint);

            // supporterDisplay
            //     .FadeOut(500, Easing.OutQuint);

            eventMenuCard.Hide();
            scoreSummaryDisplay.Hide();
            onlinePlayersDisplay.Hide();

            return base.OnExiting(e);
        }

        public void PresentBeatmap(WorkingBeatmap beatmap, RulesetInfo ruleset)
        {
            Logger.Log($"{nameof(MainMenu)} completing {nameof(PresentBeatmap)} with beatmap {beatmap} ruleset {ruleset}");

            Beatmap.Value = beatmap;
            Ruleset.Value = ruleset;

            Schedule(loadSongSelect);
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.Back:
                    // In the case of a host being able to exit, the back action is handled by ExitConfirmOverlay.
                    Debug.Assert(!host.CanExit);

                    return host.SuspendToBackground();
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        private void loadSongSelect() => this.Push(new SoloSongSelect());

        private void loadQuickPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.QuickPlay));

        private void loadRankedPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.RankedPlay));

        private partial class MobileDisclaimerDialog : PopupDialog
        {
            public MobileDisclaimerDialog(Action confirmed)
            {
                HeaderText = ButtonSystemStrings.MobileDisclaimerHeader;
                BodyText = ButtonSystemStrings.MobileDisclaimerBody;

                Icon = FontAwesome.Solid.SmileBeam;

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogOkButton
                    {
                        Text = ButtonSystemStrings.MobileDisclaimerOkButton,
                        Action = confirmed,
                    },
                };
            }
        }
    }
}
