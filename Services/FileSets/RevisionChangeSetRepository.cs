using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public class RevisionChangeSetRepository : IRevisionChangeSetRepository
    {
        private readonly ILogger<RevisionChangeSetRepository> _logger;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _lockWait = 2000;
        private const string FileSetChangeSetExt = ".changeSet";
        private const string FileSetChangeSetPath = "changesets";

        public RevisionChangeSetRepository(ILogger<RevisionChangeSetRepository> logger)
        {
            this._logger = logger;
            this.CreateChangeSetDirectoryIfNeeded();
        }

        public async Task<List<RevisionChangeSet>> GetAll()
        {
            List<RevisionChangeSet> result = new List<RevisionChangeSet>();
            if (!Directory.Exists(this.ChangeSetDirectory))
                return result;
            if (await RevisionChangeSetRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    string[] strArray = Directory.GetFiles(this.ChangeSetDirectory, "*.changeSet", SearchOption.TopDirectoryOnly);
                    for (int index = 0; index < strArray.Length; ++index)
                    {
                        string eachFile = strArray[index];
                        try
                        {
                            result.Add(File.ReadAllText(eachFile).ToObject<RevisionChangeSet>());
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Exception while getting RevisionChangeSet from file " + eachFile, nameof(GetAll), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                            Shared.SafeDelete(eachFile);
                        }
                        eachFile = (string)null;
                    }
                    strArray = (string[])null;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while getting RevisionChangeSets", nameof(GetAll), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                }
                finally
                {
                    RevisionChangeSetRepository._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed.", nameof(GetAll), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
            return result;
        }

        public async Task<bool> Save(RevisionChangeSet revisionChangeSet)
        {
            bool result = false;
            if (await RevisionChangeSetRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    File.WriteAllText(this.GetChangeSetPath(revisionChangeSet), revisionChangeSet.ToJson());
                    result = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while saving RevisionChangeSet", nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                }
                finally
                {
                    RevisionChangeSetRepository._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed.", nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
            return result;
        }

        public async Task<bool> Delete(RevisionChangeSet revisionChangeSet)
        {
            bool result = false;
            if (await RevisionChangeSetRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    string changeSetPath = this.GetChangeSetPath(revisionChangeSet);
                    if (File.Exists(changeSetPath))
                        File.Delete(changeSetPath);
                    result = true;
                }
                catch (Exception ex)
                {
                    ILogger<RevisionChangeSetRepository> logger = this._logger;
                    Exception exception = ex;
                    RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
                    string str = "Exception while deleting RevisionChangeSet " + (revisionChangeSet1 != null ? revisionChangeSet1.ToJson() : (string)null);
                    this._logger.LogErrorWithSource(exception, str, nameof(Delete), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                }
                finally
                {
                    RevisionChangeSetRepository._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed.", nameof(Delete), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
            return result;
        }

        public async Task<bool> Cleanup(DateTime deleteDate)
        {
            if (!Directory.Exists(this.ChangeSetDirectory))
                return true;
            bool result = false;
            if (await RevisionChangeSetRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    string[] files = Directory.GetFiles(this.ChangeSetDirectory, "*.changeSet", SearchOption.TopDirectoryOnly);
                    result = true;
                    foreach (string path in files)
                    {
                        try
                        {
                            if (File.GetCreationTime(path) <= deleteDate)
                            {
                                File.Delete(path);
                                this._logger.LogInfoWithSource("Deleting file " + path, nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                            }
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Exception while cleaning up RevisionChangeSet from file " + path, nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                            result = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while cleaning up RevisionChangeSets", nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
                    result = false;
                }
                finally
                {
                    RevisionChangeSetRepository._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed.", nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
            return result;
        }

        private string GetChangeSetPath(RevisionChangeSet revisionChangeSet)
        {
            return this.GetChangeSetPath(revisionChangeSet != null ? revisionChangeSet.FileSetId : 0L, revisionChangeSet != null ? revisionChangeSet.RevisionId : 0L);
        }

        private string GetChangeSetPath(long fileSetId, long revisionId)
        {
            return Path.ChangeExtension(Path.Combine(this.ChangeSetDirectory, string.Format("{0}-{1}", (object)fileSetId, (object)revisionId)), ".changeSet");
        }

        private string ChangeSetDirectory => Path.Combine(Constants.FileSetsRoot, "changesets");

        private void CreateChangeSetDirectoryIfNeeded()
        {
            try
            {
                if (Directory.Exists(this.ChangeSetDirectory))
                    return;
                Directory.CreateDirectory(this.ChangeSetDirectory);
            }
            catch (Exception ex)
            {
                ILogger<RevisionChangeSetRepository> logger = this._logger;
                if (logger == null)
                    return;
                this._logger.LogErrorWithSource(ex, "Exception creating directory " + this.ChangeSetDirectory, nameof(CreateChangeSetDirectoryIfNeeded), "/sln/src/UpdateClientService.API/Services/FileSets/RevisionChangeSetRepository.cs");
            }
        }
    }
}
