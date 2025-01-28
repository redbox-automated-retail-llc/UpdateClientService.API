using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.KioskEngine
{
    public class KioskEngineService : IKioskEngineService
    {
        private readonly ILogger<KioskEngineService> _logger;
        private readonly IHttpService _http;
        private const string KioskEngineUrl = "http://localhost:9002";

        private int? KioskEngineProcessId { get; set; }

        public KioskEngineService(ILogger<KioskEngineService> logger, IHttpService http)
        {
            this._logger = logger;
            this._http = http;
        }

        public async Task<KioskEngineStatus> GetStatus()
        {
            if (!this.KioskEngineProcessId.HasValue)
                this.KioskEngineProcessId = await this.GetProcessId();
            return this.KioskEngineProcessId.HasValue ? (!KioskEngineService.TryGetProcessById(this.KioskEngineProcessId, out Process _) ? KioskEngineStatus.Stopped : KioskEngineStatus.Running) : KioskEngineStatus.Unknown;
        }

        public async Task<PerformShutdownResponse> PerformShutdown(int timeoutMs, int attempts)
        {
            PerformShutdownResponse response = new PerformShutdownResponse();
            try
            {
                int attempt = 0;
                while (true)
                {
                    bool flag = attempt++ < attempts;
                    if (flag)
                        flag = await this.GetStatus() != KioskEngineStatus.Stopped;
                    if (flag)
                    {
                        int num = await this.PerformShutdown(timeoutMs) ? 1 : 0;
                    }
                    else
                        break;
                }
                if (await this.GetStatus() != KioskEngineStatus.Stopped)
                    KioskEngineService.HandleTimeout(timeoutMs, attempts, response);
            }
            catch (Exception ex)
            {
                KioskEngineService.HandleException(response, ex);
            }
            response.ProcessId = this.KioskEngineProcessId;
            this._logger.LogInfoWithSource("Result -> " + response.ToJson(), nameof(PerformShutdown), "/sln/src/UpdateClientService.API/Services/KioskEngine/KioskEngineService.cs");
            this.KioskEngineProcessId = new int?();
            return response;
        }

        private async Task<bool> PerformShutdown(int timeoutMs)
        {
            KioskEngineService kioskEngineService = this;
            bool flag;
            using (CancellationTokenSource cts = new CancellationTokenSource(timeoutMs))
                flag = await Task.Run<bool>((Func<Task<bool>>)(async () =>
                {
                    HttpRequestMessage request = kioskEngineService._http.GenerateRequest("http://localhost:9002", "api/engine/shutdown", (HttpContent)null, HttpMethod.Post);
                    APIResponse<PerformShutdownResponse> apiResponse = await kioskEngineService._http.SendRequestAsync<PerformShutdownResponse>(request, callerMethod: nameof(PerformShutdown), callerLocation: "/sln/src/UpdateClientService.API/Services/KioskEngine/KioskEngineService.cs", logRequest: false, logResponse: false);
                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        this._logger.LogErrorWithSource("Encountered an error while requesting shutdown from Kiosk Engine. Response -> " + apiResponse.GetErrors(), nameof(PerformShutdown), "/sln/src/UpdateClientService.API/Services/KioskEngine/KioskEngineService.cs");
                        return false;
                    }
                    kioskEngineService.KioskEngineProcessId = (int?)apiResponse.Response?.ProcessId;
                    while (true)
                    {
                        if (await (kioskEngineService.GetStatus()) != KioskEngineStatus.Stopped && !cts.IsCancellationRequested)
                            await Task.Delay(3000);
                        else
                            break;
                    }
                    return !cts.IsCancellationRequested;
                }), cts.Token);
            return flag;
        }

        private async Task<int?> GetProcessId()
        {
            Process process = ((IEnumerable<Process>)Process.GetProcessesByName("kioskengine.exe")).FirstOrDefault<Process>();
            if (process != null)
                return new int?(process.Id);
            APIResponse<int?> apiResponse = await this._http.SendRequestAsync<int?>(this._http.GenerateRequest("http://localhost:9002", "api/engine/processid", (HttpContent)null, HttpMethod.Get), new int?(3000), nameof(GetProcessId), "/sln/src/UpdateClientService.API/Services/KioskEngine/KioskEngineService.cs", logRequest: false, logResponse: false);
            return apiResponse.IsSuccessStatusCode ? apiResponse.Response : new int?();
        }

        private static bool TryGetProcessById(int? id, out Process process)
        {
            process = (Process)null;
            if (!id.HasValue)
                return false;
            try
            {
                process = Process.GetProcessById(id.Value);
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static void HandleTimeout(
          int timeoutMs,
          int attempts,
          PerformShutdownResponse response)
        {
            response.Error = string.Format("Kiosk Engine did not shutdown given the provided parameters. Timeout: {0}ms, Attempts: {1}.", (object)timeoutMs, (object)attempts);
        }

        private static void HandleException(PerformShutdownResponse response, Exception e)
        {
            response.Error = e.Message;
        }
    }
}
