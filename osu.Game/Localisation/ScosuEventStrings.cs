// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Strings of the scosu! event: its main menu card and its overlay.
    /// </summary>
    public static class ScosuEventStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScosuEvent";

        /// <summary>
        /// "event"
        /// </summary>
        public static LocalisableString HeaderTitle => new TranslatableString(getKey(@"header_title"), @"event");

        /// <summary>
        /// "climb the ranking, finish the objectives, earn a title"
        /// </summary>
        public static LocalisableString HeaderDescription => new TranslatableString(getKey(@"header_description"), @"climb the ranking, finish the objectives, earn a title");

        /// <summary>
        /// "scosu! event"
        /// </summary>
        public static LocalisableString Label => new TranslatableString(getKey(@"label"), @"scosu! event");

        /// <summary>
        /// "Upcoming"
        /// </summary>
        public static LocalisableString Upcoming => new TranslatableString(getKey(@"upcoming"), @"Upcoming");

        /// <summary>
        /// "Live"
        /// </summary>
        public static LocalisableString Live => new TranslatableString(getKey(@"live"), @"Live");

        /// <summary>
        /// "Ended"
        /// </summary>
        public static LocalisableString Ended => new TranslatableString(getKey(@"ended"), @"Ended");

        /// <summary>
        /// "Starts in"
        /// </summary>
        public static LocalisableString StartsIn => new TranslatableString(getKey(@"starts_in"), @"Starts in");

        /// <summary>
        /// "Ends in"
        /// </summary>
        public static LocalisableString EndsIn => new TranslatableString(getKey(@"ends_in"), @"Ends in");

        /// <summary>
        /// "Ended on {0}"
        /// </summary>
        public static LocalisableString EndedOn(string date) => new TranslatableString(getKey(@"ended_on"), @"Ended on {0}", date);

        /// <summary>
        /// "Days"
        /// </summary>
        public static LocalisableString Days => new TranslatableString(getKey(@"days"), @"Days");

        /// <summary>
        /// "Hours"
        /// </summary>
        public static LocalisableString Hours => new TranslatableString(getKey(@"hours"), @"Hours");

        /// <summary>
        /// "Min"
        /// </summary>
        public static LocalisableString Minutes => new TranslatableString(getKey(@"minutes"), @"Min");

        /// <summary>
        /// "Sec"
        /// </summary>
        public static LocalisableString Seconds => new TranslatableString(getKey(@"seconds"), @"Sec");

        /// <summary>
        /// "Started {0}"
        /// </summary>
        public static LocalisableString Started(string date) => new TranslatableString(getKey(@"started"), @"Started {0}", date);

        /// <summary>
        /// "Ends {0}"
        /// </summary>
        public static LocalisableString Ends(string date) => new TranslatableString(getKey(@"ends"), @"Ends {0}", date);

        /// <summary>
        /// "Day {0} of {1}"
        /// </summary>
        public static LocalisableString DayOf(int day, int days) => new TranslatableString(getKey(@"day_of"), @"Day {0} of {1}", day, days);

        /// <summary>
        /// "{0} players"
        /// </summary>
        public static LocalisableString Players(LocalisableString count) => new TranslatableString(getKey(@"players"), @"{0} players", count);

        /// <summary>
        /// "Your rank"
        /// </summary>
        public static LocalisableString YourRank => new TranslatableString(getKey(@"your_rank"), @"Your rank");

        /// <summary>
        /// "of {0}"
        /// </summary>
        public static LocalisableString Of(LocalisableString count) => new TranslatableString(getKey(@"of"), @"of {0}", count);

        /// <summary>
        /// "Not ranked yet"
        /// </summary>
        public static LocalisableString NotRanked => new TranslatableString(getKey(@"not_ranked"), @"Not ranked yet");

        /// <summary>
        /// "Set a play that counts to join the ranking."
        /// </summary>
        public static LocalisableString NotRankedHint => new TranslatableString(getKey(@"not_ranked_hint"), @"Set a play that counts to join the ranking.");

        /// <summary>
        /// "Gained"
        /// </summary>
        public static LocalisableString Gained => new TranslatableString(getKey(@"gained"), @"Gained");

        /// <summary>
        /// "Score gained"
        /// </summary>
        public static LocalisableString ScoreGained => new TranslatableString(getKey(@"score_gained"), @"Score gained");

        /// <summary>
        /// "To pass {0} (#{1})"
        /// </summary>
        public static LocalisableString ToPass(string username, LocalisableString rank) => new TranslatableString(getKey(@"to_pass"), @"To pass {0} (#{1})", username, rank);

        /// <summary>
        /// "{0} more"
        /// </summary>
        public static LocalisableString More(LocalisableString score) => new TranslatableString(getKey(@"more"), @"{0} more", score);

        /// <summary>
        /// "You're in the lead!"
        /// </summary>
        public static LocalisableString Leading => new TranslatableString(getKey(@"leading"), @"You're in the lead!");

        /// <summary>
        /// "Objectives"
        /// </summary>
        public static LocalisableString Objectives => new TranslatableString(getKey(@"objectives"), @"Objectives");

        /// <summary>
        /// "{0} / {1} completed"
        /// </summary>
        public static LocalisableString Completed(int done, int total) => new TranslatableString(getKey(@"completed"), @"{0} / {1} completed", done, total);

        /// <summary>
        /// "Next: {0} ({1})"
        /// </summary>
        public static LocalisableString NextObjective(string label, string progress) => new TranslatableString(getKey(@"next_objective"), @"Next: {0} ({1})", label, progress);

        /// <summary>
        /// "Every objective done!"
        /// </summary>
        public static LocalisableString AllObjectivesDone => new TranslatableString(getKey(@"all_objectives_done"), @"Every objective done!");

        /// <summary>
        /// "Done"
        /// </summary>
        public static LocalisableString Done => new TranslatableString(getKey(@"done"), @"Done");

        /// <summary>
        /// "Open the event"
        /// </summary>
        public static LocalisableString Open => new TranslatableString(getKey(@"open"), @"Open the event");

        /// <summary>
        /// "Play now"
        /// </summary>
        public static LocalisableString PlayNow => new TranslatableString(getKey(@"play_now"), @"Play now");

        /// <summary>
        /// "On the website"
        /// </summary>
        public static LocalisableString OnTheWebsite => new TranslatableString(getKey(@"on_the_website"), @"On the website");

        /// <summary>
        /// "About"
        /// </summary>
        public static LocalisableString About => new TranslatableString(getKey(@"about"), @"About");

        /// <summary>
        /// "Ranking"
        /// </summary>
        public static LocalisableString Ranking => new TranslatableString(getKey(@"ranking"), @"Ranking");

        /// <summary>
        /// "by ranked score gained"
        /// </summary>
        public static LocalisableString RankingHint => new TranslatableString(getKey(@"ranking_hint"), @"by ranked score gained");

        /// <summary>
        /// "No one has played yet."
        /// </summary>
        public static LocalisableString NoPlayers => new TranslatableString(getKey(@"no_players"), @"No one has played yet.");

        /// <summary>
        /// "{0} plays"
        /// </summary>
        public static LocalisableString Plays(LocalisableString count) => new TranslatableString(getKey(@"plays"), @"{0} plays", count);

        /// <summary>
        /// "All {0} players on the website"
        /// </summary>
        public static LocalisableString AllPlayers(LocalisableString count) => new TranslatableString(getKey(@"all_players"), @"All {0} players on the website", count);

        /// <summary>
        /// "Rewards"
        /// </summary>
        public static LocalisableString Rewards => new TranslatableString(getKey(@"rewards"), @"Rewards");

        /// <summary>
        /// "profile titles, given when the event ends"
        /// </summary>
        public static LocalisableString RewardsHint => new TranslatableString(getKey(@"rewards_hint"), @"profile titles, given when the event ends");

        /// <summary>
        /// "Earned"
        /// </summary>
        public static LocalisableString Earned => new TranslatableString(getKey(@"earned"), @"Earned");

        /// <summary>
        /// "On track"
        /// </summary>
        public static LocalisableString OnTrack => new TranslatableString(getKey(@"on_track"), @"On track");

        /// <summary>
        /// "There is no event right now."
        /// </summary>
        public static LocalisableString None => new TranslatableString(getKey(@"none"), @"There is no event right now.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
