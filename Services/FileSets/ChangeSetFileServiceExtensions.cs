using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.FileSets
{
    public static class ChangeSetFileServiceExtensions
    {
        public static IServiceCollection AddChangeSetFileService(
          this IServiceCollection serviceCollection)
        {
            ServiceCollectionServiceExtensions.AddScoped<IChangeSetFileService, ChangeSetFileService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IRevisionChangeSetRepository, RevisionChangeSetRepository>(serviceCollection);
            return serviceCollection;
        }
    }
}
