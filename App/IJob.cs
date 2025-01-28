using UpdateClientService.API.Services;

namespace UpdateClientService.API.App
{
    public interface IJob : IPersistentData
    {
        long Id { get; }
    }
}
