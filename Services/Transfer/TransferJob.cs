using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UpdateClientService.API.App;

namespace UpdateClientService.API.Services.Transfer
{
    public class TransferJob : ITransferJob
    {
        private List<ITransferItem> m_items;

        public List<Error> SetNoProgressTimeout(uint timeout)
        {
            List<Error> errors = new List<Error>();
            if (timeout < 1U)
                return errors;
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.SetNoProgressTimeout(timeout);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error setting no progress timeout of job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error setting no progress timeout of job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> SetMinimumRetryDelay(uint seconds)
        {
            List<Error> errors = new List<Error>();
            if (seconds < 60U)
                seconds = 60U;
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.SetMinimumRetryDelay(seconds);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error setting minimum retry delay of job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error setting minimum retry delay of job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> TakeOwnership()
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.TakeOwnership();
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error taking ownership of job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error taking ownership of job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> AddItem(string url, string file)
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            IEnumBackgroundCopyFiles pEnum = (IEnumBackgroundCopyFiles)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.TakeOwnership();
                ppJob.AddFile(url, file);
                ppJob.EnumFiles(out pEnum);
                this.m_items = TransferJob.GetFiles(pEnum);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error adding item to job. Given url: " + url + " Given file path: " + file, backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error adding item to job. Given url: " + url + " Given file path: " + file, errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
                if (pEnum != null)
                    Marshal.ReleaseComObject((object)pEnum);
            }
            return errors;
        }

        public List<Error> Complete()
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.Complete();
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error completing job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error completing job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> Cancel()
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.Cancel();
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error canceling job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error canceling job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> Suspend()
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.Suspend();
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error suspending job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error suspending job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> Resume()
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.Resume();
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error resuming job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error resuming job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> SetPriority(TransferJobPriority priority)
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.SetPriority((BG_JOB_PRIORITY)priority);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error setting job priority on job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error setting job priority on job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> SetCallback(ITransferCallbackParameters parameters)
        {
            List<Error> errors = new List<Error>();
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                ppJob.SetNotifyFlags(BG_JOB_NOTIFICATION_TYPE.BG_NOTIFY_JOB_TRANSFERRED | BG_JOB_NOTIFICATION_TYPE.BG_NOTIFY_JOB_ERROR);
                string Program = Path.Combine(parameters.Path, parameters.Executable);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (string str in parameters.Arguments)
                    stringBuilder.Append(string.Format(" {0}", (object)str));
                string Parameters = string.Format("{0}{1}", (object)Program, stringBuilder.Length > 0 ? (object)stringBuilder.ToString() : (object)string.Empty);
                ((IBackgroundCopyJob2)ppJob).SetNotifyCmdLine(Program, Parameters);
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error setting callback on job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error setting callback on job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (this.Status != TransferStatus.TransientError && this.Status != TransferStatus.Error)
                return errors;
            IBackgroundCopyManager backgroundCopyManager = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                Guid id = this.ID;
                backgroundCopyManager.GetJob(ref id, out ppJob);
                IBackgroundCopyError ppError;
                ppJob.GetError(out ppError);
                string pErrorDescription;
                ppError.GetErrorDescription(0U, out pErrorDescription);
                string pContextDescription;
                ppError.GetErrorContextDescription(0U, out pContextDescription);
                errors.Add(new Error()
                {
                    Code = "B999",
                    Message = pErrorDescription + ". " + pContextDescription
                });
            }
            catch (COMException ex)
            {
                this.HandleCOMException("Error getting errors on job", backgroundCopyManager, errors, ex);
            }
            catch (Exception ex)
            {
                this.HandleGenericException("Error getting errors on job", errors, ex);
            }
            finally
            {
                Marshal.ReleaseComObject((object)backgroundCopyManager);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
            return errors;
        }

        public List<Error> GetItems(out List<ITransferItem> items)
        {
            items = new List<ITransferItem>((IEnumerable<ITransferItem>)this.m_items.ToArray());
            return new List<Error>();
        }

        public TransferJobType JobType { get; private set; }

        public string Owner { get; private set; }

        public string Name { get; private set; }

