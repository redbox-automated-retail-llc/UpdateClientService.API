namespace UpdateClientService.API.Services.Kernel
{
    public interface IKernelService
    {
        bool PerformShutdown(ShutdownType shutdownType);

        void SyncTimeAndTimezone(object data);
    }
}
