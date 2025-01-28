using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Utilities
{
    public class CleanupService : ICleanupService, IInvocable
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<CleanupService> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public CleanupService(IOptions<AppSettings> appSettings, ILogger<CleanupService> logger)
        {
            this._appSettings = appSettings;
            this._logger = logger;
        }

        public async Task Invoke() => await this.DoWork();

        private async Task DoWork()
        {
            await this._lock.WaitAsync();
            this._logger.LogInfoWithSource("Begin Cleanup of Old Files.", nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
            IEnumerable<CleanupFiles> cleanUpFiles = this._appSettings.Value.CleanUpFiles;
            if (!cleanUpFiles.Any<CleanupFiles>())
            {
                this._logger.LogInfoWithSource("No Files Defined For Cleanup.", nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
            }
            else
            {
                int num1 = 0;
                foreach (CleanupFiles cleanupFiles in cleanUpFiles)
                {
                    try
                    {
                        if (!Directory.Exists(cleanupFiles.Path))
                        {
                            this._logger.LogErrorWithSource(string.Format("Skipping files in folder {0} with maxage {1} - Directory doesn't exist.", (object)cleanupFiles.Path, (object)cleanupFiles.MaxAge), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                        }
                        else
                        {
                            string[] files = Directory.GetFiles(cleanupFiles.Path);
                            this._logger.LogInfoWithSource(string.Format("Cleanup Folder {0} with {1} files.", (object)cleanupFiles.Path, (object)((IEnumerable<string>)files).Count<string>()), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                            int num2 = 0;
                            foreach (string path in files)
                            {
                                try
                                {
                                    DateTime lastWriteTime = File.GetLastWriteTime(path);
                                    DateTime date = lastWriteTime.Date;
                                    DateTime dateTime1 = DateTime.Now;
                                    dateTime1 = dateTime1.Date;
                                    DateTime dateTime2 = dateTime1.AddDays((double)(-1 * cleanupFiles.MaxAge));
                                    if (date < dateTime2)
                                    {
                                        ILogger<CleanupService> logger = this._logger;
                                        string str1 = path;
                                        dateTime1 = DateTime.Now;
                                        int days = (dateTime1.Date - lastWriteTime.Date).Days;
                                        string str2 = string.Format("Deleting File: {0} - it is {1} days old.", (object)str1, (object)days);
                                        this._logger.LogInfoWithSource(str2, nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                                        File.Delete(path);
                                        ++num2;
                                    }
                                    else
                                    {
                                        ILogger<CleanupService> logger = this._logger;
                                        string str3 = path;
                                        dateTime1 = DateTime.Now;
                                        int days = (dateTime1.Date - lastWriteTime.Date).Days;
                                        string str4 = string.Format("Skipping File: {0} - it is {1} days old.", (object)str3, (object)days);
                                        this._logger.LogDebugWithSource(str4, nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this._logger.LogErrorWithSource(ex, string.Format("Unhandled Exception when deleting files in folder {0} with maxage {1}", (object)cleanupFiles.Path, (object)cleanupFiles.MaxAge), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                                }
                            }
                            num1 += num2;
                            int num3 = ((IEnumerable<string>)Directory.GetFiles(cleanupFiles.Path)).Count<string>();
                            this._logger.LogInfoWithSource(string.Format("Cleanup Folder {0} Completed with {1} files remaining. {2} files were deleted.", (object)cleanupFiles.Path, (object)num3, (object)num2), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogErrorWithSource(ex, string.Format("Unhandled Exception when deleting files in folder {0} with maxage {1}", (object)cleanupFiles.Path, (object)cleanupFiles.MaxAge), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
                    }
                }
                this._logger.LogInfoWithSource(string.Format("End Cleanup of Old Files. {0} files were deleted.", (object)num1), nameof(DoWork), "/sln/src/UpdateClientService.API/Services/Utilities/CleanupService.cs");
            }
        }
    }
}
