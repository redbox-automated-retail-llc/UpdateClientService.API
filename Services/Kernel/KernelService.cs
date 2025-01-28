using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using UpdateClientService.API.Services.Configuration;
using UpdateClientService.API.Services.Utilities;

namespace UpdateClientService.API.Services.Kernel
{
    public class KernelService : IKernelService
    {
        private readonly ICommandLineService _cmd;
        private readonly ILogger<KernelService> _logger;
        private readonly KernelServiceSettings _settings;
        private readonly bool _updateTimestamp;
        private readonly bool _updateTimezone;

        public KernelService(
          ICommandLineService cmd,
          ILogger<KernelService> logger,
          IOptionsMonitor<AppSettings> settings,
          IOptionsMonitorKioskConfiguration kioskConfiguration)
        {
            this._cmd = cmd;
            this._logger = logger;
            this._settings = settings?.CurrentValue?.KernelService;
            this._updateTimestamp = kioskConfiguration.Operations.SyncTimestamp;
            this._updateTimezone = kioskConfiguration.Operations.SyncTimezone;
        }

        public bool PerformShutdown(ShutdownType shutdownType)
        {
            this._logger.LogInfoWithSource(string.Format("Performing {0}", (object)shutdownType), nameof(PerformShutdown), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
            if (!this._settings.ShutdownEnabled)
            {
                this._logger.LogWarningWithSource(string.Format("Cannot {0} because ShutdownEnabled is set to {1}", (object)shutdownType, (object)this._settings.ShutdownEnabled), nameof(PerformShutdown), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                return false;
            }
            if (shutdownType == ShutdownType.Reboot)
                return this._cmd.TryExecutePowerShellScript("Restart-Computer -Force");
            if (shutdownType == ShutdownType.Shutdown)
                return this._cmd.TryExecutePowerShellScript("Stop-Computer -Force");
            this._logger.LogWarningWithSource(string.Format("{0} was unsuccessful", (object)shutdownType), nameof(PerformShutdown), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
            return false;
        }

        public void SyncTimeAndTimezone(object data)
        {
            try
            {
                string json = data.ToJson();
                this._logger.LogInfoWithSource("sync time data: " + json, nameof(SyncTimeAndTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                KernelService.SyncTimestampAndTimeZoneData data1 = JsonConvert.DeserializeObject<KernelService.SyncTimestampAndTimeZoneData>(json);
                if (data1 == null)
                {
                    this._logger.LogErrorWithSource("unable to deserialize sync time data!", nameof(SyncTimeAndTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                }
                else
                {
                    this.UpdateTimezone(data1);
                    this.UpdateTime(data1);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogCriticalWithSource(ex, "Exception occured sync'ing kiosk time/timezone!", nameof(SyncTimeAndTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
            }
        }

        private void UpdateTimezone(KernelService.SyncTimestampAndTimeZoneData data)
        {
            if (this._updateTimezone)
            {
                if (string.IsNullOrEmpty(data.Timezone))
                {
                    this._logger.LogErrorWithSource("Empty time zone returned from server!", nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                }
                else
                {
                    TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(data.Timezone);
                    switch (TimeZoneFunctions.SetTimeZone(systemTimeZoneById))
                    {
                        case TimeZoneFunctions.SetTimeZoneResult.Same:
                            this._logger.LogInfoWithSource("Client and Server time zones match", nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                            break;
                        case TimeZoneFunctions.SetTimeZoneResult.Changed:
                            this._logger.LogInfoWithSource("Client zone changed to " + systemTimeZoneById.DisplayName, nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                            break;
                        case TimeZoneFunctions.SetTimeZoneResult.Errored:
                            this._logger.LogErrorWithSource("Failed to set TimeZone for the kiosk!", nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                            break;
                        default:
                            this._logger.LogErrorWithSource("Unknown error setting the kiosk time zone!", nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                            break;
                    }
                }
            }
            else
                this._logger.LogInfoWithSource("Skipping time zone sync; configuration off", nameof(UpdateTimezone), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
        }

        private void UpdateTime(KernelService.SyncTimestampAndTimeZoneData data)
        {
            if (this._updateTimestamp)
            {
                switch (TimeZoneFunctions.SetTime(data.UtcTimestamp))
                {
                    case TimeZoneFunctions.SetTimeResult.InRange:
                        this._logger.LogInfoWithSource("Kiosk time is in range", nameof(UpdateTime), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                        break;
                    case TimeZoneFunctions.SetTimeResult.Changed:
                        this._logger.LogInfoWithSource("Kiosk time changed to match server time", nameof(UpdateTime), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                        break;
                    case TimeZoneFunctions.SetTimeResult.Errored:
                        this._logger.LogErrorWithSource("Failed to set date/time for the kiosk!", nameof(UpdateTime), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                        break;
                    default:
                        this._logger.LogErrorWithSource("Unknown error setting the kiosk time!", nameof(UpdateTime), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
                        break;
                }
            }
            else
                this._logger.LogInfoWithSource("Skipping time sync; configuration off", nameof(UpdateTime), "/sln/src/UpdateClientService.API/Services/Kernel/KernelService.cs");
        }

        private class SyncTimestampAndTimeZoneData
        {
            public DateTime UtcTimestamp { get; set; }

            public string Timezone { get; set; }
        }
    }
}
