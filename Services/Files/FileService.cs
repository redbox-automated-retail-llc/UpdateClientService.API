using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Files
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger) => this._logger = logger;

        public async Task ZipAndUploadToS3(List<FileInfo> fileToUpload, string presignedS3)
        {
            int num = new Random().Next();
            string zipPath = Path.GetTempPath() + string.Format("{0}.zip", (object)num);
            this._logger.LogInfoWithSource("Created temporary zip path: " + zipPath, nameof(ZipAndUploadToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
            Exception exception = (Exception)null;
            try
            {
                using (ZipArchive zipTo = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (FileInfo fileInfo in fileToUpload)
                    {
                        this._logger.LogInfoWithSource("Adding " + fileInfo.Name + " into " + zipPath + " zip file.", nameof(ZipAndUploadToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
                        if (fileInfo.Length > 30000000L)
                            throw new Exception(string.Format("File: {0} size is {1} byte, exceeds 30 mb limit. Can't zip file that is larger than 30mb. Contact kiosk team for questions.", (object)fileInfo.Name, (object)fileInfo.Length));
                        ZipArchiveEntry entry = zipTo.CreateEntry(fileInfo.Name);
                        using (FileStream fileStream = File.Open(fileInfo.FullName, (FileMode)3, (FileAccess)1, (FileShare)3))
                        {
                            using (Stream streamTo = entry.Open())
                                await (fileStream).CopyToAsync(streamTo);
                        }
                    }
                }
                await this.UploadFileToS3(presignedS3, zipPath);
            }
            catch (Exception ex)
            {
                exception = ex;
                this._logger.LogErrorWithSource(ex, "Exception while creating zip file", nameof(ZipAndUploadToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
            }
            finally
            {
                this._logger.LogInfoWithSource("Deleting temporary zip file: " + zipPath, nameof(ZipAndUploadToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
                File.Delete(zipPath);
            }
            if (exception != null)
                throw exception;
        }

        public async Task UploadFileToS3(string presignedS3, string filePath)
        {
            this._logger.LogInfoWithSource("Uploading file: $" + filePath + " to given s3 presigned Url", nameof(UploadFileToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
            HttpWebRequest httpRequest = WebRequest.Create(presignedS3) as HttpWebRequest;
            httpRequest.Method = "PUT";
            using (Stream dataStream = (httpRequest).GetRequestStream())
            {
                byte[] buffer = new byte[8000];
                using (FileStream fileStream = File.Open(filePath, (FileMode)3, (FileAccess)1, (FileShare)3))
                {
                    while (true)
                    {
                        int num;
                        if ((num = await (fileStream).ReadAsync(buffer, 0, buffer.Length)) > 0)
                            await dataStream.WriteAsync(buffer, 0, num);
                        else
                            break;
                    }
                }
                buffer = null;
            }
            httpRequest.GetResponse();
            this._logger.LogInfoWithSource("Successfully uploaded " + filePath + " to the s3", nameof(UploadFileToS3), "/sln/src/UpdateClientService.API/Services/Files/FileService.cs");
            httpRequest = null;
        }
    }
}
