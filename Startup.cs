using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using UpdateClientService.API.App;
using UpdateClientService.API.Services;
using UpdateClientService.API.Services.Broker;
using UpdateClientService.API.Services.Configuration;
using UpdateClientService.API.Services.DataUpdate;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.FileCache;
using UpdateClientService.API.Services.Files;
using UpdateClientService.API.Services.FileSets;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Certificate;
using UpdateClientService.API.Services.IoT.Certificate.Security;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.Commands.Controller;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;
using UpdateClientService.API.Services.IoT.DownloadFiles;
using UpdateClientService.API.Services.IoT.FileSets;
using UpdateClientService.API.Services.IoT.IoTCommand;
using UpdateClientService.API.Services.IoT.Security.Certificate;
using UpdateClientService.API.Services.Kernel;
using UpdateClientService.API.Services.KioskCertificate;
using UpdateClientService.API.Services.KioskEngine;
using UpdateClientService.API.Services.ProxyApi;
using UpdateClientService.API.Services.Segment;
using UpdateClientService.API.Services.Transfer;
using UpdateClientService.API.Services.Utilities;

namespace UpdateClientService.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => this.Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            MvcJsonMvcBuilderExtensions.AddJsonOptions(MvcServiceCollectionExtensions.AddMvc(services, (Action<MvcOptions>)(options => options.AddLoggingFilter())), (Action<MvcJsonOptions>)(options => options.SerializerSettings.Converters.Add((JsonConverter)new StringEnumConverter()))).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            RoutingServiceCollectionExtensions.AddRouting(services, (Action<RouteOptions>)(options => options.LowercaseUrls = true));
            OptionsConfigurationServiceCollectionExtensions.Configure<AppSettings>(services, (IConfiguration)this.Configuration.GetSection("AppSettings"));
            ServiceCollectionServiceExtensions.AddSingleton<IStoreService, StoreService>(services);
            services.AddMqttService();
            ServiceCollectionServiceExtensions.AddSingleton<IIotCommandDispatch, IotCommandDispatch>(services);
            ServiceCollectionServiceExtensions.AddScoped<IIoTCommandService, IoTCommandService>(services);
            ServiceCollectionServiceExtensions.AddSingleton<ICertificateService, CertificateService>(services);
            ServiceCollectionServiceExtensions.AddSingleton<IIoTCertificateServiceApiClient, IoTCertificateServiceApiClient>(services);
            ServiceCollectionServiceExtensions.AddSingleton<IPersistentDataCacheService, PersistentDataCacheService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IDownloadService, UpdateClientService.API.Services.DownloadService.DownloadService>(services);
            ServiceCollectionServiceExtensions.AddScoped<ICleanupService, CleanupService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IIoTProcessStatusService, IoTProcessStatusService>(services);
            ServiceCollectionServiceExtensions.AddSingleton<IActiveResponseService, ActiveResponsesService>(services);
            services.AddFileSetService();
            ServiceCollectionServiceExtensions.AddScoped<IKioskEngineService, KioskEngineService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileService, FileService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IStatusService, StatusService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IIoTCommandClient, IoTCommandClient>(services);
            ServiceCollectionServiceExtensions.AddScoped<IKioskFilesService, KioskFilesService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IDownloadFilesService, DownloadFilesService>(services);
            ServiceCollectionServiceExtensions.AddScoped<DownloadFilesServiceJob>(services);
            ServiceCollectionServiceExtensions.AddScoped<ITransferService, TransferService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IDownloader, BitsDownloader>(services);
            ServiceCollectionServiceExtensions.AddSingleton<ISecurityService, SecurityService>(services);
            ServiceCollectionServiceExtensions.AddSingleton<IEncryptionService, EncryptionService>(services);
            ServiceCollectionServiceExtensions.AddSingleton<IHashService, HashService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IKioskFileSetVersionsService, KioskFileSetVersionsService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetVersionsJob, FileSetVersionsJob>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileCacheService, FileCacheService>(services);
            services.AddChangeSetFileService();
            ServiceCollectionServiceExtensions.AddScoped<IFileSetCleanup, FileSetCleanup>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetCleanupJob, FileSetCleanupJob>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetDownloader, FileSetDownloader>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetRevisionDownloader, FileSetRevisionDownloader>(services);
            ServiceCollectionServiceExtensions.AddScoped<IFileSetTransition, FileSetTransition>(services);
            ServiceCollectionServiceExtensions.AddScoped<IZipDownloadHelper, ZipDownloadHelper>(services);
            ServiceCollectionServiceExtensions.AddScoped<ICommandLineService, CommandLineService>(services);
            ServiceCollectionServiceExtensions.AddScoped<IWindowsServiceFunctions, WindowsServiceFunctions>(services);
            ServiceCollectionServiceExtensions.AddScoped<IKernelService, KernelService>(services);
            services.AddConfigurationService(this.Configuration);
            services.AddSegmentService();
            ServiceCollectionServiceExtensions.AddScoped<IDataUpdateService, DataUpdateService>(services);
            services.AddKioskCertificatesJob();
            services.AddBrokerService();
            services.AddProxyApi();
            SchedulerServiceRegistration.AddScheduler(services);
            ServiceCollectionExtensions.AddHttpService(services, true, (Func<HttpClientHandler>)(() => new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            }));
            NewtonsoftServiceCollectionExtensions.AddSwaggerGenNewtonsoftSupport(services);
            SwaggerGenServiceCollectionExtensions.AddSwaggerGen(services, (Action<SwaggerGenOptions>)(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "Update Client API",
                    Version = "v1"
                });
                string str = Path.Combine(AppContext.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".xml");
                if (!File.Exists(str))
                    return;
                c.IncludeXmlComments(str);
            }));
            this.RegisterICommand(services);
        }

        public void Configure(
          IApplicationBuilder app,
          IHostingEnvironment env,
          ILogger<Startup> logger,
          IOptionsSnapshot<AppSettings> settings,
          IOptionsSnapshotKioskConfiguration kioskConfiguration)
        {
            if (HostingEnvironmentExtensions.IsDevelopment(env))
                DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(app);
            else
                HstsBuilderExtensions.UseHsts(app);
            HttpsPolicyBuilderExtensions.UseHttpsRedirection(app);
            SwaggerBuilderExtensions.UseSwagger(app, (Action<SwaggerOptions>)null);
            SwaggerUIBuilderExtensions.UseSwaggerUI(app, (Action<SwaggerUIOptions>)(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1")));
            app.UseMvc();
            this.InitializeSharedLogger(app);
            this.ScheduleServices(app, env, logger, settings);
            kioskConfiguration.Log();
        }

        private void RegisterICommand(IServiceCollection services)
        {
            Type icommandType = typeof(ICommandIoTController);
            IEnumerable<Type> types = ((IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies()).SelectMany<Assembly, Type>((Func<Assembly, IEnumerable<Type>>)(s => (IEnumerable<Type>)s.GetTypes())).Where<Type>((Func<Type, bool>)(p => icommandType.IsAssignableFrom(p) && !p.IsInterface));
            List<ICommandIoTController> commandIoTcontrollerList = new List<ICommandIoTController>();
            foreach (Type type in types)
            {
                ServiceCollectionServiceExtensions.AddScoped(services, typeof(ICommandIoTController), type);
                commandIoTcontrollerList.Add(type as ICommandIoTController);
            }
            ServiceCollectionServiceExtensions.AddScoped(services, typeof(List<ICommandIoTController>), typeof(List<ICommandIoTController>));
        }

        private void ScheduleServices(
          IApplicationBuilder app,
          IHostingEnvironment env,
          ILogger<Startup> logger,
          IOptionsSnapshot<AppSettings> settings)
        {
            app.ApplicationServices.UseScheduler((Action<IScheduler>)(scheduler =>
            {
                scheduler.Schedule<MqttProxyJob>().EveryMinute().PreventOverlapping("MqttProxyJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.Schedule<IFileSetVersionsJob>().EveryFiveMinutes().PreventOverlapping("IFileSetVersionsJob");
                else
                    scheduler.Schedule<IFileSetVersionsJob>().Cron(CronConstants.AtRandomMinuteEvery12thHour).PreventOverlapping("IFileSetVersionsJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("DownloadFilesServiceJob").Schedule<DownloadFilesServiceJob>().EveryMinute().PreventOverlapping("DownloadFilesServiceJob");
                else
                    scheduler.OnWorker("DownloadFilesServiceJob").Schedule<DownloadFilesServiceJob>().EveryFiveMinutes().PreventOverlapping("DownloadFilesServiceJob");
                scheduler.OnWorker("IDownloadService").Schedule<IDownloadService>().EveryThirtySeconds().PreventOverlapping("IDownloadService");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("IFileSetProcessingJob").Schedule<IFileSetProcessingJob>().EverySeconds(20).PreventOverlapping("IFileSetProcessingJob");
                else
                    scheduler.OnWorker("IFileSetProcessingJob").Schedule<IFileSetProcessingJob>().EveryMinute().PreventOverlapping("IFileSetProcessingJob");
                scheduler.OnWorker("ICleanupService").Schedule<ICleanupService>().DailyAtHour(6).Zoned(TimeZoneInfo.Local).PreventOverlapping("ICleanupService");
                scheduler.OnWorker("ICleanupService").Schedule<ICleanupService>().DailyAtHour(15).Zoned(TimeZoneInfo.Local).PreventOverlapping("ICleanupService");
                scheduler.OnWorker("IFileSetCleanupJob").Schedule<IFileSetCleanupJob>().Cron(CronConstants.EveryXHours(12)).PreventOverlapping("IFileSetCleanupJob");
                scheduler.OnWorker("IConfigurationFileMissingJob").Schedule<IConfigurationFileMissingJob>().EveryMinute().PreventOverlapping("IConfigurationFileMissingJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("IConfigurationServiceJob").Schedule<IConfigurationServiceJob>().EveryFiveMinutes().PreventOverlapping("IConfigurationServiceJob");
                else if (((IOptions<AppSettings>)settings)?.Value?.ConfigurationSettings?.TimerIntervalHours > 0)
                    scheduler.Schedule<IConfigurationServiceJob>().Cron(CronConstants.AtRandomMinuteEveryXHours(((IOptions<AppSettings>)settings).Value.ConfigurationSettings.TimerIntervalHours)).PreventOverlapping("IConfigurationServiceJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("IConfigurationServiceUpdateStatusJob").Schedule<IConfigurationServiceUpdateStatusJob>().EveryFiveMinutes().PreventOverlapping("IConfigurationServiceUpdateStatusJob");
                else
                    scheduler.Schedule<IConfigurationServiceUpdateStatusJob>().Cron(CronConstants.AtRandomMinuteEveryXHours(2)).PreventOverlapping("IConfigurationServiceUpdateStatusJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("ISegmentServiceJob").Schedule<ISegmentServiceJob>().EveryFiveMinutes().PreventOverlapping("ISegmentServiceJob");
                else
                    scheduler.Schedule<ISegmentServiceJob>().Cron(CronConstants.AtRandomMinuteEveryXHours(1)).PreventOverlapping("ISegmentServiceJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("IKioskCertificatesJob").Schedule<IKioskCertificatesJob>().EverySeconds(20).PreventOverlapping("IKioskCertificatesJob");
                else
                    scheduler.OnWorker("IKioskCertificatesJob").Schedule<IKioskCertificatesJob>().EveryTenMinutes().PreventOverlapping("IKioskCertificatesJob");
                if (HostingEnvironmentExtensions.IsDevelopment(env))
                    scheduler.OnWorker("IReportFailedPingsJob").Schedule<IReportFailedPingsJob>().EveryFiveMinutes().PreventOverlapping("IReportFailedPingsJob");
                else
                    scheduler.OnWorker("IReportFailedPingsJob").Schedule<IReportFailedPingsJob>().EveryFifteenMinutes().PreventOverlapping("IReportFailedPingsJob");
            })).OnError((exception => logger.LogError(exception, "Coravel global error", Array.Empty<object>())));
        }

        private void InitializeSharedLogger(IApplicationBuilder app)
        {
            SharedLogger.Factory = ServiceProviderServiceExtensions.GetRequiredService<ILoggerFactory>(app.ApplicationServices);
        }
    }
}
