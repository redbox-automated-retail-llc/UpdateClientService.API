using Nito.AsyncEx;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API
{
    public static class ResetEventExtensions
    {
        public static async Task<bool> WaitAsyncAndDisposeOnFinish(
          this AsyncManualResetEvent mre,
          TimeSpan cancelAfter,
          IoTCommandModel request = null)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource(cancelAfter);
            bool flag = false;
            try
            {
                await mre.WaitAsync(tokenSource.Token);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "ResetEventExtensions.WaitAsyncAndDisposeOnFinish -> Error occured while waiting" + (request != null ? " for request id " + request?.RequestId : (string)null));
            }
            finally
            {
                flag = tokenSource.IsCancellationRequested;
                tokenSource.Dispose();
            }
            return flag;
        }
    }
}
