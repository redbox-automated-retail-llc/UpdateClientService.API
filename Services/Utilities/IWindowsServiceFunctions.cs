namespace UpdateClientService.API.Services.Utilities
{
    public interface IWindowsServiceFunctions
    {
        bool ServiceExists(string name);

        void StartService(string name);

        void StopService(string name);

        void InstallService(
          string name,
          string displayName,
          string binPath,
          string type = null,
          string startType = null,
          string dependencies = null);
    }
}
