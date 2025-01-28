using System.Collections.Generic;

namespace UpdateClientService.API.Services
{
    public interface IStatusService
    {
        FileVersionDataResponse GetFileVersions();

        FileVersionDataResponse GetFileVersions(IEnumerable<string> filepaths);
    }
}
