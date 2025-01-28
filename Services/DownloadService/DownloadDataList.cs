using System;
using System.Collections.Generic;
using System.Linq;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.DownloadService
{
    public class DownloadDataList : List<DownloadData>
    {
        public DownloadDataList GetByFileSetId(long fileSetId)
        {
            DownloadDataList byFileSetId = new DownloadDataList();
            foreach (DownloadData downloadData in (List<DownloadData>)this)
            {
                if (downloadData.ParseFileSetIdFromDownloadDataKey() == fileSetId)
                    byFileSetId.Add(downloadData);
            }
            return byFileSetId;
        }

        public DownloadData GetByRevisionChangeSetKey(RevisionChangeSetKey revisionChangeSetKey)
        {
            string key = DownloadData.GetRevisionKey(revisionChangeSetKey);
            return this.Where<DownloadData>((Func<DownloadData, bool>)(d => d.Key == key)).FirstOrDefault<DownloadData>();
        }

        public DownloadData GetByBitsGuid(Guid bitsGuid)
        {
            return this.FirstOrDefault<DownloadData>((Func<DownloadData, bool>)(downloadData => downloadData.BitsGuid == bitsGuid));
        }

        public bool ExistsByKey(string key)
        {
            return this.Any<DownloadData>((Func<DownloadData, bool>)(downloadData => downloadData.Key == key));
        }

        public bool IsDownloading
        {
            get
            {
                return this.Any<DownloadData>((Func<DownloadData, bool>)(eachDownloadData => eachDownloadData.DownloadState == DownloadState.Downloading || eachDownloadData.DownloadState == DownloadState.Error));
            }
        }
    }
}
