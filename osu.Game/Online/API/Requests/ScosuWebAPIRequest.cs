// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// A request to the scosu server's website API (<c>/api/web</c>), which accepts the game's bearer token like <c>/api/v2</c> does.
    /// </summary>
    public abstract class ScosuWebAPIRequest<T> : APIRequest<T>
        where T : class
    {
        protected override string Uri => $@"{API!.Endpoints.APIUrl}/api/web/{Target}";
    }
}
