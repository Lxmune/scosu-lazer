// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Online
{
    public class ProductionEndpointConfiguration : EndpointConfiguration
    {
        /// <summary>
        /// The scosu server, used when no custom server is set.
        /// </summary>
        public const string DEFAULT_SERVER_URL = @"https://lazer.scosu.net";

        /// <param name="serverUrl">
        /// The root URL of the server to connect to, or <see langword="null"/> or empty for <see cref="DEFAULT_SERVER_URL"/>.
        /// Must be accepted by <see cref="TryNormaliseServerUrl"/>, otherwise <see cref="DEFAULT_SERVER_URL"/> is used.
        /// </param>
        public ProductionEndpointConfiguration(string? serverUrl = null)
        {
            if (!TryNormaliseServerUrl(serverUrl, out string url))
                url = DEFAULT_SERVER_URL;

            WebsiteUrl = APIUrl = url;
            // lazer's own public client credentials, which scosu servers expect.
            APIClientSecret = @"FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk";
            APIClientID = "5";
            SpectatorUrl = $@"{url}/signalr/spectator";
            MultiplayerUrl = $@"{url}/signalr/multiplayer";
            MetadataUrl = $@"{url}/signalr/metadata";
            RanksByStableScore = new Uri(url).Host == new Uri(DEFAULT_SERVER_URL).Host;
        }

        /// <summary>
        /// Checks that <paramref name="serverUrl"/> is the HTTPS address of a server's root (or empty, meaning <see cref="DEFAULT_SERVER_URL"/>),
        /// and returns it without a trailing slash.
        /// </summary>
        public static bool TryNormaliseServerUrl(string? serverUrl, out string normalised)
        {
            normalised = DEFAULT_SERVER_URL;

            if (string.IsNullOrWhiteSpace(serverUrl))
                return true;

            if (!Uri.TryCreate(serverUrl.Trim(), UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps
                || uri.PathAndQuery != "/" || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                return false;

            normalised = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }
    }
}