        public Guid ID { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime ModifiedTime { get; private set; }

        public DateTime? FinishTime { get; private set; }

        public ulong TotalBytesTransfered { get; private set; }

        public ulong TotalBytes { get; private set; }

        public TransferStatus Status { get; private set; }

        internal TransferJob(Guid id, IBackgroundCopyJob job) => this.Initialize(id, job);

        internal TransferJob(Guid id)
        {
            IBackgroundCopyManager o = (IBackgroundCopyManager)new BackgroundCopyManager();
            IBackgroundCopyJob ppJob = (IBackgroundCopyJob)null;
            try
            {
                o.GetJob(ref id, out ppJob);
                this.ID = id;
                this.Initialize(id, ppJob);
            }
            finally
            {
                Marshal.ReleaseComObject((object)o);
                if (ppJob != null)
                    Marshal.ReleaseComObject((object)ppJob);
            }
        }

        private static List<ITransferItem> GetFiles(IEnumBackgroundCopyFiles files)
        {
            uint puCount;
            files.GetCount(out puCount);
            List<ITransferItem> files1 = new List<ITransferItem>();
            for (int index = 0; (long)index < (long)puCount; ++index)
            {
                IBackgroundCopyFile rgelt;
                files.Next(1U, out rgelt, out uint _);
                files1.Add((ITransferItem)new TransferItem(rgelt));
                Marshal.ReleaseComObject((object)rgelt);
            }
            return files1;
        }

        private void HandleCOMException(
          string baseMessage,
          IBackgroundCopyManager manager,
          List<Error> errors,
          COMException ex)
        {
            string pErrorDescription;
            manager.GetErrorDescription(ex.ErrorCode, 0U, out pErrorDescription);
            errors.AddRange((IEnumerable<Error>)new List<Error>()
      {
        new Error()
        {
          Code = "C999",
          Message = string.Format("{0}: {1}. {2}. Exception -> {3}", (object) baseMessage, (object) this.ID, (object) pErrorDescription, (object) ex.GetFullMessage())
        },
        TransferJob.GetLastWin32Error()
      });
        }

        private void HandleGenericException(string baseMessage, List<Error> errors, Exception e)
        {
            errors.Add(new Error()
            {
                Code = "C998",
                Message = string.Format("{0}: {1} -> Exception {2}", (object)baseMessage, (object)this.ID, (object)e.GetFullMessage())
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

        private void Initialize(Guid id, IBackgroundCopyJob job)
        {
            IEnumBackgroundCopyFiles pEnum = (IEnumBackgroundCopyFiles)null;
            try
            {
                string pVal1;
                job.GetOwner(out pVal1);
                this.Owner = pVal1;
                BG_JOB_TIMES pVal2;
                job.GetTimes(out pVal2);
                this.StartTime = pVal2.CreationTime.ToUTCDateTime().Value;
                this.FinishTime = pVal2.TransferCompletionTime.ToUTCDateTime();
                this.ModifiedTime = pVal2.ModificationTime.ToUTCDateTime() ?? this.StartTime;
                BG_JOB_PROGRESS pVal3;
                job.GetProgress(out pVal3);
                this.TotalBytesTransfered = pVal3.BytesTransferred;
                this.TotalBytes = pVal3.BytesTotal;
                string pVal4;
                job.GetDisplayName(out pVal4);
                this.Name = pVal4;
                job.EnumFiles(out pEnum);
                this.m_items = TransferJob.GetFiles(pEnum);
                BG_JOB_STATE pVal5;
                job.GetState(out pVal5);
                BG_JOB_TYPE pVal6;
                job.GetType(out pVal6);
                switch (pVal6)
                {
                    case BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD:
                        this.JobType = TransferJobType.Download;
                        break;
                    case BG_JOB_TYPE.BG_JOB_TYPE_UPLOAD:
                        this.JobType = TransferJobType.Upload;
                        break;
                }
                this.Status = (TransferStatus)pVal5;
                if (this.Status == TransferStatus.Error)
                {
                    job.GetError(out IBackgroundCopyError _);
                    job.GetErrorCount(out ulong _);
                }
                this.ID = id;
            }
            finally
            {
                if (pEnum != null)
                    Marshal.ReleaseComObject((object)pEnum);
            }
        }
    }
}
