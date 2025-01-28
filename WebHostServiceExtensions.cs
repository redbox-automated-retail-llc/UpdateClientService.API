using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ServiceProcess;

namespace UpdateClientService.API
{
    public static class WebHostServiceExtensions
    {
        public static void RunAsWindowsService(this IWebHost host)
        {
            using (ServiceProviderServiceExtensions.CreateScope(host.Services))
                ServiceBase.Run((ServiceBase)new ApplicationWebHostService(host, ServiceProviderServiceExtensions.GetRequiredService<ILogger<ApplicationWebHostService>>(host.Services)));
        }
    }
}
