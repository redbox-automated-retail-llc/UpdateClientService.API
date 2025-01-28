using System;
using System.Collections.Generic;
using System.Linq;

namespace UpdateClientService.API.Services.IoT
{
    public class IoTStatistics : IPersistentData
    {
        public List<IoTConnectionAttempt> ConnectionAttempts { get; set; } = new List<IoTConnectionAttempt>();

        public void StartConnectionAttempt()
        {
            IoTConnectionAttempt tconnectionAttempt = this.GetLatest();
            if (tconnectionAttempt != null && tconnectionAttempt.Connected.HasValue)
                tconnectionAttempt = (IoTConnectionAttempt)null;
            if (tconnectionAttempt == null)
                tconnectionAttempt = this.CreateIoTConnectionAttempt();
            tconnectionAttempt.LatestConnectionAttempt = tconnectionAttempt.LatestConnectionAttempt.HasValue ? new DateTime?(DateTime.Now) : tconnectionAttempt.StartConnectionAttempt;
            ++tconnectionAttempt.ConnectionAttempts;
        }

        public void Connected()
        {
            IoTConnectionAttempt latest = this.GetLatest();
            if (latest == null)
                return;
            latest.Connected = new DateTime?(DateTime.Now);
        }

        public void Disconnected()
        {
            IoTConnectionAttempt latest = this.GetLatest();
            if (latest == null || !latest.Connected.HasValue)
                return;
            latest.Disconnected = new DateTime?(DateTime.Now);
        }

        public void AddException(string exceptionMessage)
        {
            IoTConnectionAttempt latest = this.GetLatest();
            if (latest == null)
                return;
            if (latest.ConnectionExceptions == null)
                latest.ConnectionExceptions = new Dictionary<string, int>();
            int num1 = 0;
            latest.ConnectionExceptions.TryGetValue(exceptionMessage, out num1);
            int num2 = num1 + 1;
            latest.ConnectionExceptions[exceptionMessage] = num2;
        }

        private IoTConnectionAttempt GetLatest()
        {
            return this.ConnectionAttempts.FirstOrDefault<IoTConnectionAttempt>((Func<IoTConnectionAttempt, bool>)(x =>
            {
                DateTime? connectionAttempt = x.StartConnectionAttempt;
                DateTime? nullable = this.ConnectionAttempts.Max<IoTConnectionAttempt, DateTime?>((Func<IoTConnectionAttempt, DateTime?>)(y => y.StartConnectionAttempt));
                if (connectionAttempt.HasValue != nullable.HasValue)
                    return false;
                return !connectionAttempt.HasValue || connectionAttempt.GetValueOrDefault() == nullable.GetValueOrDefault();
            }));
        }

        private IoTConnectionAttempt CreateIoTConnectionAttempt()
        {
            IoTConnectionAttempt tconnectionAttempt = new IoTConnectionAttempt()
            {
                StartConnectionAttempt = new DateTime?(DateTime.Now)
            };
            this.ConnectionAttempts.Add(tconnectionAttempt);
            return tconnectionAttempt;
        }

        public void Cleanup()
        {
            this.ConnectionAttempts.Where<IoTConnectionAttempt>((Func<IoTConnectionAttempt, bool>)(x => x.StartConnectionAttempt.HasValue && x.StartConnectionAttempt.Value.Date < DateTime.Today.AddDays(-30.0))).ToList<IoTConnectionAttempt>().ForEach((Action<IoTConnectionAttempt>)(x => this.ConnectionAttempts.Remove(x)));
        }
    }
}
