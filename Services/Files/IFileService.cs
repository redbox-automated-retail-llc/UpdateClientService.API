using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Files
{
    public interface IFileService
    {
        Task UploadFileToS3(string presignedS3, string filePath);

        Task ZipAndUploadToS3(List<FileInfo> fileToUpload, string presignedS3);
    }
}
