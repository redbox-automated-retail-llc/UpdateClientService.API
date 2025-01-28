using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT
{
    public class IoTStatisticsService : IIoTStatisticsService
    {
        private ILogger<IoTStatisticsService> _logger;
        private readonly IPersistentDataCacheService _persistentDataCacheService;
        private IoTStatistics _iotStatistics;
        private const string IoTStatisticsFileName = "IoTStatistics.json";

        public IoTStatisticsService(
          ILogger<IoTStatisticsService> logger,
          IPersistentDataCacheService persistentDataCacheService)
        {
            this._logger = logger;
            this._persistentDataCacheService = persistentDataCacheService;
        }

        public async Task<IoTStatisticsSummaryResponse> GetIoTStatisticsSummaryResponse()
        {
            IoTStatisticsSummaryResponse response = new IoTStatisticsSummaryResponse();
            try
            {
                IoTStatistics ioTstatistics = await this.GetIoTStatistics();
                bool? nullable;
                if (ioTstatistics == null)
                {
                    nullable = new bool?();
                }
                else
                {
                    List<IoTConnectionAttempt> connectionAttempts = ioTstatistics.ConnectionAttempts;
                    nullable = connectionAttempts != null ? new bool?(connectionAttempts.Any<IoTConnectionAttempt>()) : new bool?();
                }
                if (nullable.GetValueOrDefault())
                {
                    foreach (IoTConnectionAttempt connectionAttempt in ioTstatistics.ConnectionAttempts)
                    {
                        if (connectionAttempt.Connected.HasValue && !connectionAttempt.Disconnected.HasValue)
                            connectionAttempt.Disconnected = new DateTime?(DateTime.Now);
                    }
                    foreach (IGrouping<DateTime, IoTConnectionAttempt> source in ioTstatistics.ConnectionAttempts.GroupBy<IoTConnectionAttempt, DateTime>((Func<IoTConnectionAttempt, DateTime>)(x => x.StartConnectionAttempt.Value.Date)))
                    {
                        IoTStatisticsSummary tstatisticsSummary = new IoTStatisticsSummary()
                        {
                            Date = source.Key.ToShortDateString()
                        };
                        Dictionary<string, int> dictionary = new Dictionary<string, int>();
                        tstatisticsSummary.ConnectionCount = source.Count<IoTConnectionAttempt>((Func<IoTConnectionAttempt, bool>)(x => x.Connected.HasValue));
                        tstatisticsSummary.ConnectionAttemptsCount = source.Sum<IoTConnectionAttempt>((Func<IoTConnectionAttempt, int>)(x => x.ConnectionAttempts));
                        foreach (IoTConnectionAttempt tconnectionAttempt in (IEnumerable<IoTConnectionAttempt>)source)
                        {
                            if (tconnectionAttempt.ConnectionAttemptsDuration.HasValue)
                                tstatisticsSummary.TotalConnectionAttemptsDuration += tconnectionAttempt.ConnectionAttemptsDuration.Value;
                            Dictionary<string, int> connectionExceptions = tconnectionAttempt.ConnectionExceptions;
                            if ((connectionExceptions != null ? (connectionExceptions.Any<KeyValuePair<string, int>>() ? 1 : 0) : 0) != 0)
                            {
                                foreach (KeyValuePair<string, int> connectionException in tconnectionAttempt.ConnectionExceptions)
                                {
                                    int num1 = 0;
                                    dictionary.TryGetValue(connectionException.Key, out num1);
                                    int num2 = num1 + connectionException.Value;
                                    dictionary[connectionException.Key] = num2;
                                }
                            }
                        }
                        if (dictionary.Count > 0)
                        {
                            tstatisticsSummary.ConnectionExceptions = new List<ConnectionException>();
                            foreach (KeyValuePair<string, int> keyValuePair in dictionary)
                                tstatisticsSummary.ConnectionExceptions.Add(new ConnectionException()
                                {
                                    ExceptionMessage = keyValuePair.Key,
                                    Count = keyValuePair.Value
                                });
                        }
                        response.ioTStatisticsSummaries.Add(tstatisticsSummary);
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(ex, "Exception getting IotStatisticsSummaryResponse", nameof(GetIoTStatisticsSummaryResponse), "/sln/src/UpdateClientService.API/Services/IoT/IoTStatisticsService.cs");
            }
            return response;
        }

        public async Task<bool> RecordConnectionAttempt()
        {
            bool flag;
            try
            {
                IoTStatistics ioTstatistics = await this.GetIoTStatistics();
                ioTstatistics.Cleanup();
                ioTstatistics.StartConnectionAttempt();
                flag = await this.SaveIoTStatistics(ioTstatistics);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(string.Format("Failed to record connection attempt: {0}", (object)ex), nameof(RecordConnectionAttempt), "/sln/src/UpdateClientService.API/Services/IoT/IoTStatisticsService.cs");
                flag = false;
            }
            return flag;
        }

        public async Task<bool> RecordConnectionSuccess()
        {
            bool flag;
            try
            {
                IoTStatistics ioTstatistics = await this.GetIoTStatistics();
                ioTstatistics.Connected();
                flag = await this.SaveIoTStatistics(ioTstatistics);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(string.Format("Failed to record connection success: {0}", (object)ex), nameof(RecordConnectionSuccess), "/sln/src/UpdateClientService.API/Services/IoT/IoTStatisticsService.cs");
                flag = false;
            }
            return flag;
        }

        public async Task<bool> RecordDisconnection()
        {
            bool flag;
            try
            {
                IoTStatistics ioTstatistics = await this.GetIoTStatistics();
                ioTstatistics.Disconnected();
                flag = await this.SaveIoTStatistics(ioTstatistics);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(string.Format("Failed to record connection disconnect: {0}", (object)ex), nameof(RecordDisconnection), "/sln/src/UpdateClientService.API/Services/IoT/IoTStatisticsService.cs");
                flag = false;
            }
            return flag;
        }

        public async Task<bool> RecordConnectionException(string exceptionMessage)
        {
            bool flag;
            try
            {
                IoTStatistics ioTstatistics = await this.GetIoTStatistics();
                ioTstatistics.AddException(exceptionMessage);
                flag = await this.SaveIoTStatistics(ioTstatistics);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(string.Format("Failed to record connection disconnect: {0}", (object)ex), nameof(RecordConnectionException), "/sln/src/UpdateClientService.API/Services/IoT/IoTStatisticsService.cs");
                flag = false;
            }
            return flag;
        }

        private async Task<IoTStatistics> GetIoTStatistics()
        {
            if (this._iotStatistics == null)
            {
                PersistentDataWrapper<IoTStatistics> persistentDataWrapper = await this._persistentDataCacheService.Read<IoTStatistics>("IoTStatistics.json", log: false);
                this._iotStatistics = persistentDataWrapper?.Data == null ? new IoTStatistics() : persistentDataWrapper.Data;
            }
            return this._iotStatistics;
        }

        private async Task<bool> SaveIoTStatistics(IoTStatistics ioTStatistics)
        {
            return await this._persistentDataCacheService.Write<IoTStatistics>(ioTStatistics, "IoTStatistics.json");
        }
    }
}
