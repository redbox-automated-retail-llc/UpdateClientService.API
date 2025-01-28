using System.Threading;

namespace UpdateClientService.API.Services.IoT.IoTCommand
{
    public static class ConnectionTestLock
    {
        public static SemaphoreSlim Lock { get; set; } = new SemaphoreSlim(1, 1);
    }
}
