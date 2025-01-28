using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;
using UpdateClientService.API.Services.ProxyApi;

namespace UpdateClientService.API.Services.Broker
{
    public class PingStatisticsService : IPingStatisticsService
    {
        private readonly IPersistentDataCacheService _persistentDataCacheService;
        private readonly ILogger<PingStatisticsService> _logger;
        private readonly IKioskConfiguration _kioskConfiguration;
        private readonly IProxyApi _proxyApi;
        private readonly IStoreService _storeService;
        private PingStatistics _pingStatistics;
        private const string PingStatisticsFileName = "PingStatistics.json";
        private static DateTime _initialReportFailedPingAttempt = DateTime.Now;
        private const int _initialDelayMinutes = 30;
        private const int _reportFailedPingAttemptWaitingPeriodMinutes = 60;

        public PingStatisticsService(
          ILogger<PingStatisticsService> logger,
          IPersistentDataCacheService persistentDataCacheService,
          IOptionsKioskConfiguration kioskConfiguration,
          IProxyApi proxyApi,
          IStoreService storeService)
        {
            this._logger = logger;
            this._persistentDataCacheService = persistentDataCacheService;
            this._kioskConfiguration = (IKioskConfiguration)kioskConfiguration;
            this._proxyApi = proxyApi;
            this._storeService = storeService;
        }

        public async Task<bool> RecordPingStatistic(bool pingSuccess)
        {
            bool flag;
            try
            {
                PingStatistics pingStatistics = await this.GetPingStatistics();
                pingStatistics.Cleanup();
                PingStatistic pingStatistic = pingStatistics.GetLatest();
                DateTime now = DateTime.Now;
                if (pingStatistic == null || pingStatistic.PingSuccess != pingSuccess)
                {
                    pingStatistic = new PingStatistic()
                    {
                        StartInterval = now,
                        StartIntervalUTC = now.ToUniversalTime(),
                        PingSuccess = pingSuccess
                    };
                    pingStatistics.Add(pingStatistic);
                }
                pingStatistic.EndInterval = now;
                pingStatistic.EndIntervalUTC = now.ToUniversalTime();
                ++pingStatistic.PingCount;
                flag = await this.SavePingStatistics(pingStatistics);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(string.Format("Failed to record ping attempt: {0}", (object)ex), nameof(RecordPingStatistic), "/sln/src/UpdateClientService.API/Services/Broker/PingStatisticsService.cs");
                flag = false;
            }
            return flag;
        }

        public async Task<PingStatisticsResponse> GetPingStatisticsResponse()
        {
            PingStatisticsResponse response = new PingStatisticsResponse();
            try
            {
                PingStatisticsResponse statisticsResponse = response;
                statisticsResponse.PingStatistics = await this.GetPingStatistics();
                statisticsResponse = (PingStatisticsResponse)null;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception getting Ping Statistics Response", nameof(GetPingStatisticsResponse), "/sln/src/UpdateClientService.API/Services/Broker/PingStatisticsService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public async Task<LastSuccessfulPingResponse> GetLastSuccessfulPing()
        {
            LastSuccessfulPingResponse response = new LastSuccessfulPingResponse();
            try
            {
                response.LastSuccessfulPingUTC = (await this.GetPingStatistics())?.GetLastSuccessful()?.EndIntervalUTC;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception getting last successful ping", nameof(GetLastSuccessfulPing), "/sln/src/UpdateClientService.API/Services/Broker/PingStatisticsService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        private async Task<PingStatistics> GetPingStatistics()
        {
            if (this._pingStatistics == null)
            {
                PersistentDataWrapper<PingStatistics> persistentDataWrapper = await this._persistentDataCacheService.Read<PingStatistics>("PingStatistics.json", log: false);
                this._pingStatistics = persistentDataWrapper?.Data == null ? new PingStatistics() : persistentDataWrapper.Data;
            }
            return this._pingStatistics;
        }

        private async Task<bool> SavePingStatistics(PingStatistics pingStatistics)
        {
            return await this._persistentDataCacheService.Write<PingStatistics>(pingStatistics, "PingStatistics.json");
        }

        public async Task<bool> ReportFailedPing()
        {
            bool result = true;
            try
            {
                if (DateTime.Now > PingStatisticsService._initialReportFailedPingAttempt.AddMinutes(30.0))
                {
                    PingStatistics pingStatistics = await this.GetPingStatistics();
                    PingStatistic latest = pingStatistics.GetLatest();
                    DateTime? endIntervalUtc = pingStatistics?.GetLastSuccessful()?.EndIntervalUTC;
                    if (endIntervalUtc.HasValue)
                    {
                        if (endIntervalUtc.Value.AddMinutes(60.0) < DateTime.UtcNow)
                        {
                            if (latest != null)
                            {
                                if (!latest.PingSuccess)
                                {
                                    if (this._kioskConfiguration.KioskHealth.ReportFailedPingsEnabled)
                                    {
                                        APIResponse<ApiBaseResponse> apiResponse = await this._proxyApi.RecordKioskPingFailure(new KioskPingFailure()
                                        {
                                            KioskId = this._storeService.KioskId,
                                            LastSuccessfulPingUTC = endIntervalUtc.Value,
                                            LastUpdateUTC = DateTime.UtcNow
                                        });
                                        result = apiResponse != null && apiResponse.StatusCode == HttpStatusCode.OK;
                                    }
                                    else
                                        this._logger.LogInfoWithSource("Skipping reporting of failed ping because config setting ReportFailedPingsEnabled is turned off", nameof(ReportFailedPing), "/sln/src/UpdateClientService.API/Services/Broker/PingStatisticsService.cs");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception reporting failed ping.", nameof(ReportFailedPing), "/sln/src/UpdateClientService.API/Services/Broker/PingStatisticsService.cs");
                result = false;
            }
            return result;
        }
    }
}
