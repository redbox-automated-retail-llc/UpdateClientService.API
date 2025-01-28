using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using UpdateClientService.API.App;

namespace UpdateClientService.API.Services.Transfer
{
    public class TransferService : ITransferService
    {
        private const uint EnumAllUsers = 1;
        private const uint EnumCurrentUser = 0;
        private readonly ILogger<TransferService> _logger;

        public TransferService(ILogger<TransferService> logger) => this._logger = logger;

        public List<Error> AreJobsRunning(out bool isRunning)
        {
            isRunning = false;
            List<Error> source = new List<Error>();
            List<ITransferJob> jobs;
            source.AddRange((IEnumerable<Error>)this.GetJobs(out jobs, false));
            if (source.Any<Error>() || jobs.Count <= 0 || jobs.FindIndex((Predicate<ITransferJob>)(j => j.Name.StartsWith("<~") && j.Name.EndsWith("~>"))) <= -1)
                return source;
            isRunning = true;
            return source;
        }

        public List<Error> CancelAll()
        {
            List<ITransferJob> jobs1;
            List<Error> jobs2 = this.GetJobs(out jobs1, false);
            if (!jobs2.Any<Error>())
                jobs1.ForEach((Action<ITransferJob>)(j =>
                {
                    if (j.Status == TransferStatus.Transferred)
                        j.Complete();
                    else
                        j.Cancel();
                }));
            return jobs2;
        }

        public List<Error> GetRepositoriesInTransit(out HashSet<long> inTransit)
        {
            inTransit = new HashSet<long>();
            List<Error> source = new List<Error>();
            List<ITransferJob> jobs;
            source.AddRange((IEnumerable<Error>)this.GetJobs(out jobs, false));
            if (source.Any<Error>())
                return source;
            foreach (ITransferJob transferJob in jobs)
            {
                int status = (int)transferJob.Status;
            }
            return source;
        }

        public List<Error> GetJobs(out List<ITransferJob> jobs, bool allUsers)
        {
            jobs = new List<ITransferJob>();
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager o = (IBackgroundCopyManager)new BackgroundCopyManager();
            IEnumBackgroundCopyJobs ppenum = (IEnumBackgroundCopyJobs)null;
            try
            {
                o.EnumJobs(allUsers ? 1U : 0U, out ppenum);
                uint puCount;
                ppenum.GetCount(out puCount);
                for (int index = 0; (long)index < (long)puCount; ++index)
                {
                    IBackgroundCopyJob rgelt;
                    ppenum.Next(1U, out rgelt, out uint _);
                    Guid pVal;
                    rgelt.GetId(out pVal);
                    jobs.Add((ITransferJob)new TransferJob(pVal, rgelt));
                    Marshal.ReleaseComObject((object)rgelt);
                }
                jobs.Sort((Comparison<ITransferJob>)((lhs, rhs) =>
                {
                    if (lhs.FinishTime.HasValue && rhs.FinishTime.HasValue)
                        return lhs.FinishTime.Value.CompareTo(rhs.FinishTime.Value);
                    if (lhs.FinishTime.HasValue && !rhs.FinishTime.HasValue)
                        return 1;
                    return !lhs.FinishTime.HasValue && rhs.FinishTime.HasValue ? -1 : lhs.StartTime.CompareTo(rhs.StartTime);
                }));
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error getting BITS job", errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException(errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)o);
                if (ppenum != null)
                    Marshal.ReleaseComObject((object)ppenum);
            }
            return errors;
        }

