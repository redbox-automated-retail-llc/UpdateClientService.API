using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Certificate;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class RenewIoTCertificates : ICommandIoTController
    {
        private readonly ICertificateService _certificateInstaller;

        public CommandEnum CommandEnum => CommandEnum.RenewIoTCertificates;

        public int Version => 1;

        public RenewIoTCertificates(ICertificateService certificateInstaller)
        {
            this._certificateInstaller = certificateInstaller;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            IotCert certificateAsync = await this._certificateInstaller.GetCertificateAsync(true);
        }
    }
}
