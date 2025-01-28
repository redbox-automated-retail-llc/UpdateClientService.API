using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Redbox.NetCore.Middleware.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = Program.GetConfiguration(new ConfigurationBuilder(), null).Build();

        public static void Main(string[] args)
        {
            bool flag = !Debugger.IsAttached && !args.Contains("--console");
            if (flag)
            {
                string fileName = Process.GetCurrentProcess().MainModule.FileName;
                string directoryName = Path.GetDirectoryName(fileName);
                Directory.SetCurrentDirectory(directoryName);
            }
            try
            {
                Logger logger = new LoggerConfiguration().ReadFrom.Configuration(Program.Configuration, null).Enrich.WithThreadId().Enrich.FromLogContext().WriteTo.Console(LogEventLevel.Verbose, "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", null, null, null, null).CreateLogger();
                Log.Logger = logger;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Encountered unhandled exception while configuring logger. Exception -> {0}", ex));
                return;
            }
            try
            {
                Log.Information("------------------------------------ Starting web host -------------------------------------");
                Log.Information("ASPNETCORE_ENVIRONMENT: " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                Log.Information(string.Format("Service version: {0}", Assembly.GetExecutingAssembly().GetName().Version));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                if (!flag)
                {
                    Log.Information("Program.Main - Starting as a console application");
                    Program.CreateWebHostBuilder(args).Build().Run();
                }
                else
                {
                    Log.Information("Program.Main - Starting as a Windows Service");
                    Program.CreateWebHostBuilder(args).Build().RunAsWindowsService();
                }
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, "Program.Main - Web host terminated with error");
            }
            finally
            {
                Log.Information("------------------------------------ Stopping web host -------------------------------------");
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            Uri serviceUri = Program.GetServiceUri(Program.Configuration);
            Log.Information(string.Format("CreateWebHostBuilder: url={0}  port={1}", serviceUri.AbsoluteUri, serviceUri.Port));
            return WebHost.CreateDefaultBuilder(args).UseUrls(new string[] { serviceUri.AbsoluteUri }).UseSetting("Port", serviceUri.Port.ToString())
                .UseSerilog(null, false)
                .ConfigureAppConfiguration(delegate (WebHostBuilderContext webHostBuilderContext, IConfigurationBuilder conf)
                {
                    Program.GetConfiguration(conf, webHostBuilderContext);
                })
                .UseStartup<Startup>()
                .UseAppMetrics();
        }

        static Uri GetServiceUri(IConfiguration config)
        {
            return new Uri(config.GetValue("AppSettings:BaseServiceUrl", string.Empty));
        }

        static IConfigurationBuilder GetConfiguration(IConfigurationBuilder builder, WebHostBuilderContext webHostBuilderContext = null)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", false, true).AddJsonFile(ConfigurationService.ConfigurationFilePath, true, true);
            string text;
            if (webHostBuilderContext == null)
            {
                text = null;
            }
            else
            {
                IHostingEnvironment hostingEnvironment = webHostBuilderContext.HostingEnvironment;
                text = ((hostingEnvironment != null) ? hostingEnvironment.EnvironmentName : null);
            }
            string text2 = text ?? "Production";
            builder.AddJsonFile("appsettings." + text2 + ".json", true, true);
            return builder;
        }

        const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";
    }
}
