// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
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
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Overlays.Volume;
using osu.Game.Rulesets;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Menu.Hub;
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
        private MusicController musicController { get; set; }

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private Storage storage { get; set; }

        [Resolved(canBeNull: true)]
        private LoginOverlay login { get; set; }

        [Resolved(canBeNull: true)]
        private IDialogOverlay dialogOverlay { get; set; }

        [Resolved]
        private SessionStatics sessionStatics { get; set; }

        // used to stop kiai fountain samples when navigating to other screens
        IBindable<bool> ISamplePlaybackDisabler.SamplePlaybackDisabled => samplePlaybackDisabled;
        private readonly Bindable<bool> samplePlaybackDisabled = new Bindable<bool>();

        protected override BackgroundScreen CreateBackground() => new BackgroundScreenDefault();

        protected override bool PlayExitSound => false;

        private Bindable<bool> loginDisplayed;
        private Bindable<bool> showMobileDisclaimer;

        private bool exitConfirmedViaDialog;

        /// <summary>
        /// How far the background moves up while the menu scrolls a whole screen down to the hub, for depth.
        /// </summary>
        private const float hub_background_parallax = 24;

        private ParallaxContainer buttonsContainer;
        private MenuHubPage hubPage;
        private HubDock hubDock;
        private HubHint hubHint;
        private bool hubOpen;

        // scosu: shared by the corner cards and the hub's tiles, so that they make one request each.
        [Cached]
        private readonly ScoreSummaryProvider scoreSummaryProvider = new ScoreSummaryProvider();

        [Cached]
        private readonly WeeklyReportProvider weeklyReportProvider = new WeeklyReportProvider();
        private Container logoTarget;
        private OnlineMenuBanner onlineMenuBanner;
        private MenuTipDisplay menuTipDisplay;
        private FillFlowContainer bottomElementsFlow;
        // scosu: osu!supporter can't be bought on scosu, so its prompt is disabled.
        // private SupporterDisplay supporterDisplay;
        private ScoreSummaryDisplay scoreSummaryDisplay;
        private EventMenuCard eventMenuCard;
        private WeeklyReportMenuCard weeklyReportCard;
        private BriefingMenuCard briefingCard;
        private FillFlowContainer leftDisplays;
        private FillFlowContainer rightDisplays;
        private FillFlowContainer bottomLeftDisplays;
        private FillFlowContainer bottomRightDisplays;
        private MenuChatCard chatCard;
        private HubBar hubBar;

        private Sample reappearSampleSwoosh;

        [Resolved(canBeNull: true)]
        private SkinEditorOverlay skinEditor { get; set; }

        [Resolved(canBeNull: true)]
        private VolumeOverlay volumeOverlay { get; set; }

        [CanBeNull]
        private IDisposable logoProxy;

        [BackgroundDependencyLoader(true)]
        private void load(BeatmapListingOverlay beatmapListing, SettingsOverlay settings, OsuConfigManager config, SessionStatics statics, AudioManager audio)
        {
            loginDisplayed = statics.GetBindable<bool>(Static.LoginOverlayDisplayed);
            showMobileDisclaimer = config.GetBindable<bool>(OsuSetting.ShowMobileDisclaimer);

            AddRangeInternal(new[]
            {
                scoreSummaryProvider,
                weeklyReportProvider,
                SeasonalUIConfig.ENABLED ? new MainMenuSeasonalLighting() : Empty(),
                new ScrollAdjustsVolume(),
                // scosu: while the band is out, the mouse wheel opens (down) and closes (up) the hub instead of changing the volume.
                new HubScrollHandler { Scrolled = onHubScroll },
                buttonsContainer = new ParallaxContainer
                {
                    ParallaxAmount = 0.01f,
                    // The band scrolls up a screen while the hub is open (see setHub()).
                    RelativePositionAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        Buttons = new ButtonSystem
                        {
                            OnEditBeatmap = editBeatmap,
                            OnEditSkin = () =>
                            {
                                skinEditor?.Show();
                            },
                            OnSolo = loadSongSelect,
                            OnMultiplayer = () => this.Push(new Multiplayer()),
                            OnQuickPlay = loadQuickPlay,
                            OnRankedPlay = loadRankedPlay,
                            OnPlaylists = () => this.Push(new Playlists()),
                            OnDailyChallenge = openDailyChallenge,
                            OnExit = _ => exitMenu(),
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
                    // Scrolls away with the band (see setHub()).
                    RelativePositionAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Spacing = new Vector2(5),
                    // Above the bar and the hub's hint.
                    Margin = new MarginPadding { Bottom = HubBar.HEIGHT + HubHint.HEIGHT + 20 },
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
                        // scosu: the menu at rest has today's score top left, the weekly report top right (when there's a new one),
                        // and the event bottom left (or top right, see placeEventCard()), above the bar. They scroll away with
                        // the band when the hub opens (see setHub()). The song playing is in the toolbar and the hub's bar.
                        leftDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            RelativePositionAxes = Axes.Y,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            // The card keeps its own margin from the screen's edges (see CornerCard).
                            Child = scoreSummaryDisplay = new ScoreSummaryDisplay(),
                        },
                        bottomLeftDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            RelativePositionAxes = Axes.Y,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            // Above the bar.
                            Margin = new MarginPadding { Bottom = HubBar.HEIGHT + 10 },
                            Child = eventMenuCard = new EventMenuCard
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                            },
                        },
                        // scosu: #general bottom right, above the bar.
                        bottomRightDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            RelativePositionAxes = Axes.Y,
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Margin = new MarginPadding { Bottom = HubBar.HEIGHT + 10 },
                            Child = chatCard = new MenuChatCard
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                            },
                        },
                        rightDisplays = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            RelativePositionAxes = Axes.Y,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            // The event's card when it asks to be here (see placeEventCard()), then the weekly report.
                            Children = new Drawable[]
                            {
                                weeklyReportCard = new WeeklyReportMenuCard
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                },
                                // scosu: what changed since the last session, once after logging in.
                                briefingCard = new BriefingMenuCard
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                },
                            },
                        },
                        // The hub: the menu's second screen, a grid of tiles that scrolls in from below (see setHub()).
                        hubPage = new MenuHubPage
                        {
                            OnDailyChallenge = openDailyChallenge,
                        },
                        // The bar along the bottom (today's score, the event, players and friends online, the song playing),
                        // there whenever the band is out, on both screens. In front of the hub, which scrolls in from behind it.
                        hubBar = new HubBar
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                        },
                        // "On scosu now" at the bottom of the first screen, above the bar (and in front of its shadow): opens the hub, and scrolls away with the band.
                        hubHint = new HubHint
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativePositionAxes = Axes.Y,
                            Margin = new MarginPadding { Bottom = HubBar.HEIGHT + 4 },
                            Alpha = 0,
                            Action = () => setHub(true),
                            Scrolled = onHubScroll,
                        },
                        // The band, docked along the top while the hub is open.
                        hubDock = new HubDock(new[]
                        {
                            new HubDock.Item(ButtonSystemStrings.Play, OsuIcon.Player, loadSongSelect, Primary: true),
                            new HubDock.Item(MenuHubStrings.Multiplayer, OsuIcon.Online, () => this.Push(new Multiplayer())),
                            new HubDock.Item(MenuHubStrings.Daily, OsuIcon.DailyChallenge, () =>
                            {
                                if (hubPage.DailyChallengeRoom is Room room)
                                    openDailyChallenge(room);
                            }),
                            new HubDock.Item(ButtonSystemStrings.Browse, OsuIcon.Beatmap, () => beatmapListing?.ToggleVisibility()),
                            new HubDock.Item(ButtonSystemStrings.Edit, OsuIcon.EditCircle, editBeatmap),
                        }, new[]
                        {
                            new HubDock.Item(MenuHubStrings.BackUp, FontAwesome.Solid.ChevronUp, () => setHub(false)),
                            new HubDock.Item(ButtonSystemStrings.Settings, OsuIcon.Settings, () => settings?.ToggleVisibility()),
                            new HubDock.Item(ButtonSystemStrings.Exit, OsuIcon.CrossCircle, exitMenu),
                        })
                        {
                            OnLogo = () => setHub(false),
                        },
                    },
                },
            });

            Buttons.StateChanged += state =>
            {
                // The hub closes with the band (back to the logo, or leaving the menu).
                if (!bandIsOut)
                    setHub(false);
                updateHint();

                switch (state)
                {
                    case ButtonSystemState.Initial:
                    case ButtonSystemState.Exit:
                        updateBackgroundDim();
                        onlineMenuBanner.State.Value = Visibility.Hidden;
                        eventMenuCard.Hide();
                        weeklyReportCard.Hide();
                        briefingCard.Hide();
                        chatCard.Hide();
                        scoreSummaryDisplay.Hide();
                        hubBar.Hide();
                        break;

                    default:
                        updateBackgroundDim();
                        onlineMenuBanner.State.Value = Visibility.Visible;
                        eventMenuCard.Show();
                        weeklyReportCard.Show();
                        briefingCard.Show();
                        chatCard.Show();
                        scoreSummaryDisplay.Show();
                        hubBar.Show();
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


            eventMenuCard.MenuPosition.BindValueChanged(position => placeEventCard(position.NewValue), true);
        }

        /// <summary>
        /// Moves the event's card to the side its event asks for: above the score summary, or on its own on the right.
        /// </summary>
        private void placeEventCard(ScosuEventMenuPosition position)
        {
            // On the right, above the weekly report; otherwise in the bottom left corner.
            if (position == ScosuEventMenuPosition.TopRight)
                moveCard(eventMenuCard, rightDisplays, Anchor.TopRight, -1);
            else
                moveCard(eventMenuCard, bottomLeftDisplays, Anchor.BottomLeft, 0);
        }

        /// <summary>
        /// Whether the band is out (not on the logo, nor leaving the menu): the hub can only be open then.
        /// </summary>
        private bool bandIsOut => Buttons.State is ButtonSystemState.TopLevel or ButtonSystemState.Play or ButtonSystemState.Multi or ButtonSystemState.Edit;

        /// <summary>
        /// scosu: scrolls the menu down to its hub (its second screen) or back up. The band, the logo, the corner cards and the tips
        /// leave at the top while the hub comes in from below, the background moves a little, for depth, and the band docks along
        /// the top. The bar stays. The hub's tiles show the same things as the cards (and more), from the same providers.
        /// </summary>
        private void setHub(bool open)
        {
            if (open && !bandIsOut)
                return;

            if (hubOpen == open)
                return;

            hubOpen = open;

            float y = open ? -1 : 0;

            foreach (var drawable in new Drawable[] { buttonsContainer, leftDisplays, rightDisplays, bottomLeftDisplays, bottomRightDisplays, bottomElementsFlow, hubHint })
                drawable.MoveToY(y, MenuHubPage.SCROLL_DURATION, Easing.OutQuint);

            ApplyToBackground(b => b.MoveToY(open ? -hub_background_parallax : 0, MenuHubPage.SCROLL_DURATION, Easing.OutQuint));
            updateBackgroundDim();

            hubPage.State.Value = hubDock.State.Value = open ? Visibility.Visible : Visibility.Hidden;
            updateHint();
        }

        /// <summary>
        /// The hint shows while the hub can be opened: the band is out and the hub closed.
        /// </summary>
        private void updateHint()
        {
            bool shown = bandIsOut && !hubOpen;

            hubHint.FadeTo(shown ? 1 : 0, shown ? 400 : 200, Easing.OutQuint);
            hubHint.Enabled.Value = shown;
        }

        /// <summary>
        /// Moves a card into a corner's column (the flows lay out by position: lowest first), facing that corner.
        /// </summary>
        private void moveCard(Drawable card, FillFlowContainer target, Anchor anchor, float layoutPosition)
        {
            if (card.Parent != target)
            {
                ((FillFlowContainer)card.Parent)?.Remove(card, false);

                card.Anchor = card.Origin = anchor;
                target.Add(card);
            }

            target.SetLayoutPosition(card, layoutPosition);
        }

        /// <summary>
        /// Full brightness on the logo, a little darker with the band out, and darker again on the hub, so that its tiles read.
        /// </summary>
        private void updateBackgroundDim()
        {
            float dim = SeasonalUIConfig.ENABLED ? 0.84f : 1;

            if (Buttons.State is not (ButtonSystemState.Initial or ButtonSystemState.Exit))
                dim *= hubOpen ? 0.4f : 0.75f;

            ApplyToBackground(b => b.FadeColour(OsuColour.Gray(dim), hubOpen ? MenuHubPage.SCROLL_DURATION : 500, Easing.OutSine));
        }

        private void editBeatmap()
        {
            Beatmap.SetDefault();
            this.Push(new EditorLoader());
        }

        /// <summary>
        /// The exit buttons (the band's and the hub's) and escape at the logo. scosu: asks "Are you sure you want to exit osu!?"
        /// like osu!stable does, instead of lazer's hold-escape-to-exit (see <see cref="OnExiting"/>).
        /// </summary>
        private void exitMenu() => this.Exit();

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

            eventMenuCard.Hide();
            scoreSummaryDisplay.Hide();
            hubBar.Hide();

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
            hubBar.Show();

            samplePlaybackDisabled.Value = false;
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            bool requiresConfirmation =
                // we need to have a dialog overlay to confirm in the first place.
                dialogOverlay != null
                // if the dialog has already displayed and been accepted by the user, we are good.
                && !exitConfirmedViaDialog;

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
                    }, () => Game.CancelRestartOnExit()));
                }

                return true;
            }

            Buttons.State = ButtonSystemState.Exit;
            OverlayActivationMode.Value = OverlayActivation.Disabled;


            this.FadeOut(3000);

            bottomElementsFlow
                .FadeOut(500, Easing.OutQuint);

            // supporterDisplay
            //     .FadeOut(500, Easing.OutQuint);

            eventMenuCard.Hide();
            scoreSummaryDisplay.Hide();
            hubBar.Hide();

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
                // Reached once the band is back at the logo (see ButtonSystem.goBack()).
                case GlobalAction.Back:
                    if (!host.CanExit)
                        return host.SuspendToBackground();

                    if (this.IsCurrentScreen())
                        exitMenu();

                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        private void loadSongSelect() => this.Push(new SoloSongSelect());

        /// <summary>
        /// Opens the daily challenge (from the band's button or the hub's tile), with its intro the first time.
        /// </summary>
        private void openDailyChallenge(Room room)
        {
            if (sessionStatics.Get<bool>(Static.DailyChallengeIntroPlayed))
                this.Push(new DailyChallenge(room));
            else
                this.Push(new DailyChallengeIntro(room));
        }

        private void loadQuickPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.QuickPlay));

        private void loadRankedPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.RankedPlay));

        internal enum MainMenuScrollAction
        {
            PassToVolume,
            OpenHub,
            CloseHub,
            Consume,
        }

        /// <summary>
        /// scosu: decides how a main-menu wheel event is routed between the hub and the volume overlay.
        /// In the base menu state (hub closed, volume overlay hidden), scrolling down opens the hub while scrolling up
        /// enters volume mode by letting <see cref="ScrollAdjustsVolume"/> show <see cref="VolumeOverlay"/>.
        /// While the volume overlay is visible, both scroll directions adjust volume until it auto-hides.
        /// </summary>
        internal static MainMenuScrollAction ResolveHubScroll(bool bandIsOut, bool hubOpen, bool volumeOverlayVisible, bool altPressed, float scrollDeltaY)
        {
            if (!bandIsOut || altPressed || volumeOverlayVisible)
                return MainMenuScrollAction.PassToVolume;

            if (scrollDeltaY == 0)
                return MainMenuScrollAction.Consume;

            if (hubOpen)
                return scrollDeltaY > 0 ? MainMenuScrollAction.CloseHub : MainMenuScrollAction.Consume;

            return scrollDeltaY < 0 ? MainMenuScrollAction.OpenHub : MainMenuScrollAction.PassToVolume;
        }

        private bool onHubScroll(ScrollEvent e)
        {
            bool volumeVisible = volumeOverlay?.State.Value == Visibility.Visible;

            switch (ResolveHubScroll(bandIsOut, hubOpen, volumeVisible, e.AltPressed, e.ScrollDelta.Y))
            {
                case MainMenuScrollAction.PassToVolume:
                    return false;

                case MainMenuScrollAction.OpenHub:
                    setHub(true);
                    return true;

                case MainMenuScrollAction.CloseHub:
                    setHub(false);
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Takes the mouse wheel anywhere on the menu that nothing above it uses (the hub's page scrolls itself when it has to).
        /// </summary>
        private partial class HubScrollHandler : Drawable
        {
            public Func<ScrollEvent, bool> Scrolled;

            public HubScrollHandler()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override bool OnScroll(ScrollEvent e) => Scrolled?.Invoke(e) ?? false;
        }

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
