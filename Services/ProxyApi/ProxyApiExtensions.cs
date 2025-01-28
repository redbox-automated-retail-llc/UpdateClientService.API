using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.ProxyApi
{
    public static class ProxyApiExtensions
    {
        public static IServiceCollection AddProxyApi(this IServiceCollection services)
        {
            return ServiceCollectionServiceExtensions.AddScoped<IProxyApi, UpdateClientService.API.Services.ProxyApi.ProxyApi>(services);
        }
    }
}
