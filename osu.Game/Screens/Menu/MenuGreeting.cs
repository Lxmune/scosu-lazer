// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// scosu: the greeting at the top of the login briefing, picked at random among those for the time of day.
    /// Mostly English, with some Japanese (romanised) and French. They aren't translated: they are as much flavour as words.
    /// <c>{0}</c> is the username.
    /// </summary>
    public static class MenuGreeting
    {
        public enum TimeOfDay
        {
            /// <summary>
            /// 5:00 to 11:59.
            /// </summary>
            Morning,

            /// <summary>
            /// 12:00 to 17:59.
            /// </summary>
            Afternoon,

            /// <summary>
            /// 18:00 to 21:59.
            /// </summary>
            Evening,

            /// <summary>
            /// 22:00 to 4:59.
            /// </summary>
            Night,
        }

        public static readonly string[] MORNING =
        {
            "Good morning",
            "Good morning, {0}",
            "Morning, {0}",
            "Ohayō, {0}",
            "Ohayō gozaimasu",
            "Bonjour, {0}",
            "Bon matin, {0}",
            "Rise and click, {0}",
            "Coffee first, {0}?",
            "Early bird gets the FC",
            "Fresh day, fresh streak",
            "Croissant and a stream?",
            "Top of the morning, {0}",
        };

        public static readonly string[] AFTERNOON =
        {
            "Good afternoon",
            "Good afternoon, {0}",
            "Afternoon, {0}",
            "Konnichiwa, {0}",
            "Yōkoso, {0}",
            "Bon après-midi, {0}",
            "Salut, {0} !",
            "Ça va, {0} ?",
            "Lunch break warm-up?",
            "Tea time, aim time",
            "Back for more, {0}?",
            "Hey {0}, ready to farm?",
            "A fine day for a full combo",
        };

        public static readonly string[] EVENING =
        {
            "Good evening",
            "Good evening, {0}",
            "Evening, {0}",
            "Konbanwa, {0}",
            "Okaeri, {0}",
            "Otsukaresama, {0}",
            "Bonsoir, {0}",
            "Bonne soirée, {0}",
            "One more map before dinner?",
            "Golden hour, golden ranks",
            "The evening grind begins",
            "How was your day, {0}?",
            "Evening session, {0}?",
        };

        public static readonly string[] NIGHT =
        {
            "Up late, {0}?",
            "Still awake, {0}?",
            "Hello, night owl",
            "Night owl mode, {0}",
            "Oyasumi? Not yet, {0}",
            "Yofukashi, {0}?",
            "Bonne nuit… presque",
            "Couche-tard, {0} ?",
            "Midnight farming, {0}?",
            "Good night… or one more?",
            "The stars are out, {0}",
            "Late streams, quiet keys",
            "Just one more try",
        };

        public static TimeOfDay At(DateTimeOffset localTime)
        {
            int hour = localTime.Hour;

            if (hour >= 5 && hour < 12)
                return TimeOfDay.Morning;
            if (hour >= 12 && hour < 18)
                return TimeOfDay.Afternoon;
            if (hour >= 18 && hour < 22)
                return TimeOfDay.Evening;

            return TimeOfDay.Night;
        }

        public static string[] For(TimeOfDay timeOfDay) => timeOfDay switch
        {
            TimeOfDay.Morning => MORNING,
            TimeOfDay.Afternoon => AFTERNOON,
            TimeOfDay.Evening => EVENING,
            _ => NIGHT,
        };

        /// <summary>
        /// A greeting for <paramref name="localTime"/>, with <paramref name="username"/> in it where it has a place for it.
        /// </summary>
        public static string Pick(DateTimeOffset localTime, string username, Random random)
        {
            string[] greetings = For(At(localTime));
            return string.Format(greetings[random.Next(greetings.Length)], username);
        }
    }
}
