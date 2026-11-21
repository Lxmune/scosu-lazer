// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace osu.Game.Online.Rooms
{
    /// <summary>
    /// scosu: signs score submissions, so the server can tell a release build of the game from a script that only knows the API.
    /// The server's side is <c>src/score_signature.rs</c>, which has the full description; both check <see cref="TEST_VECTOR_SIGNATURE"/>.
    /// <para>
    /// This only raises the bar. This source is public and the key is built into every release, so anyone who extracts it from a binary
    /// (or patches the game to sign for them) can sign anything. Development builds have no key and send no signature.
    /// </para>
    /// <para>
    /// The signature is <c>"v1=" + hex(HMAC-SHA256(key, "scosu-score-v1\n" + target + "\n" + body))</c>, where <c>target</c> is the request's path after
    /// <c>api/v2/</c> (it names the score token, and the map or room) and <c>body</c> is the request's JSON body, as UTF-8, exactly as sent.
    /// </para>
    /// </summary>
    public static class ScoreSignature
    {
        public const string HEADER = @"X-Scosu-Signature";

        /// <summary>
        /// The assembly metadata the release build puts the key in (see <c>osu.Game.csproj</c>).
        /// </summary>
        public const string KEY_METADATA = @"ScosuScoreSigningKey";

        /// <summary>
        /// The key of this build, or <c>null</c> if it has none (a development build).
        /// </summary>
        private static readonly string? key = typeof(ScoreSignature).Assembly
                                                                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                    .FirstOrDefault(a => a.Key == KEY_METADATA)?.Value?
                                                                    .Trim() is { Length: > 0 } built
            ? built
            : null;

        /// <summary>
        /// The header's value for a submission, or <c>null</c> if this build can't sign.
        /// </summary>
        public static string? Sign(string target, string body) => key == null ? null : Sign(key, target, body);

        public static string Sign(string key, string target, string body)
        {
            byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"scosu-score-v1\n{target}\n{body}"));
            return $"v1={Convert.ToHexStringLower(hash)}";
        }

        /// <summary>
        /// The signature of the vector the server's tests check too: key <c>test-signing-key-not-secret</c>, target <c>beatmaps/123/solo/scores/456</c>,
        /// body <c>{"ruleset_id":0,"passed":true,"total_score":1234567}</c>.
        /// </summary>
        public const string TEST_VECTOR_SIGNATURE = @"v1=ae634c02a139dc19818c75c38b0d439b3b0a21a0b7f97d3b4a845e609a624674";
    }
}
