using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.FileSets
{
    public static class FileSetServiceExtensions
    {
        public static IServiceCollection AddFileSetService(this IServiceCollection serviceCollection)
        {
            ServiceCollectionServiceExtensions.AddScoped<IFileSetService, FileSetService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IStateFileService, StateFileService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IStateFileRepository, StateFileRepository>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetProcessingJob, FileSetProcessingJob>(serviceCollection);
            return serviceCollection;
        }
    }
}
