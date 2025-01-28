using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.KioskCertificate
{
    public static class Extensions
    {
        public static IServiceCollection AddKioskCertificatesJob(this IServiceCollection services)
        {
            return ServiceCollectionServiceExtensions.AddScoped<IKioskCertificatesJob, KioskCertificatesJob>(services);
        }
    }
}
