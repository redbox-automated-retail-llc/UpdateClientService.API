using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IFileSetCleanup
    {
        Task Run();
    }
}
