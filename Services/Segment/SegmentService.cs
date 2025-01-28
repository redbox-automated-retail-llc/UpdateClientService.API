using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.Segment
{
    public class SegmentService : ISegmentService
    {
        private readonly ILogger<SegmentService> _logger;
        private readonly IPersistentDataCacheService _persistentDataCacheService;
        private readonly IIoTCommandClient _iotCommandClient;
        private readonly IKioskConfiguration _kioskConfiguration;
        private const string SegmentsFileName = "segments.json";
        private int _processingSegments;

        public SegmentService(
          ILogger<SegmentService> logger,
          IIoTCommandClient ioTCommandClient,
          IPersistentDataCacheService persistentDataCacheService,
          IOptionsSnapshotKioskConfiguration kioskConfiguration)
        {
            this._logger = logger;
            this._iotCommandClient = ioTCommandClient;
            this._persistentDataCacheService = persistentDataCacheService;
            this._kioskConfiguration = (IKioskConfiguration)kioskConfiguration;
        }

        public async Task<KioskSegmentsResponse> GetKioskSegments()
        {
            KioskSegmentsResponse response = new KioskSegmentsResponse();
            try
            {
                PersistentDataWrapper<SegmentService.KioskSegmentsData> persistentDataCache = await this.GetKioskSegmentsFromPersistentDataCache();
                if (persistentDataCache?.Data != null)
                    response.Segments.AddRange((IEnumerable<KioskSegmentModel>)persistentDataCache.Data);
                else
                    response.StatusCode = HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(ex, "Exception while getting kiosk segments", nameof(GetKioskSegments), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
            }
            return response;
        }

        private async Task<PersistentDataWrapper<SegmentService.KioskSegmentsData>> GetKioskSegmentsFromPersistentDataCache()
        {
            PersistentDataWrapper<SegmentService.KioskSegmentsData> result = new PersistentDataWrapper<SegmentService.KioskSegmentsData>();
            try
            {
                PersistentDataWrapper<SegmentService.KioskSegmentsData> persistentDataWrapper = await this._persistentDataCacheService.Read<SegmentService.KioskSegmentsData>("segments.json", true, log: false);
                if (persistentDataWrapper != null)
                    result = persistentDataWrapper;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception getting kiosk segments", nameof(GetKioskSegmentsFromPersistentDataCache), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
            }
            return result;
        }

        public async Task<ApiBaseResponse> UpdateKioskSegmentsFromUpdateService()
        {
            ApiBaseResponse response = new ApiBaseResponse();
            try
            {
                if (!this._kioskConfiguration.Operations.SegmentUpdateEnabled)
                {
                    response.StatusCode = HttpStatusCode.ServiceUnavailable;
                    return response;
                }
                IoTCommandResponse<ObjectResult> tcommandResponse = await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
                {
                    Version = 1,
                    RequestId = Guid.NewGuid().ToString(),
                    Command = CommandEnum.GetKioskSegments,
                    QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce,
                    LogResponse = new bool?(false)
                }, new PerformIoTCommandParameters()
                {
                    IoTTopic = "$aws/rules/kioskrestcall",
                    WaitForResponse = true
                });
                if (tcommandResponse != null && tcommandResponse.StatusCode == 200)
                {
                    KioskSegmentsResponse segmentsResponse = JsonConvert.DeserializeObject<KioskSegmentsResponse>(tcommandResponse?.Payload?.Value?.ToString());
                    if (segmentsResponse != null)
                    {
                        SegmentService.KioskSegmentsData kioskSegmentsData = new SegmentService.KioskSegmentsData();
                        kioskSegmentsData.AddRange((IEnumerable<KioskSegmentModel>)segmentsResponse.Segments);
                        response.StatusCode = !await this.UpdateKioskSegmentsFile(kioskSegmentsData) ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.InternalServerError;
                        this._logger.LogErrorWithSource("Unable to deserialize Iot command payload as KioskSegmentsResponse", nameof(UpdateKioskSegmentsFromUpdateService), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
                    }
                }
                else
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    this._logger.LogErrorWithSource(string.Format("Iot command {0} returned statuscode {1}", (object)CommandEnum.GetKioskSegments, (object)tcommandResponse?.StatusCode), nameof(UpdateKioskSegmentsFromUpdateService), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting kiosk segments.", nameof(UpdateKioskSegmentsFromUpdateService), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        private async Task<bool> UpdateKioskSegmentsFile(
          SegmentService.KioskSegmentsData kioskSegmentsData)
        {
            bool flag = false;
            if (Interlocked.CompareExchange(ref this._processingSegments, 1, 0) == 1)
            {
                this._logger.LogInfoWithSource("Prevented attempt to process kiosk segments.", nameof(UpdateKioskSegmentsFile), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
                return flag;
            }
            try
            {
                flag = await this._persistentDataCacheService.Write<SegmentService.KioskSegmentsData>(kioskSegmentsData, "segments.json");
            }
            finally
            {
                this._processingSegments = 0;
            }
            return flag;
        }

        public async Task<bool> UpdateKioskSegmentsIfNeeded()
        {
            bool response = false;
            try
            {
                PersistentDataWrapper<SegmentService.KioskSegmentsData> persistentDataCache = await this.GetKioskSegmentsFromPersistentDataCache();
                if (persistentDataCache != null)
                {
                    if (!this.IsKioskSegmentDataExpired(persistentDataCache))
                        goto label_9;
                }
                ApiBaseResponse apiBaseResponse = await this.UpdateKioskSegmentsFromUpdateService();
                if (apiBaseResponse != null)
                {
                    if (apiBaseResponse.StatusCode == HttpStatusCode.OK)
                        response = true;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception updating kiosk segments", nameof(UpdateKioskSegmentsIfNeeded), "/sln/src/UpdateClientService.API/Services/Segment/SegmentService.cs");
            }
        label_9:
            return response;
        }

        private bool IsKioskSegmentDataExpired(
          PersistentDataWrapper<SegmentService.KioskSegmentsData> persistentDataWrapper)
        {
            return (DateTime.Now - persistentDataWrapper.Modified).TotalHours > (double)this._kioskConfiguration.Operations.SegmentUpdateFrequencyHours;
        }

        private class KioskSegmentsData : List<KioskSegmentModel>, IPersistentData
        {
        }
    }
}
