using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<IConfigurationService> _logger;
        private readonly IStoreService _storeService;
        private IIoTCommandClient _iotCommandClient;
        private readonly IOptionsMonitor<AppSettings> _settings;
        private readonly IPersistentDataCacheService _persistentDataCacheService;
        private static DateTime _lastCallToGetKioskConfigurationSettingChanges = DateTime.MinValue;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _lockWait = 2000;
        private const string ConfigurationFileName = "configuration.json";
        private const string OldConfigurationFileName = "configuration old.json";
        private const string ConfigurationStatusFileName = "ConfigurationStatus.json";
        private int _processingChanges;

        public ConfigurationService(
          ILogger<IConfigurationService> logger,
          IStoreService storeService,
          IIoTCommandClient ioTCommandClient,
          IOptionsMonitor<AppSettings> settings,
          IPersistentDataCacheService persistentDataCacheService)
        {
            this._logger = logger;
            this._storeService = storeService;
            this._iotCommandClient = ioTCommandClient;
            this._settings = settings;
            this._persistentDataCacheService = persistentDataCacheService;
        }

        public async Task<bool> UpdateConfigurationStatusIfNeeded()
        {
            bool response = false;
            try
            {
                PersistentDataWrapper<ConfigurationStatus> dataCacheService = await this.GetConfigurationStatusFromPersistentDataCacheService();
                if (dataCacheService != null)
                {
                    if (!this.IsMoreThan26HoursAgo(dataCacheService.Modified))
                        goto label_9;
                }
                ConfigurationStatusResponse configurationStatusResponse = await this.UpdateConfigurationStatus();
                if (configurationStatusResponse != null)
                {
                    if (configurationStatusResponse.StatusCode == HttpStatusCode.OK)
                        response = true;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting config status", nameof(UpdateConfigurationStatusIfNeeded), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
        label_9:
            return response;
        }

        private bool IsMoreThan26HoursAgo(DateTime dateTime)
        {
            return (DateTime.Now - dateTime).TotalHours > 26.0;
        }

        public async Task<ConfigurationStatusResponse> UpdateConfigurationStatus()
        {
            ConfigurationStatusResponse response = new ConfigurationStatusResponse();
            try
            {
                IoTCommandResponse<ObjectResult> tcommandResponse = await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
                {
                    Version = 1,
                    RequestId = Guid.NewGuid().ToString(),
                    Command = CommandEnum.GetConfigStatus,
                    QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce,
                    LogResponse = new bool?(false)
                }, new PerformIoTCommandParameters()
                {
                    IoTTopic = "$aws/rules/kioskrestcall",
                    WaitForResponse = true
                });
                if (tcommandResponse != null && tcommandResponse.StatusCode == 200)
                {
                    ConfigurationStatusResponse configurationStatusResponse = JsonConvert.DeserializeObject<ConfigurationStatusResponse>(tcommandResponse?.Payload?.Value?.ToString());
                    if (configurationStatusResponse?.ConfigurationStatus != null && configurationStatusResponse.StatusCode == HttpStatusCode.OK)
                    {
                        if (await this.SetConfigurationStatus(configurationStatusResponse.ConfigurationStatus))
                        {
                            response.StatusCode = HttpStatusCode.OK;
                            response.ConfigurationStatus = configurationStatusResponse.ConfigurationStatus;
                        }
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.InternalServerError;
                        this._logger.LogErrorWithSource("Error updating configuration status.", nameof(UpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                    }
                    configurationStatusResponse = (ConfigurationStatusResponse)null;
                }
                else
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    this._logger.LogErrorWithSource("Error updating configuration status.", nameof(UpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(ex, "Exception updating configuration status.", nameof(UpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return response;
        }

        public async Task<ConfigurationStatusResponse> GetConfigurationStatus()
        {
            ConfigurationStatusResponse response = new ConfigurationStatusResponse()
            {
                ConfigurationStatus = new ConfigurationStatus()
            };
            try
            {
                PersistentDataWrapper<ConfigurationStatus> dataCacheService = await this.GetConfigurationStatusFromPersistentDataCacheService();
                if (dataCacheService?.Data != null)
                    response.ConfigurationStatus = dataCacheService.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(ex, "Exception retrieving configuration status.", nameof(GetConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return response;
        }

        private async Task<PersistentDataWrapper<ConfigurationStatus>> GetConfigurationStatusFromPersistentDataCacheService()
        {
            PersistentDataWrapper<ConfigurationStatus> result = new PersistentDataWrapper<ConfigurationStatus>();
            try
            {
                PersistentDataWrapper<ConfigurationStatus> persistentDataWrapper = await this._persistentDataCacheService.Read<ConfigurationStatus>("ConfigurationStatus.json", true, log: false);
                if (persistentDataWrapper != null)
                    result = persistentDataWrapper;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception getting configuration status", nameof(GetConfigurationStatusFromPersistentDataCacheService), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return result;
        }

        private async Task<bool> SetConfigurationStatus(ConfigurationStatus configurationStatus)
        {
            bool result = false;
            try
            {
                result = await this._persistentDataCacheService.Write<ConfigurationStatus>(configurationStatus, "ConfigurationStatus.json");
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception setting configuration status", nameof(SetConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return result;
        }

        public async Task<ApiBaseResponse> TriggerUpdateConfigurationStatus(
          TriggerUpdateConfigStatusRequest triggerUpdateConfigStatusRequest)
        {
            ApiBaseResponse apiResponse = new ApiBaseResponse();
            try
            {
                if (triggerUpdateConfigStatusRequest != null)
                {
                    if (triggerUpdateConfigStatusRequest.ExecutionTimeFrameMs.HasValue)
                    {
                        long? executionTimeFrameMs = triggerUpdateConfigStatusRequest.ExecutionTimeFrameMs;
                        long num1 = 0;
                        if (!(executionTimeFrameMs.GetValueOrDefault() == num1 & executionTimeFrameMs.HasValue))
                        {
                            await Task.Run((async () =>
                            {
                                int num2 = new Random().Next((int)triggerUpdateConfigStatusRequest.ExecutionTimeFrameMs.Value);
                                this._logger.LogInfoWithSource(string.Format("waiting {0} ms before calling UpdateConfigurationStatus", (object)num2), nameof(TriggerUpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                                await Task.Delay(num2);
                                ConfigurationStatusResponse configurationStatusResponse = await this.UpdateConfigurationStatus();
                            }));
                            goto label_9;
                        }
                    }
                    apiResponse = (ApiBaseResponse)await this.UpdateConfigurationStatus();
                }
                else
                {
                    this._logger.LogErrorWithSource("parameter triggerUpdateConfigStatusRequest must not be null.", nameof(TriggerUpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception triggering UpdateConfigurationStatus.", nameof(TriggerUpdateConfigurationStatus), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
        label_9:
            return apiResponse;
        }

        public async Task<ApiBaseResponse> TriggerGetKioskConfigurationSettingChanges(
          TriggerGetConfigChangesRequest triggerGetConfigChangesRequest)
        {
            ConfigurationService configurationService1 = this;
            ApiBaseResponse apiResponse = new ApiBaseResponse();
            try
            {
                if (triggerGetConfigChangesRequest != null)
                {
                    TriggerGetConfigChangesRequest configChangesRequest1 = triggerGetConfigChangesRequest;
                    long? nullable1;
                    int num1;
                    if (configChangesRequest1 == null)
                    {
                        num1 = 0;
                    }
                    else
                    {
                        nullable1 = configChangesRequest1.RequestedConfigurationVersionId;
                        long num2 = 0;
                        num1 = nullable1.GetValueOrDefault() == num2 & nullable1.HasValue ? 1 : 0;
                    }
                    if (num1 != 0)
                    {
                        TriggerGetConfigChangesRequest configChangesRequest2 = triggerGetConfigChangesRequest;
                        nullable1 = new long?();
                        long? nullable2 = nullable1;
                        configChangesRequest2.RequestedConfigurationVersionId = nullable2;
                    }
                    if (triggerGetConfigChangesRequest != null)
                    {
                        nullable1 = triggerGetConfigChangesRequest.ExecutionTimeFrameMs;
                        long num3 = 0;
                        if (!(nullable1.GetValueOrDefault() == num3 & nullable1.HasValue))
                        {
                            await Task.Run((async () =>
                            {
                                int randomMs = new Random().Next((int)triggerGetConfigChangesRequest.ExecutionTimeFrameMs.Value);
                                this._logger.LogInfoWithSource(string.Format("waiting {0} ms before calling GetKioskConfigurationSettingChanges", (object)randomMs), nameof(TriggerGetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                                DateTime lastCall = await this.GetLastCallToGetKioskConfigurationSettingChanges();
                                await Task.Delay(randomMs);
                                DateTime dateTime = lastCall;
                                if (dateTime != await this.GetLastCallToGetKioskConfigurationSettingChanges())
                                    return;
                                ApiBaseResponse configurationSettingChanges = await this.GetKioskConfigurationSettingChanges((long?)triggerGetConfigChangesRequest?.RequestedConfigurationVersionId);
                            }));
                            return apiResponse;
                        }
                    }
                    ConfigurationService configurationService2 = configurationService1;
                    TriggerGetConfigChangesRequest configChangesRequest3 = triggerGetConfigChangesRequest;
                    long? requestedConfigurationVersionId;
                    if (configChangesRequest3 == null)
                    {
                        nullable1 = new long?();
                        requestedConfigurationVersionId = nullable1;
                    }
                    else
                        requestedConfigurationVersionId = configChangesRequest3.RequestedConfigurationVersionId;
                    apiResponse = await (configurationService2.GetKioskConfigurationSettingChanges(requestedConfigurationVersionId));
                }
                else
                {
                    this._logger.LogErrorWithSource("parameter triggerGetConfigChangesRequest must not be null.", nameof(TriggerGetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                    apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception triggering GetKioskConfigurationChanges.", nameof(TriggerGetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
            return apiResponse;
        }

        public async Task<ApiBaseResponse> GetKioskConfigurationSettingChanges(
          long? requestedConfigurationVersionId)
        {
            await this.SetLastCallToGetKioskConfigurationSettingChanges();
            ApiBaseResponse response = new ApiBaseResponse();
            try
            {
                ConfigurationStatusResponse configurationStatus = await this.GetConfigurationStatus();
                if (configurationStatus == null || !configurationStatus.ConfigurationStatus.EnableUpdates)
                {
                    this._logger.LogInfoWithSource("aborting GetKioskConfigurationSettingChanges because ConfigurationStatus.EnableUpdates = false", nameof(GetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                    response.StatusCode = HttpStatusCode.ServiceUnavailable;
                    return response;
                }
                ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData = await this.CreateKioskConfigurationSettingChangesRequest(requestedConfigurationVersionId);
                while (!configurationSettingChangesData.AllPagesLoadedOrAborted)
                {
                    IoTCommandResponse<ObjectResult> tcommandResponse = await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
                    {
                        Version = 1,
                        RequestId = Guid.NewGuid().ToString(),
                        Command = CommandEnum.GetConfigChanges,
                        Payload = (object)configurationSettingChangesData.KioskConfigurationSettingChangesRequest,
                        QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce,
                        LogResponse = new bool?(false)
                    }, new PerformIoTCommandParameters()
                    {
                        IoTTopic = "$aws/rules/kioskrestcall",
                        WaitForResponse = true
                    });
                    int? nullable1;
                    if (tcommandResponse != null && tcommandResponse.StatusCode == 200)
                    {
                        KioskSettingChangesResponse kioskSettingChangesResponse = JsonConvert.DeserializeObject<KioskSettingChangesResponse>(tcommandResponse?.Payload?.Value?.ToString());
                        if (kioskSettingChangesResponse != null)
                        {
                            if (kioskSettingChangesResponse.IsFirstPage)
                            {
                                configurationSettingChangesData.KioskConfigurationChangesResponse = kioskSettingChangesResponse;
                                if (!kioskSettingChangesResponse.IsLastPage)
                                {
                                    ILogger<IConfigurationService> logger = this._logger;
                                    KioskSettingChangesResponse settingChangesResponse = kioskSettingChangesResponse;
                                    int? nullable2;
                                    if (settingChangesResponse == null)
                                    {
                                        nullable1 = new int?();
                                        nullable2 = nullable1;
                                    }
                                    else
                                        nullable2 = settingChangesResponse.PageCount;
                                    string str = string.Format("Response has {0} pages", (object)nullable2);
                                    this._logger.LogInfoWithSource(str, nameof(GetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                                }
                            }
                            else
                                await this.AddNewSettings(configurationSettingChangesData.KioskConfigurationChangesResponse?.NewConfigurationSettingValues, kioskSettingChangesResponse.NewConfigurationSettingValues);
                            if (kioskSettingChangesResponse.IsLastPage)
                            {
                                response.StatusCode = !await this.ProcessConfigurationSettingChanges(configurationSettingChangesData) ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
                                configurationSettingChangesData.AllPagesLoadedOrAborted = true;
                            }
                            else
                            {
                                KioskConfigurationSettingChangesRequest settingChangesRequest = configurationSettingChangesData.KioskConfigurationSettingChangesRequest;
                                nullable1 = kioskSettingChangesResponse.PageNumber;
                                int? nullable3 = nullable1.HasValue ? new int?(nullable1.GetValueOrDefault() + 1) : new int?();
                                settingChangesRequest.PageNumber = nullable3;
                            }
                        }
                        else
                        {
                            response.StatusCode = HttpStatusCode.InternalServerError;
                            this._logger.LogErrorWithSource("Unable to deserialize Iot command payload as GetKioskConfigurationSettingChangesResponse", nameof(GetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                            configurationSettingChangesData.AllPagesLoadedOrAborted = true;
                        }
                        kioskSettingChangesResponse = (KioskSettingChangesResponse)null;
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.InternalServerError;
                        ILogger<IConfigurationService> logger = this._logger;
                        CommandEnum local1 = CommandEnum.GetConfigChanges;
                        int? nullable4;
                        if (tcommandResponse == null)
                        {
                            nullable1 = new int?();
                            nullable4 = nullable1;
                        }
                        else
                            nullable4 = new int?(tcommandResponse.StatusCode);
                        string str = string.Format("Iot command {0} returned statuscode {1}", local1, nullable4);
                        this._logger.LogErrorWithSource(str, nameof(GetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                        configurationSettingChangesData.AllPagesLoadedOrAborted = true;
                    }
                }
                configurationSettingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(ex, "Error in GetKioskConfigurationSettingChanges", nameof(GetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return response;
        }

        private async Task<DateTime> GetLastCallToGetKioskConfigurationSettingChanges()
        {
            DateTime result = DateTime.MinValue;
            if (await ConfigurationService._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    result = ConfigurationService._lastCallToGetKioskConfigurationSettingChanges;
                }
                finally
                {
                    ConfigurationService._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed. Unable to get last call to GetKioskConfigurationSettingChanges", nameof(GetLastCallToGetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            return result;
        }

        private async Task SetLastCallToGetKioskConfigurationSettingChanges()
        {
            if (await ConfigurationService._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    ConfigurationService._lastCallToGetKioskConfigurationSettingChanges = DateTime.Now;
                }
                finally
                {
                    ConfigurationService._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed. Unable to set last call to GetKioskConfigurationSettingChanges", nameof(SetLastCallToGetKioskConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
        }

        private async Task ActivateConfigurationVersion(
          KioskConfigurationVersionActivationRequest kioskConfigurationVersionActivationRequest)
        {
            string str = (await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
            {
                Version = 1,
                RequestId = Guid.NewGuid().ToString(),
                Command = CommandEnum.ActivateConfigVersion,
                Payload = (object)kioskConfigurationVersionActivationRequest,
                QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce,
                LogResponse = new bool?(false)
            }, new PerformIoTCommandParameters()
            {
                IoTTopic = "$aws/rules/kioskrestcall",
                WaitForResponse = true
            })).StatusCode == 200 ? "successful" : "failed";
            this._logger.LogInfoWithSource(string.Format("Activation of configuration version {0} {1}", (object)kioskConfigurationVersionActivationRequest.ConfigurationVersionId, (object)str), nameof(ActivateConfigurationVersion), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
        }

        private async Task<ConfigurationService.ConfigurationSettingChangesData> CreateKioskConfigurationSettingChangesRequest(
          long? requestedConfigurationVersionId)
        {
            KioskConfigurationSettingChangesRequest request = new KioskConfigurationSettingChangesRequest()
            {
                KioskId = this._storeService.KioskId,
                RequestedConfigurationVersionId = requestedConfigurationVersionId,
                SettingChangesRequestId = Guid.NewGuid().ToString()
            };
            (bool, GetKioskConfigurationSettingValues) valueTuple = await this.ReadConfigurationFile(ConfigurationService.ConfigurationFilePath);
            bool flag = valueTuple.Item1;
            GetKioskConfigurationSettingValues getKioskConfigurationSettingValues = valueTuple.Item2 ?? new GetKioskConfigurationSettingValues();
            request.CurrentConfigurationVersionId = getKioskConfigurationSettingValues.ConfigurationVersion;
            request.ConfigurationVersionHash = this.ComputeConfigurationSettingValuesHashValue(getKioskConfigurationSettingValues);
            return new ConfigurationService.ConfigurationSettingChangesData()
            {
                KioskConfigurationSettingChangesRequest = request,
                KioskConfigurationSettingValues = getKioskConfigurationSettingValues
            };
        }

        private string ComputeConfigurationSettingValuesHashValue(
          GetKioskConfigurationSettingValues getKioskConfigurationSettingValues)
        {
            string settingValuesHashValue = (string)null;
            if (getKioskConfigurationSettingValues != null)
            {
                StringBuilder stringBuilder1 = new StringBuilder(string.Format("{0},{1}", (object)getKioskConfigurationSettingValues.KioskId, (object)getKioskConfigurationSettingValues.ConfigurationVersion));
                getKioskConfigurationSettingValues.Categories.Sort(new Comparison<KioskConfigurationCategory>(sortCategories));
                foreach (KioskConfigurationCategory category in getKioskConfigurationSettingValues.Categories)
                {
                    stringBuilder1.Append("," + category.Name);
                    category.Settings.Sort(new Comparison<KioskSetting>(sortSettings));
                    foreach (KioskSetting setting in category.Settings)
                    {
                        stringBuilder1.Append(string.Format(",{0},{1},{2}", (object)setting.SettingId, (object)setting.Name, (object)setting.DataType));
                        setting.SettingValues.Sort(new Comparison<KioskSettingValue>(sortSettingValues));
                        foreach (KioskSettingValue settingValue in setting.SettingValues)
                        {
                            string str1 = settingValue.EncryptionType.HasValue ? string.Format("{0},", (object)settingValue.EncryptionType) : (string)null;
                            StringBuilder stringBuilder2 = stringBuilder1;
                            object[] objArray = new object[6]
                            {
                (object) settingValue.ConfigurationSettingValueId,
                (object) str1,
                (object) settingValue.Value,
                (object) settingValue.Rank,
                (object) settingValue.EffectiveDateTime.ToString("u"),
                null
                            };
                            DateTime? expireDateTime = settingValue.ExpireDateTime;
                            ref DateTime? local = ref expireDateTime;
                            objArray[5] = (object)(local.HasValue ? local.GetValueOrDefault().ToString("u") : (string)null);
                            string str2 = string.Format(",{0},{1}{2},{3},{4},{5}", objArray);
                            stringBuilder2.Append(str2);
                        }
                    }
                }
                settingValuesHashValue = Convert.ToBase64String(((HashAlgorithm)new SHA512Managed()).ComputeHash(Encoding.UTF8.GetBytes(stringBuilder1.ToString())));
            }
            return settingValuesHashValue;

            int sortCategories(KioskConfigurationCategory a, KioskConfigurationCategory b)
            {
                return a.Name.CompareTo(b.Name);
            }

            int sortSettings(KioskSetting a, KioskSetting b) => a.Name.CompareTo(b.Name);

            int sortSettingValues(KioskSettingValue a, KioskSettingValue b)
            {
                return a.ConfigurationSettingValueId.CompareTo(b.ConfigurationSettingValueId);
            }
        }

        private async Task<bool> ProcessConfigurationSettingChanges(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (Interlocked.CompareExchange(ref this._processingChanges, 1, 0) == 1)
            {
                this._logger.LogInfoWithSource(string.Format("Prevented attempt to process configuration setting changes for version {0}.", (object)configurationSettingChangesData?.NewVersionId), nameof(ProcessConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                return false;
            }
            try
            {
                if (!configurationSettingChangesData.IsVersionCurrent)
                {
                    await this.ValidateResponse(configurationSettingChangesData);
                    await this.InitializePath(configurationSettingChangesData);
                    await this.ClearOldSettings(configurationSettingChangesData);
                    await this.AddNewSettings(configurationSettingChangesData);
                    await this.SaveTempConfigurationFile(configurationSettingChangesData);
                    await this.RenameCurrentConfigFile(configurationSettingChangesData);
                    await this.RenameTempFileAsCurrent(configurationSettingChangesData);
                    await this.ActivateConfigVersion(configurationSettingChangesData);
                    await this.CleanupFailedAttempt(configurationSettingChangesData);
                }
            }
            finally
            {
                this._processingChanges = 0;
            }
            if (configurationSettingChangesData.IsVersionCurrent)
            {
                this._logger.LogInfoWithSource(string.Format("No configuration changes to process.  Version {0} is current.", (object)configurationSettingChangesData.CurrentVersionId), nameof(ProcessConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            else
            {
                string str = configurationSettingChangesData.Success ? "Successful" : "Failed";
                this._logger.LogInfoWithSource(string.Format("Processing of configuration changes from verion {0} to version {1} {2}", (object)configurationSettingChangesData.CurrentVersionId, (object)configurationSettingChangesData.NewVersionId, (object)str), nameof(ProcessConfigurationSettingChanges), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }
            return configurationSettingChangesData.Success;
        }

        private async Task ActivateConfigVersion(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            KioskSettingChangesResponse configurationChangesResponse = configurationSettingChangesData.KioskConfigurationChangesResponse;
            if ((configurationChangesResponse != null ? (configurationChangesResponse.VersionIsCurrent ? 1 : 0) : 0) != 0)
                return;
            await Task.Run((Action)(() =>
            {
                KioskConfigurationVersionActivationRequest kioskConfigurationVersionActivationRequest = new KioskConfigurationVersionActivationRequest()
                {
                    KioskId = this._storeService.KioskId,
                    ModifiedBy = "System",
                    ActivationDateTimeUtc = DateTime.UtcNow,
                    ConfigurationVersionId = configurationSettingChangesData.NewVersionId
                };
                Task.Run((Func<Task>)(async () => await this.ActivateConfigurationVersion(kioskConfigurationVersionActivationRequest)));
            }));
        }

        private async Task ValidateResponse(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            await Task.Run((Action)(() =>
            {
                long? configurationVersionId = configurationSettingChangesData.KioskConfigurationChangesResponse.OriginalConfigurationVersionId;
                long currentVersionId = configurationSettingChangesData.CurrentVersionId;
                if (configurationVersionId.GetValueOrDefault() == currentVersionId & configurationVersionId.HasValue)
                    return;
                configurationSettingChangesData.Success = false;
                this._logger.LogErrorWithSource(string.Format("Error in processing configuration changes.  Current configuration version id {0} doesn't match change results original configuration version id {1}", (object)configurationSettingChangesData?.CurrentVersionId, (object)(long?)configurationSettingChangesData?.KioskConfigurationChangesResponse?.OriginalConfigurationVersionId), nameof(ValidateResponse), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }));
        }

        private async Task RenameTempFileAsCurrent(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            ConfigurationService.ConfigurationSettingChangesData settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.RenameFile(configurationSettingChangesData.TempConfigFileName, ConfigurationService.ConfigurationFilePath);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
        }

        private async Task RenameCurrentConfigFile(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            ConfigurationService.ConfigurationSettingChangesData settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.DeleteFile(this.OldConfigurationFilePath);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
            if (!configurationSettingChangesData.Success || !File.Exists(ConfigurationService.ConfigurationFilePath))
                return;
            settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.RenameFile(ConfigurationService.ConfigurationFilePath, this.OldConfigurationFilePath);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
        }

        private async Task<bool> RenameFile(string originalFileName, string newFileName)
        {
            bool result = false;
            await Task.Run((Action)(() =>
            {
                try
                {
                    File.Move(originalFileName, newFileName);
                    if (!File.Exists(newFileName))
                        return;
                    result = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Unable to rename file " + originalFileName + " to " + newFileName, nameof(RenameFile), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                }
            }));
            return result;
        }

        private async Task SaveTempConfigurationFile(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            ConfigurationService.ConfigurationSettingChangesData settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.SaveConfigurationFile(configurationSettingChangesData.TempConfigFileName, configurationSettingChangesData.KioskConfigurationSettingValues);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
        }

        private async Task<bool> SaveConfigurationFile(
          string configurationFileName,
          GetKioskConfigurationSettingValues kioskConfigurationSettingValues)
        {
            bool result = false;
            string json = (string)null;
            await Task.Run((Action)(() =>
            {
                try
                {
                    json = JsonConvert.SerializeObject((object)new KioskConfigurationSettingsFile()
                    {
                        KioskConfigurationSettings = kioskConfigurationSettingValues
                    }, (Formatting)1);
                    File.WriteAllText(configurationFileName, json);
                    result = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, string.Format("Error saving configiration file {0}.  json length: {1}", (object)configurationFileName, (object)json.Length), nameof(SaveConfigurationFile), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                }
            }));
            return result;
        }

        private async Task AddNewSettings(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            GetKioskConfigurationSettingValues configurationSettingValues1 = configurationSettingChangesData?.KioskConfigurationChangesResponse?.NewConfigurationSettingValues;
            GetKioskConfigurationSettingValues configurationSettingValues2 = configurationSettingChangesData.KioskConfigurationSettingValues;
            configurationSettingValues2.ConfigurationVersion = configurationSettingValues1.ConfigurationVersion;
            configurationSettingValues2.ConfigurationVersionHash = configurationSettingChangesData.KioskConfigurationChangesResponse?.ConfigurationVersionHash;
            configurationSettingValues2.KioskId = configurationSettingValues1.KioskId;
            this._logger.LogInfoWithSource(string.Format("Updating {0} setting Values", (object)configurationSettingValues1.Categories.Sum<KioskConfigurationCategory>((Func<KioskConfigurationCategory, int>)(c => c.Settings.Sum<KioskSetting>((Func<KioskSetting, int>)(s => s.SettingValues.Count))))), nameof(AddNewSettings), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            await this.AddNewSettings(configurationSettingValues2, configurationSettingValues1);
        }

        private async Task AddNewSettings(
          GetKioskConfigurationSettingValues settingValuesDestination,
          GetKioskConfigurationSettingValues settingValuesToAdd)
        {
            await Task.Run((Action)(() =>
            {
                if (settingValuesToAdd == null || settingValuesDestination == null)
                    return;
                foreach (KioskConfigurationCategory category in settingValuesToAdd.Categories)
                {
                    KioskConfigurationCategory orAddCategory = this.GetOrAddCategory(category, settingValuesDestination);
                    foreach (KioskSetting setting in category.Settings)
                    {
                        KioskSetting orAddSetting = this.GetOrAddSetting(setting, orAddCategory);
                        foreach (KioskSettingValue settingValue in setting.SettingValues)
                            this.AddSettingValue(settingValue, orAddSetting);
                        orAddSetting.SettingValues.Sort(new Comparison<KioskSettingValue>(sortSettingValues));
                    }
                    orAddCategory.Settings.Sort(new Comparison<KioskSetting>(sortSettings));
                }
                settingValuesDestination.Categories.Sort(new Comparison<KioskConfigurationCategory>(sortCategories));
            }));

            int sortCategories(KioskConfigurationCategory a, KioskConfigurationCategory b)
            {
                return a.Name.CompareTo(b.Name);
            }

            int sortSettings(KioskSetting a, KioskSetting b) => a.Name.CompareTo(b.Name);

            int sortSettingValues(KioskSettingValue a, KioskSettingValue b)
            {
                return a.ConfigurationSettingValueId.CompareTo(b.ConfigurationSettingValueId);
            }
        }

        private void AddSettingValue(KioskSettingValue kioskSettingValue, KioskSetting kioskSetting)
        {
            if (kioskSetting.SettingValues.FirstOrDefault<KioskSettingValue>((Func<KioskSettingValue, bool>)(x => x.ConfigurationSettingValueId == kioskSettingValue.ConfigurationSettingValueId)) != null)
                return;
            KioskSettingValue kioskSettingValue1 = new KioskSettingValue()
            {
                ConfigurationSettingValueId = kioskSettingValue.ConfigurationSettingValueId,
                EffectiveDateTime = kioskSettingValue.EffectiveDateTime,
                ExpireDateTime = kioskSettingValue.ExpireDateTime,
                Rank = kioskSettingValue.Rank,
                SegmentationName = kioskSettingValue.SegmentationName,
                SegmentId = kioskSettingValue.SegmentId,
                SegmentName = kioskSettingValue.SegmentName,
                EncryptionType = kioskSettingValue.EncryptionType,
                Value = kioskSettingValue.Value
            };
            kioskSetting.SettingValues.Add(kioskSettingValue1);
        }

        private KioskSetting GetOrAddSetting(
          KioskSetting kioskSetting,
          KioskConfigurationCategory kioskConfigurationCategory)
        {
            KioskSetting orAddSetting = kioskConfigurationCategory.Settings.FirstOrDefault<KioskSetting>((Func<KioskSetting, bool>)(x => x.SettingId == kioskSetting.SettingId));
            if (orAddSetting == null)
            {
                orAddSetting = new KioskSetting()
                {
                    Name = kioskSetting.Name,
                    SettingId = kioskSetting.SettingId,
                    DataType = kioskSetting.DataType
                };
                kioskConfigurationCategory.Settings.Add(orAddSetting);
            }
            return orAddSetting;
        }

        private KioskConfigurationCategory GetOrAddCategory(
          KioskConfigurationCategory kioskConfigurationCategory,
          GetKioskConfigurationSettingValues kioskConfigurationSettingValues)
        {
            KioskConfigurationCategory orAddCategory = kioskConfigurationSettingValues.Categories.FirstOrDefault<KioskConfigurationCategory>((Func<KioskConfigurationCategory, bool>)(x => x.Name == kioskConfigurationCategory?.Name));
            if (orAddCategory == null)
            {
                orAddCategory = new KioskConfigurationCategory()
                {
                    Name = kioskConfigurationCategory.Name
                };
                kioskConfigurationSettingValues.Categories.Add(orAddCategory);
            }
            return orAddCategory;
        }

        private async Task ClearOldSettings(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            await Task.Run((Action)(() =>
            {
                ConfigurationService.ConfigurationSettingChangesData settingChangesData1 = configurationSettingChangesData;
                int num1;
                if (settingChangesData1 == null)
                {
                    num1 = 0;
                }
                else
                {
                    bool? configurationSettingValues = (bool?)settingChangesData1.KioskConfigurationChangesResponse?.RemoveAllExistingConfigurationSettingValues;
                    bool flag = true;
                    num1 = configurationSettingValues.GetValueOrDefault() == flag & configurationSettingValues.HasValue ? 1 : 0;
                }
                if (num1 != 0)
                {
                    configurationSettingChangesData.KioskConfigurationSettingValues.Categories.Clear();
                }
                else
                {
                    ConfigurationService.ConfigurationSettingChangesData settingChangesData2 = configurationSettingChangesData;
                    int num2;
                    if (settingChangesData2 == null)
                    {
                        num2 = 0;
                    }
                    else
                    {
                        KioskSettingChangesResponse configurationChangesResponse = settingChangesData2.KioskConfigurationChangesResponse;
                        int? nullable1;
                        if (configurationChangesResponse == null)
                        {
                            nullable1 = new int?();
                        }
                        else
                        {
                            IEnumerable<long> configurationSettingValueIds = configurationChangesResponse.RemovedConfigurationSettingValueIds;
                            nullable1 = configurationSettingValueIds != null ? new int?(configurationSettingValueIds.Count<long>()) : new int?();
                        }
                        int? nullable2 = nullable1;
                        int num3 = 0;
                        num2 = nullable2.GetValueOrDefault() > num3 & nullable2.HasValue ? 1 : 0;
                    }
                    if (num2 == 0)
                        return;
                    foreach (KioskConfigurationCategory category in configurationSettingChangesData.KioskConfigurationSettingValues.Categories)
                    {
                        KioskConfigurationCategory eachCategory = category;
                        foreach (KioskSetting setting in eachCategory.Settings)
                        {
                            KioskSetting eachSetting = setting;
                            eachSetting.SettingValues.Where<KioskSettingValue>((Func<KioskSettingValue, bool>)(x => configurationSettingChangesData.KioskConfigurationChangesResponse.RemovedConfigurationSettingValueIds.Contains<long>(x.ConfigurationSettingValueId))).ToList<KioskSettingValue>().ForEach((Action<KioskSettingValue>)(x => eachSetting.SettingValues.Remove(x)));
                        }
                        eachCategory.Settings.Where<KioskSetting>((Func<KioskSetting, bool>)(x => !x.SettingValues.Any<KioskSettingValue>())).ToList<KioskSetting>().ForEach((Action<KioskSetting>)(x => eachCategory.Settings.Remove(x)));
                    }
                    configurationSettingChangesData.KioskConfigurationSettingValues.Categories.Where<KioskConfigurationCategory>((Func<KioskConfigurationCategory, bool>)(x => !x.Settings.Any<KioskSetting>())).ToList<KioskConfigurationCategory>().ForEach((Action<KioskConfigurationCategory>)(x => configurationSettingChangesData.KioskConfigurationSettingValues.Categories.Remove(x)));
                }
            }));
        }

        private async Task<(bool, GetKioskConfigurationSettingValues)> ReadConfigurationFile(
          string configurationFileName)
        {
            bool result = false;
            GetKioskConfigurationSettingValues getKioskConfigurationSettingValues = (GetKioskConfigurationSettingValues)null;
            await Task.Run((Action)(() =>
            {
                try
                {
                    if (File.Exists(configurationFileName))
                    {
                        getKioskConfigurationSettingValues = JsonConvert.DeserializeObject<KioskConfigurationSettingsFile>(File.ReadAllText(configurationFileName))?.KioskConfigurationSettings;
                        result = true;
                    }
                    else
                        result = false;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception reading configuration file " + configurationFileName, nameof(ReadConfigurationFile), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                }
            }));
            return (result, getKioskConfigurationSettingValues);
        }

        private async Task CleanupFailedAttempt(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (configurationSettingChangesData.Success)
                return;
            if (!File.Exists(ConfigurationService.ConfigurationFilePath) && File.Exists(this.OldConfigurationFilePath))
            {
                int num1 = await this.RenameFile(this.OldConfigurationFilePath, ConfigurationService.ConfigurationFilePath) ? 1 : 0;
            }
            int num2 = await this.DeleteTempConfigurationFile(configurationSettingChangesData) ? 1 : 0;
        }

        private static string ConfigurationFileDirectory
        {
            get => FilePaths.TypeMappings[FileTypeEnum.Configuration];
        }

        public static string ConfigurationFilePath
        {
            get => Path.Combine(ConfigurationService.ConfigurationFileDirectory, "configuration.json");
        }

        private string OldConfigurationFilePath
        {
            get
            {
                return Path.Combine(ConfigurationService.ConfigurationFileDirectory, "configuration old.json");
            }
        }

        private async Task InitializePath(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            if (!configurationSettingChangesData.Success)
                return;
            ConfigurationService.ConfigurationSettingChangesData settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.CreateConfigFilePathIfNeeded(ConfigurationService.ConfigurationFileDirectory);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
            if (!configurationSettingChangesData.Success)
                return;
            configurationSettingChangesData.TempConfigFileName = Path.Combine(ConfigurationService.ConfigurationFileDirectory, string.Format("configuration v{0}.json", (object)configurationSettingChangesData?.KioskConfigurationChangesResponse?.NewConfigurationSettingValues?.ConfigurationVersion));
            settingChangesData = configurationSettingChangesData;
            settingChangesData.Success = await this.DeleteTempConfigurationFile(configurationSettingChangesData);
            settingChangesData = (ConfigurationService.ConfigurationSettingChangesData)null;
        }

        private async Task<bool> CreateConfigFilePathIfNeeded(string configFilePath)
        {
            bool result = true;
            await Task.Run((Action)(() =>
            {
                if (Directory.Exists(configFilePath) || Directory.CreateDirectory(configFilePath).Exists)
                    return;
                result = false;
                this._logger.LogErrorWithSource("Unable to create configuration path " + configFilePath, nameof(CreateConfigFilePathIfNeeded), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
            }));
            return result;
        }

        private async Task<bool> DeleteTempConfigurationFile(
          ConfigurationService.ConfigurationSettingChangesData configurationSettingChangesData)
        {
            return await this.DeleteFile(configurationSettingChangesData.TempConfigFileName);
        }

        private async Task<bool> DeleteFile(string fileName)
        {
            bool result = false;
            await Task.Run((Action)(() =>
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                        result = !File.Exists(fileName);
                    }
                    else
                        result = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Unable to delete file " + fileName, nameof(DeleteFile), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationService.cs");
                }
            }));
            return result;
        }

        private class ConfigurationSettingChangesData
        {
            public KioskSettingChangesResponse KioskConfigurationChangesResponse { get; set; }

            public GetKioskConfigurationSettingValues KioskConfigurationSettingValues { get; set; }

            public KioskConfigurationSettingChangesRequest KioskConfigurationSettingChangesRequest { get; set; }

            public string TempConfigFileName { get; set; }

            public bool Success { get; set; } = true;

            public bool AllPagesLoadedOrAborted { get; set; }

            public bool IsVersionCurrent
            {
                get
                {
                    KioskSettingChangesResponse configurationChangesResponse = this.KioskConfigurationChangesResponse;
                    return configurationChangesResponse != null && configurationChangesResponse.VersionIsCurrent;
                }
            }

            public long CurrentVersionId
            {
                get
                {
                    KioskConfigurationSettingChangesRequest settingChangesRequest = this.KioskConfigurationSettingChangesRequest;
                    return settingChangesRequest == null ? 0L : settingChangesRequest.CurrentConfigurationVersionId;
                }
            }

            public long NewVersionId
            {
                get
                {
                    return this.KioskConfigurationChangesResponse?.NewConfigurationSettingValues?.ConfigurationVersion ?? 0;
                }
            }
        }
    }
}
