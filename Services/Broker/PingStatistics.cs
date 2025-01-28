using System;
using System.Collections.Generic;
using System.Linq;

namespace UpdateClientService.API.Services.Broker
{
    public class PingStatistics : List<PingStatistic>, IPersistentData
    {
        public PingStatistic GetLatest()
        {
            return this.FirstOrDefault<PingStatistic>((Func<PingStatistic, bool>)(x => x.StartInterval == this.Max<PingStatistic, DateTime>((Func<PingStatistic, DateTime>)(y => y.StartInterval))));
        }

        public PingStatistic GetLastSuccessful()
        {
            PingStatistic lastSuccessful = (PingStatistic)null;
            foreach (PingStatistic pingStatistic in (List<PingStatistic>)this)
            {
                if (pingStatistic.PingSuccess && (lastSuccessful == null || pingStatistic.EndIntervalUTC > lastSuccessful.EndIntervalUTC))
                    lastSuccessful = pingStatistic;
            }
            return lastSuccessful;
        }

        public void Cleanup()
        {
            this.Where<PingStatistic>((Func<PingStatistic, bool>)(x =>
            {
                DateTime dateTime1 = x.StartInterval;
                DateTime date = dateTime1.Date;
                dateTime1 = DateTime.Today;
                DateTime dateTime2 = dateTime1.AddDays(-30.0);
                return date < dateTime2;
            })).ToList<PingStatistic>().ForEach((Action<PingStatistic>)(x => this.Remove(x)));
        }
    }
}
