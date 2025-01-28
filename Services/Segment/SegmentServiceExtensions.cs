using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.Segment
{
    public static class SegmentServiceExtensions
    {
        public static IServiceCollection AddSegmentService(this IServiceCollection serviceCollection)
        {
            ServiceCollectionServiceExtensions.AddScoped<ISegmentService, SegmentService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<ISegmentServiceJob, SegmentServiceJob>(serviceCollection);
            return serviceCollection;
        }
    }
}
