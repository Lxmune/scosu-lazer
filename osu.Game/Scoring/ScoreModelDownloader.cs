// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;

namespace osu.Game.Scoring
{
    public class ScoreModelDownloader : ModelDownloader<ScoreInfo, IScoreInfo>
    {
        public ScoreModelDownloader(IModelImporter<ScoreInfo> scoreManager, IAPIProvider api)
            : base(scoreManager, api)
        {
        }

        protected override ArchiveDownloadRequest<IScoreInfo> CreateDownloadRequest(IScoreInfo score, bool minimiseDownload) => new DownloadReplayRequest(score);

        public override ArchiveDownloadRequest<IScoreInfo>? GetExistingDownload(IScoreInfo model)
            => CurrentDownloads.Find(r => r.Model.MatchesOnlineID(model));

        public bool Download(IScoreInfo model) => Download(model, false, null);

        /// <summary>
        /// scosu: a replay downloaded for a score is that score, whatever ID its file carries. The server serves replays as they were stored,
        /// and an older one may not carry the score's ID here (or carry none), which left the replay unrecognised: still offered for download,
        /// and never found to be watched or analysed.
        /// </summary>
        protected override void PostImport(IScoreInfo model, IReadOnlyList<Live<ScoreInfo>> imported)
        {
            if (model.OnlineID <= 0)
                return;

            foreach (var score in imported)
            {
                score.PerformWrite(s =>
                {
                    if (s.OnlineID != model.OnlineID)
                        s.OnlineID = model.OnlineID;
                });
            }
        }
    }
}
