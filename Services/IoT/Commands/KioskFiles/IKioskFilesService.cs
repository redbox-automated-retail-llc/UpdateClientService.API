using System.Collections.Generic;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public interface IKioskFilesService
    {
        Task<MqttResponse<Dictionary<string, string>>> PeekRequestedFilesAsync(
          KioskFilePeekRequest logRequestModel);

        Task<MqttResponse<string>> UploadFilesAsync(KioskUploadFileRequest kioskFileRequest);
    }
}
