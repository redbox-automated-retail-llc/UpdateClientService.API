using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace UpdateClientService.API.Services
{
    public class StatusService : IStatusService
    {
        private readonly ILogger<StatusService> _logger;
        private readonly IOptionsMonitor<AppSettings> _settings;
        private const string FILE_NOT_FOUND = "FileNotFound";

        public StatusService(ILogger<StatusService> logger, IOptionsMonitor<AppSettings> settings)
        {
            this._logger = logger;
            this._settings = settings;
        }

        public FileVersionDataResponse GetFileVersions()
        {
            FileVersionDataResponse fileVersions = new FileVersionDataResponse();
            IEnumerable<string> fileVersionPaths = this._settings?.CurrentValue?.FileVersionPaths;
            if (fileVersionPaths != null && fileVersionPaths.Any<string>())
                return this.GetFileVersions(fileVersionPaths);
            this._logger.LogErrorWithSource("No filepaths found or specified.", nameof(GetFileVersions), "/sln/src/UpdateClientService.API/Services/Status/StatusService.cs");
            fileVersions.StatusCode = HttpStatusCode.BadRequest;
            return fileVersions;
        }

        public FileVersionDataResponse GetFileVersions(IEnumerable<string> filepaths)
        {
            FileVersionDataResponse fileVersions = new FileVersionDataResponse();
            try
            {
                List<FileVersionData> results = new List<FileVersionData>();
                filepaths.AsParallel<string>().ForAll<string>((Action<string>)(file =>
                {
                    if (File.Exists(file))
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(file);
                        results.Add(new FileVersionData()
                        {
                            Name = versionInfo.FileName,
                            Version = versionInfo.FileVersion
                        });
                    }
                    else
                        results.Add(new FileVersionData()
                        {
                            Name = Path.GetFileName(file),
                            Version = "FileNotFound"
                        });
                }));
                fileVersions.Data = (IEnumerable<FileVersionData>)results;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Unhandled exception occurred in StatusService.GetFileVersions", nameof(GetFileVersions), "/sln/src/UpdateClientService.API/Services/Status/StatusService.cs");
                fileVersions.StatusCode = HttpStatusCode.InternalServerError;
            }
            return fileVersions;
        }
    }
}
