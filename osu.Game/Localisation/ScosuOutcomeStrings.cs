// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of what a play did on scosu (the score panel's status, place and ranked score, and the standing), and of where its combo broke.
    /// </summary>
    public static class ScosuOutcomeStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuOutcome";

        /// <summary>
        /// "Counted"
        /// </summary>
        public static LocalisableString Counted => new TranslatableString(getKey(@"counted"), @"Counted");

        /// <summary>
        /// "Not counted"
        /// </summary>
        public static LocalisableString NotCounted => new TranslatableString(getKey(@"not_counted"), @"Not counted");

        /// <summary>
        /// "The ranked score this play added."
        /// </summary>
        public static LocalisableString RankedScoreGainTooltip => new TranslatableString(getKey(@"ranked_score_gain_tooltip"), @"The ranked score this play added.");

        /// <summary>
        /// "Below your best"
        /// </summary>
        public static LocalisableString BelowBest => new TranslatableString(getKey(@"below_best"), @"Below your best");

        /// <summary>
        /// "Only your best score on a beatmap counts: this play adds no ranked score."
        /// </summary>
        public static LocalisableString BelowBestTooltip => new TranslatableString(getKey(@"below_best_tooltip"), @"Only your best score on a beatmap counts: this play adds no ranked score.");

        /// <summary>
        /// "No ranked score yet"
        /// </summary>
        public static LocalisableString NoRankedScoreYet => new TranslatableString(getKey(@"no_ranked_score_yet"), @"No ranked score yet");

        /// <summary>
        /// "The beatmap is qualified: the play is on its leaderboard, and adds to your ranked score once the beatmap is ranked."
        /// </summary>
        public static LocalisableString QualifiedTooltip => new TranslatableString(getKey(@"qualified_tooltip"),
            @"The beatmap is qualified: the play is on its leaderboard, and adds to your ranked score once the beatmap is ranked.");

        /// <summary>
        /// "Your place on this beatmap's leaderboard."
        /// </summary>
        public static LocalisableString MapRankTooltip => new TranslatableString(getKey(@"map_rank_tooltip"), @"Your place on this beatmap's leaderboard.");

        /// <summary>
        /// "Your place on this beatmap's leaderboard (#{0} before this play)."
        /// </summary>
        public static LocalisableString MapRankChangeTooltip(LocalisableString before) => new TranslatableString(getKey(@"map_rank_change_tooltip"),
            @"Your place on this beatmap's leaderboard (#{0} before this play).", before);

        /// <summary>
        /// "Your place in the ranking by ranked score."
        /// </summary>
        public static LocalisableString GlobalRankTooltip => new TranslatableString(getKey(@"global_rank_tooltip"), @"Your place in the ranking by ranked score.");

        /// <summary>
        /// "#1, taken from {0}"
        /// </summary>
        public static LocalisableString TookFirstFrom(LocalisableString player) => new TranslatableString(getKey(@"took_first_from"), @"#1, taken from {0}", player);

        /// <summary>
        /// "On the {0} leaderboard"
        /// </summary>
        public static LocalisableString OwnLeaderboard(LocalisableString name) => new TranslatableString(getKey(@"own_leaderboard"), @"On the {0} leaderboard", name);

        /// <summary>
        /// "Relax"
        /// </summary>
        public static LocalisableString Relax => new TranslatableString(getKey(@"relax"), @"Relax");

        /// <summary>
        /// "Autopilot"
        /// </summary>
        public static LocalisableString Autopilot => new TranslatableString(getKey(@"autopilot"), @"Autopilot");

        /// <summary>
        /// "The play was failed."
        /// </summary>
        public static LocalisableString ReasonFailed => new TranslatableString(getKey(@"reason_failed"), @"The play was failed.");

        /// <summary>
        /// "This beatmap has no leaderboard ({0})."
        /// </summary>
        public static LocalisableString ReasonNoLeaderboard(LocalisableString status) => new TranslatableString(getKey(@"reason_no_leaderboard"),
            @"This beatmap has no leaderboard ({0}).", status);

        /// <summary>
        /// "{0} is never ranked."
        /// </summary>
        public static LocalisableString ReasonUnrankedMod(LocalisableString mod) => new TranslatableString(getKey(@"reason_unranked_mod"), @"{0} is never ranked.", mod);

        /// <summary>
        /// "{0} is only ranked with its default settings."
        /// </summary>
        public static LocalisableString ReasonModSettings(LocalisableString mod) => new TranslatableString(getKey(@"reason_mod_settings"),
            @"{0} is only ranked with its default settings.", mod);

        /// <summary>
        /// "{0} (+{1})"
        /// </summary>
        public static LocalisableString PlayerAndGap(LocalisableString player, LocalisableString gap) => new TranslatableString(getKey(@"player_and_gap"), @"{0} (+{1})", player, gap);

        /// <summary>
        /// "Not on the leaderboard"
        /// </summary>
        public static LocalisableString NotOnLeaderboard => new TranslatableString(getKey(@"not_on_leaderboard"), @"Not on the leaderboard");

        /// <summary>
        /// "Top of the ranking"
        /// </summary>
        public static LocalisableString TopOfRanking => new TranslatableString(getKey(@"top_of_ranking"), @"Top of the ranking");

        /// <summary>
        /// "Why it didn't count"
        /// </summary>
        public static LocalisableString WhyNotCounted => new TranslatableString(getKey(@"why_not_counted"), @"Why it didn't count");

        /// <summary>
        /// "Full combo"
        /// </summary>
        public static LocalisableString FullCombo => new TranslatableString(getKey(@"full_combo"), @"Full combo");

        /// <summary>
        /// "First play"
        /// </summary>
        public static LocalisableString FirstPlay => new TranslatableString(getKey(@"first_play"), @"First play");

        /// <summary>
        /// "{0}x combo lost"
        /// </summary>
        public static LocalisableString ComboLost(LocalisableString combo) => new TranslatableString(getKey(@"combo_lost_x"), @"{0}x combo lost", combo);

        /// <summary>
        /// "What this break cost in score isn't known for this play, so the breaks are ranked by the combo they ended."
        /// </summary>
        public static LocalisableString ComboLostTooltip => new TranslatableString(getKey(@"combo_lost_tooltip"),
            @"What this break cost in score isn't known for this play, so the breaks are ranked by the combo they ended.");

        /// <summary>
        /// "max {0}x"
        /// </summary>
        public static LocalisableString MaxComboLabel(LocalisableString combo) => new TranslatableString(getKey(@"max_combo_label"), @"max {0}x", combo);

        /// <summary>
        /// "Your Standing"
        /// </summary>
        public static LocalisableString StandingHeader => new TranslatableString(getKey(@"standing_header"), @"Your Standing");

        /// <summary>
        /// "Ranked score"
        /// </summary>
        public static LocalisableString RankedScore => new TranslatableString(getKey(@"ranked_score"), @"Ranked score");

        /// <summary>
        /// "Combo Timeline"
        /// </summary>
        public static LocalisableString ComboTimelineHeader => new TranslatableString(getKey(@"combo_timeline_header"), @"Combo Timeline");

        /// <summary>
        /// "Full combo: nothing broke."
        /// </summary>
        public static LocalisableString NothingBroke => new TranslatableString(getKey(@"nothing_broke"), @"Full combo: nothing broke.");

        /// <summary>
        /// "miss"
        /// </summary>
        public static LocalisableString Miss => new TranslatableString(getKey(@"miss"), @"miss");

        /// <summary>
        /// "slider break"
        /// </summary>
        public static LocalisableString SliderBreak => new TranslatableString(getKey(@"slider_break"), @"slider break");

        /// <summary>
        /// "combo {0} → 0"
        /// </summary>
        public static LocalisableString ComboReset(LocalisableString combo) => new TranslatableString(getKey(@"combo_reset"), @"combo {0} → 0", combo);

        /// <summary>
        /// "right after another break"
        /// </summary>
        public static LocalisableString RightAfterAnotherBreak => new TranslatableString(getKey(@"right_after_another_break"), @"right after another break");

        /// <summary>
        /// "cost −{0}"
        /// </summary>
        public static LocalisableString BreakCost(LocalisableString score) => new TranslatableString(getKey(@"break_cost_exact"), @"cost −{0}", score);

        /// <summary>
        /// "+{0} more breaks around it"
        /// </summary>
        public static LocalisableString MoreBreaksNearby(int count) => new TranslatableString(getKey(@"more_breaks_nearby"), @"+{0} more breaks around it", count);

        /// <summary>
        /// "Click to watch from {0}"
        /// </summary>
        public static LocalisableString ClickToWatchFrom(LocalisableString time) => new TranslatableString(getKey(@"click_to_watch_from"), @"Click to watch from {0}", time);

        /// <summary>
        /// "Costliest Breaks"
        /// </summary>
        public static LocalisableString CostliestBreaksHeader => new TranslatableString(getKey(@"costliest_breaks_header"), @"Costliest Breaks");

        /// <summary>
        /// "Exactly the osu!stable score the play lost here: what it would have scored with only this break hit perfectly instead."
        /// </summary>
        public static LocalisableString CostTooltip => new TranslatableString(getKey(@"cost_tooltip_exact"),
            @"Exactly the osu!stable score the play lost here: what it would have scored with only this break hit perfectly instead.");

        /// <summary>
        /// "Watch"
        /// </summary>
        public static LocalisableString Watch => new TranslatableString(getKey(@"watch"), @"Watch");

        /// <summary>
        /// "Watch the replay from {0}"
        /// </summary>
        public static LocalisableString WatchFrom(LocalisableString time) => new TranslatableString(getKey(@"watch_from"), @"Watch the replay from {0}", time);

        /// <summary>
        /// "Play History"
        /// </summary>
        public static LocalisableString HistoryHeader => new TranslatableString(getKey(@"history_header"), @"Play History");

        /// <summary>
        /// "Analyse the play"
        /// </summary>
        public static LocalisableString Analyse => new TranslatableString(getKey(@"analyse_play"), @"Analyse the play");

        /// <summary>
        /// "Analyse the play: it is replayed first, which takes a moment"
        /// </summary>
        public static LocalisableString AnalyseByReplaying => new TranslatableString(getKey(@"analyse_by_replaying"), @"Analyse the play: it is replayed first, which takes a moment");

        /// <summary>
        /// "Click to show or hide the play's statistics"
        /// </summary>
        public static LocalisableString PanelStatisticsHint => new TranslatableString(getKey(@"panel_statistics_hint"), @"Click to show or hide the play's statistics");

        /// <summary>
        /// "Analysing the replay"
        /// </summary>
        public static LocalisableString AnalysingReplay => new TranslatableString(getKey(@"analysing_replay"), @"Analysing the replay");

        /// <summary>
        /// "Looking for the replay"
        /// </summary>
        public static LocalisableString FindingReplay => new TranslatableString(getKey(@"finding_replay"), @"Looking for the replay");

        /// <summary>
        /// "Getting the play's judgements"
        /// </summary>
        public static LocalisableString GettingJudgements => new TranslatableString(getKey(@"getting_judgements"), @"Getting the play's judgements");

        /// <summary>
        /// "Downloading the replay"
        /// </summary>
        public static LocalisableString DownloadingReplay => new TranslatableString(getKey(@"downloading_replay"), @"Downloading the replay");

        /// <summary>
        /// "Replaying the play: {0}%"
        /// </summary>
        public static LocalisableString ReplayingPlay(int percent) => new TranslatableString(getKey(@"replaying_play"), @"Replaying the play: {0}%", percent);

        /// <summary>
        /// "Replaying the play: {0}% · about {1}s left"
        /// </summary>
        public static LocalisableString ReplayingPlayTimeLeft(int percent, int seconds) => new TranslatableString(getKey(@"replaying_play_time_left"),
            @"Replaying the play: {0}% · about {1}s left", percent, seconds);

        /// <summary>
        /// "Replay Analysis"
        /// </summary>
        public static LocalisableString ReplayAnalysisHeader => new TranslatableString(getKey(@"replay_analysis_header"), @"Replay Analysis");

        /// <summary>
        /// "This play has no replay, so the statistics that need one can't be shown."
        /// </summary>
        public static LocalisableString NoReplayToAnalyse => new TranslatableString(getKey(@"no_replay_to_analyse"),
            @"This play has no replay, so the statistics that need one can't be shown.");

        /// <summary>
        /// "Download the beatmap to see the statistics that need the play's replay."
        /// </summary>
        public static LocalisableString BeatmapNeededForReplay => new TranslatableString(getKey(@"beatmap_needed_for_replay"),
            @"Download the beatmap to see the statistics that need the play's replay.");

        /// <summary>
        /// "The replay couldn't be downloaded, so the statistics that need it can't be shown."
        /// </summary>
        public static LocalisableString ReplayDownloadFailed => new TranslatableString(getKey(@"replay_download_failed"),
            @"The replay couldn't be downloaded, so the statistics that need it can't be shown.");

        /// <summary>
        /// "The replay doesn't play back the same here ({1} misses instead of {0}), so the statistics that need it can't be shown."
        /// </summary>
        public static LocalisableString ReplayWentAstray(int scored, int replayed) => new TranslatableString(getKey(@"replay_went_astray"),
            @"The replay doesn't play back the same here ({1} misses instead of {0}), so the statistics that need it can't be shown.", scored, replayed);

        /// <summary>
        /// "S.Break"
        /// </summary>
        public static LocalisableString SliderBreakHeader => new TranslatableString(getKey(@"slider_break_header"), @"S.Break");

        /// <summary>
        /// "Slider breaks: slider ticks and repeats let go, which reset the combo."
        /// </summary>
        public static LocalisableString SliderBreaksTooltip => new TranslatableString(getKey(@"slider_breaks_tooltip"), @"Slider breaks: slider ticks and repeats let go, which reset the combo.");

        /// <summary>
        /// "−{0} breaks"
        /// </summary>
        public static LocalisableString ScoreBarBreaks(LocalisableString score) => new TranslatableString(getKey(@"score_bar_breaks"), @"−{0} breaks", score);

        /// <summary>
        /// "−{0} judgements"
        /// </summary>
        public static LocalisableString ScoreBarJudgements(LocalisableString score) => new TranslatableString(getKey(@"score_bar_judgements"), @"−{0} judgements", score);

        /// <summary>
        /// "−{0} missing"
        /// </summary>
        public static LocalisableString ScoreBarMissing(LocalisableString score) => new TranslatableString(getKey(@"score_bar_missing"), @"−{0} missing", score);

        /// <summary>
        /// "{0} pp"
        /// </summary>
        public static LocalisableString Performance(LocalisableString pp) => new TranslatableString(getKey(@"performance"), @"{0} pp", pp);

        /// <summary>
        /// "Why You Missed"
        /// </summary>
        public static LocalisableString WhyYouMissedHeader => new TranslatableString(getKey(@"why_you_missed_header"), @"Why You Missed");

        /// <summary>
        /// "Estimated from the replay: what went wrong in the {0} misses, slider breaks and slider ends found in it."
        /// </summary>
        public static LocalisableString CausesFoundInReplay(int found) => new TranslatableString(getKey(@"causes_found_in_replay"),
            @"Estimated from the replay: what went wrong in the {0} misses, slider breaks and slider ends found in it.", found);

        /// <summary>
        /// "1 miss"
        /// </summary>
        public static LocalisableString OneMiss => new TranslatableString(getKey(@"one_miss"), @"1 miss");

        /// <summary>
        /// "{0} misses"
        /// </summary>
        public static LocalisableString MissCount(LocalisableString count) => new TranslatableString(getKey(@"miss_count"), @"{0} misses", count);

        /// <summary>
        /// "1 slider break"
        /// </summary>
        public static LocalisableString OneSliderBreak => new TranslatableString(getKey(@"one_slider_break"), @"1 slider break");

        /// <summary>
        /// "{0} slider breaks"
        /// </summary>
        public static LocalisableString SliderBreakCount(LocalisableString count) => new TranslatableString(getKey(@"slider_break_count"), @"{0} slider breaks", count);

        /// <summary>
        /// "Closest to FC · this play"
        /// </summary>
        public static LocalisableString TileClosestThisPlay => new TranslatableString(getKey(@"tile_closest_this_play"), @"Closest to FC · this play");

        /// <summary>
        /// "Waiting for the replay…"
        /// </summary>
        public static LocalisableString WaitingForReplay => new TranslatableString(getKey(@"waiting_for_replay"), @"Waiting for the replay…");

        /// <summary>
        /// "The replay shows no misses."
        /// </summary>
        public static LocalisableString NoMissesInReplay => new TranslatableString(getKey(@"no_misses_in_replay"), @"The replay shows no misses.");

        /// <summary>
        /// "Too early"
        /// </summary>
        public static LocalisableString CauseEarly => new TranslatableString(getKey(@"cause_early"), @"Too early");

        /// <summary>
        /// "Pressed before the hit window."
        /// </summary>
        public static LocalisableString CauseEarlyTooltip => new TranslatableString(getKey(@"cause_early_tooltip"), @"Pressed before the hit window.");

        /// <summary>
        /// "Too late"
        /// </summary>
        public static LocalisableString CauseLate => new TranslatableString(getKey(@"cause_late"), @"Too late");

        /// <summary>
        /// "Pressed after the hit window."
        /// </summary>
        public static LocalisableString CauseLateTooltip => new TranslatableString(getKey(@"cause_late_tooltip"), @"Pressed after the hit window.");

        /// <summary>
        /// "Aimed short"
        /// </summary>
        public static LocalisableString CauseAimedShort => new TranslatableString(getKey(@"cause_aimed_short"), @"Aimed short");

        /// <summary>
        /// "Pressed in time, with the cursor short of the object."
        /// </summary>
        public static LocalisableString CauseAimedShortTooltip => new TranslatableString(getKey(@"cause_aimed_short_tooltip"), @"Pressed in time, with the cursor short of the object.");

        /// <summary>
        /// "Aimed past"
        /// </summary>
        public static LocalisableString CauseAimedLong => new TranslatableString(getKey(@"cause_aimed_long"), @"Aimed past");

        /// <summary>
        /// "Pressed in time, with the cursor past the object."
        /// </summary>
        public static LocalisableString CauseAimedLongTooltip => new TranslatableString(getKey(@"cause_aimed_long_tooltip"), @"Pressed in time, with the cursor past the object.");

        /// <summary>
        /// "Aimed to the side"
        /// </summary>
        public static LocalisableString CauseAimedOff => new TranslatableString(getKey(@"cause_aimed_off"), @"Aimed to the side");

        /// <summary>
        /// "Pressed in time, with the cursor off to one side."
        /// </summary>
        public static LocalisableString CauseAimedOffTooltip => new TranslatableString(getKey(@"cause_aimed_off_tooltip"), @"Pressed in time, with the cursor off to one side.");

        /// <summary>
        /// "Notelocked"
        /// </summary>
        public static LocalisableString CauseBlocked => new TranslatableString(getKey(@"cause_blocked"), @"Notelocked");

        /// <summary>
        /// "Pressed in time on the object, but the press went to another one."
        /// </summary>
        public static LocalisableString CauseBlockedTooltip => new TranslatableString(getKey(@"cause_blocked_tooltip"), @"Pressed in time on the object, but the press went to another one.");

        /// <summary>
        /// "No press"
        /// </summary>
        public static LocalisableString CauseNoPress => new TranslatableString(getKey(@"cause_no_press"), @"No press");

        /// <summary>
        /// "No key was pressed near the object's time."
        /// </summary>
        public static LocalisableString CauseNoPressTooltip => new TranslatableString(getKey(@"cause_no_press_tooltip"), @"No key was pressed near the object's time.");

        /// <summary>
        /// "Left the slider"
        /// </summary>
        public static LocalisableString CauseFollowLost => new TranslatableString(getKey(@"cause_follow_lost"), @"Left the slider");

        /// <summary>
        /// "The cursor left the slider's follow circle."
        /// </summary>
        public static LocalisableString CauseFollowLostTooltip => new TranslatableString(getKey(@"cause_follow_lost_tooltip"), @"The cursor left the slider's follow circle.");

        /// <summary>
        /// "Let go of the key"
        /// </summary>
        public static LocalisableString CauseKeyReleased => new TranslatableString(getKey(@"cause_key_released"), @"Let go of the key");

        /// <summary>
        /// "The key was let go while the slider was running."
        /// </summary>
        public static LocalisableString CauseKeyReleasedTooltip => new TranslatableString(getKey(@"cause_key_released_tooltip"), @"The key was let go while the slider was running.");

        /// <summary>
        /// "Other"
        /// </summary>
        public static LocalisableString CauseOther => new TranslatableString(getKey(@"cause_other"), @"Other");

        /// <summary>
        /// "On average {0} ms early."
        /// </summary>
        public static LocalisableString AverageEarly(LocalisableString ms) => new TranslatableString(getKey(@"average_early"), @"On average {0} ms early.", ms);

        /// <summary>
        /// "On average {0} ms late."
        /// </summary>
        public static LocalisableString AverageLate(LocalisableString ms) => new TranslatableString(getKey(@"average_late"), @"On average {0} ms late.", ms);

        /// <summary>
        /// "On average {0} circle radii away."
        /// </summary>
        public static LocalisableString AverageDistance(LocalisableString radii) => new TranslatableString(getKey(@"average_distance"), @"On average {0} circle radii away.", radii);

        /// <summary>
        /// "Top among friends"
        /// </summary>
        public static LocalisableString TopAmongFriends => new TranslatableString(getKey(@"top_among_friends"), @"Top among friends");

        /// <summary>
        /// "Beatmap"
        /// </summary>
        public static LocalisableString TileBeatmap => new TranslatableString(getKey(@"tile_beatmap"), @"Beatmap");

        /// <summary>
        /// "Friends"
        /// </summary>
        public static LocalisableString TileFriends => new TranslatableString(getKey(@"tile_friends"), @"Friends");

        /// <summary>
        /// "Global"
        /// </summary>
        public static LocalisableString TileGlobal => new TranslatableString(getKey(@"tile_global"), @"Global");

        /// <summary>
        /// "Misses"
        /// </summary>
        public static LocalisableString TileMisses => new TranslatableString(getKey(@"tile_misses"), @"Misses");

        /// <summary>
        /// "Slider breaks"
        /// </summary>
        public static LocalisableString TileSliderBreaks => new TranslatableString(getKey(@"tile_slider_breaks"), @"Slider breaks");

        /// <summary>
        /// "Slider ends"
        /// </summary>
        public static LocalisableString TileSliderEnds => new TranslatableString(getKey(@"tile_slider_ends"), @"Slider ends");

        /// <summary>
        /// "Missing"
        /// </summary>
        public static LocalisableString TileMissing => new TranslatableString(getKey(@"tile_missing"), @"Missing");

        /// <summary>
        /// "Plays"
        /// </summary>
        public static LocalisableString TilePlays => new TranslatableString(getKey(@"tile_plays"), @"Plays");

        /// <summary>
        /// "Pass rate"
        /// </summary>
        public static LocalisableString TilePassRate => new TranslatableString(getKey(@"tile_pass_rate"), @"Pass rate");

        /// <summary>
        /// "Closest to FC"
        /// </summary>
        public static LocalisableString TileClosest => new TranslatableString(getKey(@"tile_closest"), @"Closest to FC");

        /// <summary>
        /// "of {0}"
        /// </summary>
        public static LocalisableString OfCount(LocalisableString count) => new TranslatableString(getKey(@"of_count"), @"of {0}", count);

        /// <summary>
        /// "{0} of {1} passed"
        /// </summary>
        public static LocalisableString PassesOf(LocalisableString passes, LocalisableString plays) => new TranslatableString(getKey(@"passes_of"), @"{0} of {1} passed", passes, plays);

        /// <summary>
        /// "{0} of {1}"
        /// </summary>
        public static LocalisableString ScoreOfMaximum(LocalisableString score, LocalisableString maximum) => new TranslatableString(getKey(@"score_of_maximum"), @"{0} of {1}", score, maximum);

        /// <summary>
        /// "FC"
        /// </summary>
        public static LocalisableString FullComboShort => new TranslatableString(getKey(@"full_combo_short"), @"FC");

        /// <summary>
        /// "Score"
        /// </summary>
        public static LocalisableString ScoreLane => new TranslatableString(getKey(@"score_lane"), @"Score");

        /// <summary>
        /// "Combo only"
        /// </summary>
        public static LocalisableString ComboOnlyLane => new TranslatableString(getKey(@"combo_only_lane"), @"Combo only");

        /// <summary>
        /// "Timing"
        /// </summary>
        public static LocalisableString TimingLane => new TranslatableString(getKey(@"timing_lane"), @"Timing");

        /// <summary>
        /// "full combo {0}"
        /// </summary>
        public static LocalisableString FullComboScore(LocalisableString score) => new TranslatableString(getKey(@"full_combo_score"), @"full combo {0}", score);

        /// <summary>
        /// "Full combo"
        /// </summary>
        public static LocalisableString FullComboToggle => new TranslatableString(getKey(@"full_combo_toggle"), @"Full combo");

        /// <summary>
        /// "Map difficulty"
        /// </summary>
        public static LocalisableString MapDifficultyLane => new TranslatableString(getKey(@"map_difficulty_lane"), @"Map difficulty");

        /// <summary>
        /// "Precision"
        /// </summary>
        public static LocalisableString Precision => new TranslatableString(getKey(@"precision"), @"Precision");

        /// <summary>
        /// "Speed"
        /// </summary>
        public static LocalisableString Speed => new TranslatableString(getKey(@"speed"), @"Speed");

        /// <summary>
        /// "hardest at {0}"
        /// </summary>
        public static LocalisableString HardestAt(LocalisableString time) => new TranslatableString(getKey(@"hardest_at"), @"hardest at {0}", time);

        /// <summary>
        /// "The beatmap's own difficulty, not how you played: precision is aim, speed is tapping."
        /// </summary>
        public static LocalisableString MapDifficultyNote => new TranslatableString(getKey(@"map_difficulty_note"),
            @"The beatmap's own difficulty, not how you played: precision is aim, speed is tapping.");

        /// <summary>
        /// "map here: precision {0} · speed {1} of its hardest"
        /// </summary>
        public static LocalisableString MapDifficultyAt(LocalisableString precision, LocalisableString speed) => new TranslatableString(getKey(@"map_difficulty_at"),
            @"map here: precision {0} · speed {1} of its hardest", precision, speed);

        /// <summary>
        /// "map here: {0} of its hardest"
        /// </summary>
        public static LocalisableString MapDifficultyTotalAt(LocalisableString share) => new TranslatableString(getKey(@"map_difficulty_total_at"),
            @"map here: {0} of its hardest", share);

        /// <summary>
        /// "Hide the full combo, to see this play's own score up close"
        /// </summary>
        public static LocalisableString HideFullCombo => new TranslatableString(getKey(@"hide_full_combo"), @"Hide the full combo, to see this play's own score up close");

        /// <summary>
        /// "Show the full combo's score against this play's"
        /// </summary>
        public static LocalisableString ShowFullCombo => new TranslatableString(getKey(@"show_full_combo"), @"Show the full combo's score against this play's");

        /// <summary>
        /// "this play {0}"
        /// </summary>
        public static LocalisableString ThisPlayScore(LocalisableString score) => new TranslatableString(getKey(@"this_play_score"), @"this play {0}", score);

        /// <summary>
        /// "combo {0}x"
        /// </summary>
        public static LocalisableString ComboAt(LocalisableString combo) => new TranslatableString(getKey(@"combo_at"), @"combo {0}x", combo);

        /// <summary>
        /// "early"
        /// </summary>
        public static LocalisableString Early => new TranslatableString(getKey(@"early"), @"early");

        /// <summary>
        /// "late"
        /// </summary>
        public static LocalisableString Late => new TranslatableString(getKey(@"late"), @"late");

        /// <summary>
        /// "{0} ms early"
        /// </summary>
        public static LocalisableString MillisecondsEarly(LocalisableString offset) => new TranslatableString(getKey(@"milliseconds_early"), @"{0} ms early", offset);

        /// <summary>
        /// "{0} ms late"
        /// </summary>
        public static LocalisableString MillisecondsLate(LocalisableString offset) => new TranslatableString(getKey(@"milliseconds_late"), @"{0} ms late", offset);

        /// <summary>
        /// "on time"
        /// </summary>
        public static LocalisableString OnTime => new TranslatableString(getKey(@"on_time"), @"on time");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