        public List<Error> CreateDownloadJob(string name, out ITransferJob job)
        {
            job = (ITransferJob)null;
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager o = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid pJobId;
                o.CreateJob(name, BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD, out pJobId, out ppJob);
                job = (ITransferJob)new TransferJob(pJobId, ppJob);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error creating BITS job", errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException(errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)o);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> CreateUploadJob(string name, out ITransferJob job)
        {
            job = (ITransferJob)null;
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager o = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid pJobId;
                o.CreateJob(name, BG_JOB_TYPE.BG_JOB_TYPE_UPLOAD, out pJobId, out ppJob);
                job = (ITransferJob)new TransferJob(pJobId, ppJob);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error creating BITS job", errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException(errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)o);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> GetJob(Guid id, out ITransferJob job)
        {
            List<Error> errors = new List<Error>();
            job = (ITransferJob)null;
            try
            {
                job = (ITransferJob)new TransferJob(id);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error accessing BITS job", errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException(errors, ex);
            }
            return errors;
        }

        public bool SetMaxBandwidthWhileWithInSchedule(int i)
        {
            bool flag = this.MaxBandwidthWhileWithInSchedule != i;
            this.MaxBandwidthWhileWithInSchedule = i;
            return flag;
        }

        public int MaxBandwidthWhileWithInSchedule
        {
            get => BandwidthUsageSettings.MaxBandwidthWhileWithInSchedule;
            private set => BandwidthUsageSettings.MaxBandwidthWhileWithInSchedule = value;
        }

        public bool SetMaxBandwidthWhileOutsideOfSchedule(int max)
        {
            bool flag = false;
            try
            {
                flag = this.MaxBandwidthWhileOutsideOfSchedule != max;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error Accessing BITS settings.", nameof(SetMaxBandwidthWhileOutsideOfSchedule), "/sln/src/UpdateClientService.API/Services/TransferService/TransferService.cs");
            }
            this.MaxBandwidthWhileOutsideOfSchedule = max;
            return flag;
        }

        public int MaxBandwidthWhileOutsideOfSchedule
        {
            get => BandwidthUsageSettings.MaxBandwidthWhileWithInSchedule;
            private set => BandwidthUsageSettings.MaxBandwidthWhileWithInSchedule = value;
        }

        public bool SetStartOfScheduleInHoursFromMidnight(byte b)
        {
            bool flag = false;
            try
            {
                flag = (int)this.StartOfScheduleInHoursFromMidnight != (int)b;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error Accessing BITS settings.", nameof(SetStartOfScheduleInHoursFromMidnight), "/sln/src/UpdateClientService.API/Services/TransferService/TransferService.cs");
            }
            this.StartOfScheduleInHoursFromMidnight = b;
            return flag;
        }

        public byte StartOfScheduleInHoursFromMidnight
        {
            get => BandwidthUsageSettings.StartOfScheduleInHoursFromMidnight;
            private set => BandwidthUsageSettings.StartOfScheduleInHoursFromMidnight = value;
        }

        public bool SetEndOfScheduleInHoursFromMidnight(byte b)
        {
            bool flag = false;
            try
            {
                flag = (int)this.EndOfScheduleInHoursFromMidnight != (int)b;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error Accessing BITS settings.", nameof(SetEndOfScheduleInHoursFromMidnight), "/sln/src/UpdateClientService.API/Services/TransferService/TransferService.cs");
            }
            this.EndOfScheduleInHoursFromMidnight = b;
            return flag;
        }

        public byte EndOfScheduleInHoursFromMidnight
        {
            get => BandwidthUsageSettings.EndOfScheduleInHoursFromMidnight;
            private set => BandwidthUsageSettings.EndOfScheduleInHoursFromMidnight = value;
        }

        public bool SetUseSystemMaxOutsideOfSchedule(bool flag)
        {
            bool flag1 = false;
            try
            {
                flag1 = this.UseSystemMaxOutsideOfSchedule != flag;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error Accessing BITS settings.", nameof(SetUseSystemMaxOutsideOfSchedule), "/sln/src/UpdateClientService.API/Services/TransferService/TransferService.cs");
            }
            this.UseSystemMaxOutsideOfSchedule = flag;
            return flag1;
        }

        public bool UseSystemMaxOutsideOfSchedule
        {
            get => BandwidthUsageSettings.UseSystemMaxOutsideOfSchedule;
            private set => BandwidthUsageSettings.UseSystemMaxOutsideOfSchedule = value;
        }

        public bool SetEnableMaximumBandwitdthThrottle(bool flag)
        {
            bool flag1 = false;
            try
            {
                flag1 = this.EnableMaximumBandwitdthThrottle != flag;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error Accessing BITS settings.", nameof(SetEnableMaximumBandwitdthThrottle), "/sln/src/UpdateClientService.API/Services/TransferService/TransferService.cs");
            }
            this.EnableMaximumBandwitdthThrottle = flag;
            return flag1;
        }

        public bool EnableMaximumBandwitdthThrottle
        {
            get => BandwidthUsageSettings.EnableMaximumBandwitdthThrottle;
            private set => BandwidthUsageSettings.EnableMaximumBandwitdthThrottle = value;
        }

        private void HandleCOMException(string baseMessage, List<Error> errors, COMException ex)
        {
            errors.AddRange((IEnumerable<Error>)new List<Error>()
      {
        new Error()
        {
          Code = "C999",
          Message = baseMessage + ". Exception -> " + ex.GetFullMessage()
        },
        TransferService.GetLastWin32Error()
      });
        }

        private void HandleGenericException(List<Error> errors, Exception e)
        {
            errors.Add(new Error()
            {
                Code = "C998",
                Message = "Error accessing BITS job. Exception -> " + e.GetFullMessage()
            });
        }

        private static Error GetLastWin32Error()
        {
            return new Error()
            {
                Code = "B999",
                Message = "Error accessing BITS job. Exception -> " + new Win32Exception(Marshal.GetLastWin32Error()).GetFullMessage()
            };
        }
    }
}
