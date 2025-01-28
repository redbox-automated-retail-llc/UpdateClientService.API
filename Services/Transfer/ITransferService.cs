using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.Transfer
{
    public interface ITransferService
    {
        List<Error> CancelAll();

        List<Error> GetRepositoriesInTransit(out HashSet<long> inTransit);

        List<Error> GetJobs(out List<ITransferJob> jobs, bool allUsers);

        List<Error> CreateDownloadJob(string name, out ITransferJob job);

        List<Error> CreateUploadJob(string name, out ITransferJob job);

        List<Error> GetJob(Guid id, out ITransferJob job);

        List<Error> AreJobsRunning(out bool isRunning);

        bool SetMaxBandwidthWhileWithInSchedule(int i);

        int MaxBandwidthWhileWithInSchedule { get; }

        bool SetMaxBandwidthWhileOutsideOfSchedule(int max);

        int MaxBandwidthWhileOutsideOfSchedule { get; }

        bool SetStartOfScheduleInHoursFromMidnight(byte b);

        byte StartOfScheduleInHoursFromMidnight { get; }

        bool SetEndOfScheduleInHoursFromMidnight(byte b);

        byte EndOfScheduleInHoursFromMidnight { get; }

        bool SetUseSystemMaxOutsideOfSchedule(bool flag);

        bool UseSystemMaxOutsideOfSchedule { get; }

        bool SetEnableMaximumBandwitdthThrottle(bool flag);

        bool EnableMaximumBandwitdthThrottle { get; }
    }
}
