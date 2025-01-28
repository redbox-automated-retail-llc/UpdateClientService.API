using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public class KioskUploadFileRequest
    {
        public string KioskId { get; set; }

        public string S3PreSignedUrl { get; set; }

        public string FileType { get; set; }

        public List<string> FileNames { get; set; }

        public bool ZipFiles { get; set; }

        public string BasePath { get; set; }
    }
}
