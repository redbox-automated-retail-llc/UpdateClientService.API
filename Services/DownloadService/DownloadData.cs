using System;
using System.Collections.Generic;
using System.Linq;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.DownloadService
{
    public class DownloadData : IPersistentData
    {
        public string Key { get; set; }

        public string Url { get; set; }

        public string Hash { get; set; }

        public string Path { get; set; }

        public DownloadState DownloadState { get; set; }

        public DownloadType DownloadType { get; set; }

        public DownloadPriority DownloadPriority { get; set; }

        public Guid FileGuid { get; set; }

        public Guid BitsGuid { get; set; }

        public string Message { get; set; }

        public int RetryCount { get; set; }

        public string FileName => this.Key + DownloadDataConstants.DownloadExtension + ".json";

        public bool CompleteOnFinish { get; set; }

        public long BitsTransferred { get; set; }

        public long BitsTotal { get; set; }

        public static string GetRevisionKey(RevisionChangeSetKey revisionChangeSetKey)
        {
            return string.Format("{0},{1},{2}-{3}", (object)1, (object)revisionChangeSetKey.FileSetId, (object)revisionChangeSetKey.RevisionId, (object)revisionChangeSetKey.PatchRevisionId);
        }

        public long ParseFileSetIdFromDownloadDataKey()
        {
            long fromDownloadDataKey = 0;
            string[] source = this.Key.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (((IEnumerable<string>)source).Count<string>() > 1)
                fromDownloadDataKey = Convert.ToInt64(source[1]);
            return fromDownloadDataKey;
        }

        public static DownloadData Initialize(
          string key,
          string hash,
          string url,
          DownloadPriority downloadPriority,
          bool completeOnFinish)
        {
            return new DownloadData()
            {
                Key = key,
                Url = url,
                DownloadState = DownloadState.None,
                RetryCount = 0,
                FileGuid = Guid.NewGuid(),
                Hash = hash,
                DownloadType = DownloadType.Bits,
                DownloadPriority = downloadPriority,
                CompleteOnFinish = completeOnFinish
            };
        }
    }
}
