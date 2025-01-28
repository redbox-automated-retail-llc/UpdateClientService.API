namespace UpdateClientService.API.Services
{
    public interface IStoreService
    {
        long KioskId { get; }

        string Market { get; }

        string DataPath { get; }

        string RunningPath { get; }
    }
}
