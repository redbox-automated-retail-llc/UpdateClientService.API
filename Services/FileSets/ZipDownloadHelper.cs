using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UpdateClientService.API.Services.FileCache;

namespace UpdateClientService.API.Services.FileSets
{
    public class ZipDownloadHelper : IZipDownloadHelper
    {
        private readonly IFileCacheService _fileCacheService;
        private readonly ILogger<ZipDownloadHelper> _logger;

        public ZipDownloadHelper(ILogger<ZipDownloadHelper> logger, IFileCacheService fileCacheService)
        {
            this._fileCacheService = fileCacheService;
            this._logger = logger;
        }

        public bool Extract(string zipPath, RevisionChangeSetKey revisionChangeSetKey)
        {
            bool result = true;
            try
            {
                using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
                    zipArchive.Entries.Where<ZipArchiveEntry>((Func<ZipArchiveEntry, bool>)(eachZipArchiveEntry => Path.GetExtension(eachZipArchiveEntry.Name).ToLower() == ".json")).ToList<ZipArchiveEntry>().ForEach((Action<ZipArchiveEntry>)(eachJsonZipArchiveEntry =>
                    {
                        try
                        {
                            ZipDownloadHelper.ExtractionData extractionData = new ZipDownloadHelper.ExtractionData()
                            {
                                ZipArchive = zipArchive,
                                ZipArchiveEntry = eachJsonZipArchiveEntry
                            };
                            this.GetInfoText(extractionData);
                            this.GetFileData(extractionData);
                            if (extractionData.IsRevison)
                            {
                                if (!this._fileCacheService.AddRevision(revisionChangeSetKey, extractionData.FileData, extractionData.InfoText))
                                {
                                    ILogger<ZipDownloadHelper> logger = this._logger;
                                    RevisionChangeSetKey revisionChangeSetKey1 = revisionChangeSetKey;
                                    string str = "Unable to add revision for " + (revisionChangeSetKey1 != null ? revisionChangeSetKey1.IdentifyingText() : (string)null);
                                    this._logger.LogErrorWithSource(str, nameof(Extract), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                                    extractionData.IsSuccess = false;
                                }
                            }
                            else if (extractionData.IsPatchFile)
                            {
                                if (!this._fileCacheService.AddFilePatch(revisionChangeSetKey.FileSetId, extractionData.FileId, extractionData.FileRevisionId, extractionData.PatchFileRevisionId, extractionData.FileData, extractionData.InfoText))
                                {
                                    this._logger.LogErrorWithSource(string.Format("Unable to add file patch for FileSetId {0}, FileId {1}, FileRevisionId {2}, PatchFileRevision {3}", (object)revisionChangeSetKey.FileSetId, (object)extractionData.FileId, (object)extractionData.FileRevisionId, (object)extractionData.PatchFileRevisionId), nameof(Extract), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                                    extractionData.IsSuccess = false;
                                }
                            }
                            else if (!this._fileCacheService.AddFile(revisionChangeSetKey.FileSetId, extractionData.FileId, extractionData.FileRevisionId, extractionData.FileData, extractionData.InfoText))
                            {
                                this._logger.LogErrorWithSource(string.Format("Unable to add file for FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)revisionChangeSetKey.FileSetId, (object)extractionData.FileId, (object)extractionData.FileRevisionId), nameof(Extract), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                                extractionData.IsSuccess = false;
                            }
                            result &= extractionData.IsSuccess;
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Exception while extracting " + eachJsonZipArchiveEntry.Name, nameof(Extract), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                            result = false;
                        }
                    }));
            }
            catch (Exception ex)
            {
                ILogger<ZipDownloadHelper> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSetKey revisionChangeSetKey2 = revisionChangeSetKey;
                string str = "Exception while extracting files for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null) + ".";
                this._logger.LogErrorWithSource(exception, str, nameof(Extract), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                result = false;
            }
            return result;
        }

        private void GetInfoText(ZipDownloadHelper.ExtractionData extractionData)
        {
            using (Stream stream = extractionData.ZipArchiveEntry.Open())
                extractionData.InfoText = stream.ReadToEnd();
            extractionData.InfoValues = extractionData.InfoText.ToObject<Dictionary<string, object>>();
        }

        private void GetFileData(ZipDownloadHelper.ExtractionData extractionData)
        {
            string fileEntryName = Path.GetFileNameWithoutExtension(extractionData.ZipArchiveEntry.Name);
            ZipArchiveEntry zipArchiveEntry = extractionData.ZipArchive.Entries.Where<ZipArchiveEntry>((Func<ZipArchiveEntry, bool>)(e => e.Name.Equals(fileEntryName))).FirstOrDefault<ZipArchiveEntry>();
            if (zipArchiveEntry == null)
            {
                this._logger.LogErrorWithSource(fileEntryName + " is missing from zip file.", nameof(GetFileData), "/sln/src/UpdateClientService.API/Services/FileSets/ZipDownloadHelper.cs");
                extractionData.IsSuccess = false;
            }
            else
            {
                using (Stream stream = zipArchiveEntry.Open())
                    extractionData.FileData = stream.GetBytes();
            }
        }

        private class ExtractionData
        {
            public bool IsSuccess { get; set; } = true;

            public string InfoText { get; set; }

            public byte[] FileData { get; set; }

            public Dictionary<string, object> InfoValues { get; set; } = new Dictionary<string, object>();

            public ZipArchive ZipArchive { get; set; }

            public ZipArchiveEntry ZipArchiveEntry { get; set; }

            public bool IsRevison => this.InfoValues.ContainsKey("RevisionId");

            public bool IsPatchFile => this.InfoValues.ContainsKey("PatchFileRevisionId");

            public long FileId => Convert.ToInt64(this.InfoValues[nameof(FileId)]);

            public long FileRevisionId => Convert.ToInt64(this.InfoValues[nameof(FileRevisionId)]);

            public long PatchFileRevisionId
            {
                get => Convert.ToInt64(this.InfoValues[nameof(PatchFileRevisionId)]);
            }
        }
    }
}
