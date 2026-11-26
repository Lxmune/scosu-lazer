// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiscordRPC;
using DiscordRPC.Message;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Users;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace osu.Desktop
{
    internal partial class DiscordRichPresence : Component
    {
        private const string client_id = "1564082817464410132";

        private DiscordRpcClient client = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuGame game { get; set; } = null!;

        [Resolved]
        private LoginOverlay? login { get; set; }

        [Resolved]
        private MultiplayerClient multiplayerClient { get; set; } = null!;

        [Resolved]
        private LocalUserStatisticsProvider statisticsProvider { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> selectedMods { get; set; } = null!;

        private IBindable<DiscordRichPresenceMode> privacyMode = null!;
        private IBindable<UserStatus> userStatus = null!;
        private IBindable<UserActivity?> userActivity = null!;

        private readonly RichPresence presence = new RichPresence
        {
            Assets = new Assets { LargeImageKey = "osu_logo_lazer" },
            Timestamps = Timestamps.Now,
            Secrets = new Secrets
            {
                JoinSecret = null,
                SpectateSecret = null,
            },
        };

        private IBindable<APIUser>? user;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, SessionStatics session)
        {
            privacyMode = config.GetBindable<DiscordRichPresenceMode>(OsuSetting.DiscordRichPresence);
            userStatus = config.GetBindable<UserStatus>(OsuSetting.UserOnlineStatus);
            userActivity = session.GetBindable<UserActivity?>(Static.UserOnlineActivity);

            client = new DiscordRpcClient(client_id)
            {
                // SkipIdenticalPresence allows us to fire SetPresence at any point and leave it to the underlying implementation
                // to check whether a difference has actually occurred before sending a command to Discord (with a minor caveat that's handled in onReady).
                SkipIdenticalPresence = true
            };

            client.OnReady += onReady;
            client.OnError += (_, e) => Logger.Log($"An error occurred with Discord RPC Client: {e.Message} ({e.Code})", LoggingTarget.Network);

            try
            {
                client.RegisterUriScheme();
                client.Subscribe(EventType.Join);
                client.OnJoin += onJoin;
            }
            catch (Exception ex)
            {
                // This is known to fail in at least the following sandboxed environments:
                // - macOS (when packaged as an app bundle)
                // - flatpak (see: https://github.com/flathub/sh.ppy.osu/issues/170)
                // There is currently no better way to do this offered by Discord, so the best we can do is simply ignore it for now.
                Logger.Log($"Failed to register Discord URI scheme: {ex}");
            }

            client.Initialize();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            user = api.LocalUser.GetBoundCopy();

            ruleset.BindValueChanged(_ => schedulePresenceUpdate());
            // scosu: Relax and Autopilot have their own leaderboards (see RelaxLeaderboards).
            selectedMods.BindValueChanged(_ => schedulePresenceUpdate());
            userStatus.BindValueChanged(_ => schedulePresenceUpdate());
            userActivity.BindValueChanged(_ => schedulePresenceUpdate());
            privacyMode.BindValueChanged(_ => schedulePresenceUpdate());

            multiplayerClient.RoomUpdated += onRoomUpdated;
            statisticsProvider.StatisticsUpdated += onStatisticsUpdated;
        }

        private void onReady(object _, ReadyMessage __)
        {
            Logger.Log("Discord RPC Client ready.", LoggingTarget.Network, LogLevel.Debug);

            // when RPC is lost and reconnected, we have to clear presence state for updatePresence to work (see DiscordRpcClient.SkipIdenticalPresence).
            if (client.CurrentPresence != null)
                client.SetPresence(null);

            schedulePresenceUpdate();
        }

        private void onRoomUpdated() => schedulePresenceUpdate();

        private void onStatisticsUpdated(UserStatisticsUpdate _) => schedulePresenceUpdate();

        private ScheduledDelegate? presenceUpdateDelegate;

        private void schedulePresenceUpdate()
        {
            presenceUpdateDelegate?.Cancel();
            presenceUpdateDelegate = Scheduler.AddDelayed(() =>
            {
                if (!client.IsInitialized)
                    return;

                if (!api.IsLoggedIn || privacyMode.Value == DiscordRichPresenceMode.Off)
                {
                    client.ClearPresence();
                    return;
                }

                bool hideIdentifiableInformation = privacyMode.Value == DiscordRichPresenceMode.Limited || userStatus.Value == UserStatus.DoNotDisturb;

                updatePresence(hideIdentifiableInformation);
                client.SetPresence(presence);
            }, 200);
        }

        private void updatePresence(bool hideIdentifiableInformation)
        {
            if (user == null)
                return;

            // scosu: the leaderboard the play counts for, e.g. "osu! (RX)" with the RX mod.
            var leaderboard = ruleset.Value.GetLeaderboardRuleset(selectedMods.Value.Select(m => m.Acronym));
            // scosu: Discord shows at most two buttons: the player's profile (or the beatmap, when the profile is hidden) and the website.
            Button? beatmapButton = null;

            // user activity
            if (userActivity.Value != null)
            {
                presence.State = clampLength(userActivity.Value.GetStatus(hideIdentifiableInformation));
                presence.Details = clampLength(userActivity.Value.GetDetails(hideIdentifiableInformation) ?? string.Empty);

                if (userActivity.Value.GetBeatmapID(hideIdentifiableInformation) is int beatmapId && beatmapId > 0)
                {
                    beatmapButton = new Button
                    {
                        Label = "View beatmap",
                        Url = $@"{api.Endpoints.WebsiteUrl}/beatmaps/{beatmapId}?mode={ruleset.Value.ShortName}"
                    };
                }
            }
            else
            {
                presence.State = "Idle";
                presence.Details = string.Empty;
            }

            presence.Buttons = CreateButtons(api.Endpoints.WebsiteUrl, api.IsLoggedIn && !hideIdentifiableInformation ? user.Value.Id : null, leaderboard.ShortName, beatmapButton);

            // user party
            if (!hideIdentifiableInformation && multiplayerClient.Room != null && !multiplayerClient.Room.Settings.MatchType.IsMatchmakingType())
            {
                MultiplayerRoom room = multiplayerClient.Room;

                presence.Party = new Party
                {
                    Privacy = string.IsNullOrEmpty(room.Settings.Password) ? Party.PrivacySetting.Public : Party.PrivacySetting.Private,
                    ID = room.RoomID.ToString(),
                    // technically lobbies can have infinite users, but Discord needs this to be set to something.
                    // to make party display sensible, assign a powers of two above participants count (8 at minimum).
                    Max = (int)Math.Max(8, Math.Pow(2, Math.Ceiling(Math.Log2(room.Users.Count)))),
                    Size = room.Users.Count,
                };

                RoomSecret roomSecret = new RoomSecret
                {
                    RoomID = room.RoomID,
                    Password = room.Settings.Password,
                };

                if (client.HasRegisteredUriScheme)
                    presence.Secrets.JoinSecret = JsonConvert.SerializeObject(roomSecret);

                // discord cannot handle both secrets and buttons at the same time, so we need to choose something.
                // the multiplayer room seems more important.
                presence.Buttons = null;
            }
            else
            {
                presence.Party = null;
                presence.Secrets.JoinSecret = null;
            }

            // game images:
            // large image tooltip
            if (privacyMode.Value == DiscordRichPresenceMode.Limited)
                presence.Assets.LargeImageText = string.Empty;
            else
            {
                presence.Assets.LargeImageText = clampLength(largeImageText(leaderboard));
            }

            // small image: the ruleset's icon (Relax and Autopilot are played in it), named after the leaderboard.
            presence.Assets.SmallImageKey = ruleset.Value.IsLegacyRuleset() ? $"mode_{ruleset.Value.OnlineID}" : "mode_custom";
            presence.Assets.SmallImageText = leaderboard.Name;
        }

        /// <summary>
        /// scosu: "View profile" (the logged-in player's profile, on the leaderboard they're playing) and "Join scosu" (the website's home page).
        /// Without a profile to show, the beatmap being played takes its place, when there is one.
        /// </summary>
        /// <param name="websiteUrl">The website's root, without a trailing slash.</param>
        /// <param name="userId">The logged-in player, or <see langword="null"/> when nobody is logged in or the profile is hidden.</param>
        /// <param name="leaderboard">The short name of the leaderboard being played ("osu", "osurx", ...).</param>
        /// <param name="fallback">The button to show instead of the profile's.</param>
        internal static Button[] CreateButtons(string websiteUrl, int? userId, string leaderboard, Button? fallback)
        {
            var buttons = new List<Button>();

            if (userId > 1)
            {
                buttons.Add(new Button
                {
                    Label = "View profile",
                    Url = $@"{websiteUrl}/users/{userId}/{leaderboard}"
                });
            }
            else if (fallback != null)
                buttons.Add(fallback);

            buttons.Add(new Button
            {
                Label = "Join scosu",
                Url = $@"{websiteUrl}/"
            });

            return buttons.ToArray();
        }

        private void onJoin(object sender, JoinMessage args) => Scheduler.AddOnce(() =>
        {
            game.Window?.Raise();

            if (!api.IsLoggedIn)
            {
                login?.Show();
                return;
            }

            Logger.Log($"Received room secret from Discord RPC Client: \"{args.Secret}\"", LoggingTarget.Network, LogLevel.Debug);

            // Stable and lazer share the same Discord client ID, meaning they can accept join requests from each other.
            // Since they aren't compatible in multi, see if stable's format is being used and log to avoid confusion.
            if (args.Secret[0] != '{' || !tryParseRoomSecret(args.Secret, out long roomId, out string? password))
            {
                Logger.Log("Could not join multiplayer room, invitation is invalid or incompatible.", LoggingTarget.Network, LogLevel.Important);
                return;
            }

            var request = new GetRoomRequest(roomId);
            request.Success += room => Schedule(() =>
            {
                game.PresentMultiplayerMatch(room, password);
            });
            request.Failure += _ => Logger.Log($"Could not join multiplayer room, room could not be found (room ID: {roomId}).", LoggingTarget.Network, LogLevel.Important);
            api.Queue(request);
        });

        private static readonly int ellipsis_length = Encoding.UTF8.GetByteCount(new[] { '…' });

        /// <summary>
        /// scosu: "[TAG] name · #1,234 · 12.3M score", with the rank and ranked score of the leaderboard being played
        /// (scosu ranks players by ranked score).
        /// </summary>
        private string largeImageText(RulesetInfo leaderboard)
        {
            var text = new StringBuilder();

            if (!string.IsNullOrEmpty(user!.Value.Team?.ShortName))
                text.Append($@"[{user.Value.Team.ShortName}] ");
            text.Append(user.Value.Username);

            var statistics = statisticsProvider.GetStatisticsFor(leaderboard);

            if (statistics?.GlobalRank > 0)
                text.Append($@" · #{statistics.GlobalRank:N0}");
            if (statistics?.RankedScore > 0)
                text.Append($@" · {compact(statistics.RankedScore)} score");

            return text.ToString();
        }

        private static string compact(long value) => value switch
        {
            >= 1_000_000_000 => $@"{value / 1e9:0.##}B",
            >= 1_000_000 => $@"{value / 1e6:0.#}M",
            >= 1_000 => $@"{value / 1e3:0.#}K",
            _ => value.ToString(@"N0"),
        };

        private static string clampLength(string str)
        {
            // Empty strings are fine to discord even though single-character strings are not. Make it make sense.
            if (string.IsNullOrEmpty(str))
                return str;

            // As above, discord decides that *non-empty* strings shorter than 2 characters cannot possibly be valid input, because... reasons?
            // And yes, that is two *characters*, or *codepoints*, not *bytes* as further down below (as determined by empirical testing).
            // Also, spaces don't count. Because reasons, clearly.
            // That all seems very questionable, and isn't even documented anywhere. So to *make it* accept such valid input,
            // just tack on enough of U+200B ZERO WIDTH SPACEs at the end. After making sure to trim whitespace.
            string trimmed = str.Trim();
            if (trimmed.Length < 2)
                return trimmed.PadRight(2, '\u200B');

            if (Encoding.UTF8.GetByteCount(str) <= 128)
                return str;

            ReadOnlyMemory<char> strMem = str.AsMemory();

            do
            {
                strMem = strMem[..^1];
            } while (Encoding.UTF8.GetByteCount(strMem.Span) + ellipsis_length > 128);

            return string.Create(strMem.Length + 1, strMem, (span, mem) =>
            {
                mem.Span.CopyTo(span);
                span[^1] = '…';
            });
        }

        private static bool tryParseRoomSecret(string secretJson, out long roomId, out string? password)
        {
            roomId = 0;
            password = null;

            RoomSecret? roomSecret;

            try
            {
                roomSecret = JsonConvert.DeserializeObject<RoomSecret>(secretJson);
            }
            catch
            {
                return false;
            }

            if (roomSecret == null) return false;

            roomId = roomSecret.RoomID;
            password = roomSecret.Password;

            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (multiplayerClient.IsNotNull())
                multiplayerClient.RoomUpdated -= onRoomUpdated;

            if (statisticsProvider.IsNotNull())
                statisticsProvider.StatisticsUpdated -= onStatisticsUpdated;

            client.Dispose();
            base.Dispose(isDisposing);
        }

        private class RoomSecret
        {
            [JsonProperty(@"roomId", Required = Required.Always)]
            public long RoomID { get; set; }

            [JsonProperty(@"password", Required = Required.AllowNull)]
            public string? Password { get; set; }
        }
    }
}
