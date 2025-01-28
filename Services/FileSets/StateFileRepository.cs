using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public class StateFileRepository : IStateFileRepository
    {
        private readonly ILogger<StateFileRepository> _logger;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _lockWait = 2000;
        private const string FileSetStateActiveExt = ".active";
        private const string FileSetStateInProgressExt = ".inprogress";

        public StateFileRepository(ILogger<StateFileRepository> logger) => this._logger = logger;

        public async Task<bool> Delete(long fileSetId)
        {
            return await this.Delete(fileSetId, new List<string>()
      {
        ".active",
        ".inprogress"
      });
        }

        public async Task<bool> DeleteInProgress(long fileSetId)
        {
            return await this.Delete(fileSetId, new List<string>()
      {
        ".inprogress"
      });
        }

        private async Task<bool> Delete(long fileSetId, List<string> fileExtensions)
        {
            if (await StateFileRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    if (fileExtensions == null)
                        return false;
                    foreach (string fileExtension in fileExtensions)
                        Shared.SafeDelete(StateFileRepository.GetStateFilePath(fileSetId, fileExtension));
                    return true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, string.Format("Exception while deleting StateFile for FileSetId {0}.", (object)fileSetId), nameof(Delete), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                    return false;
                }
                finally
                {
                    StateFileRepository._lock.Release();
                }
            }
            else
            {
                this._logger.LogErrorWithSource("Lock failed.", nameof(Delete), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                return false;
            }
        }

        public async Task<bool> Save(StateFile stateFile)
        {
            StateFileRepository stateFileRepository = this;
            if (stateFile == null)
            {
                this._logger.LogErrorWithSource("StateFile is null", nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                return false;
            }
            if (await StateFileRepository._lock.WaitAsync(stateFileRepository._lockWait))
            {
                try
                {
                    bool flag = await stateFileRepository.SaveClientFileSetState(new ClientFileSetState()
                    {
                        FileSetId = stateFile.FileSetId,
                        RevisionId = stateFile.ActiveRevisionId,
                        FileSetState = FileSetState.Active
                    });
                    if (flag)
                        flag = await stateFileRepository.SaveClientFileSetState(new ClientFileSetState()
                        {
                            FileSetId = stateFile.FileSetId,
                            RevisionId = stateFile.InProgressRevisionId,
                            FileSetState = stateFile.InProgressRevisionId != 0L ? stateFile.InProgressFileSetState : FileSetState.Error
                        });
                    return flag;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, string.Format("Exception while saving StateFile for FileSetId {0}.", (object)stateFile?.FileSetId), nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                    return false;
                }
                finally
                {
                    StateFileRepository._lock.Release();
                }
            }
            else
            {
                this._logger.LogErrorWithSource("Lock failed.", nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                return false;
            }
        }

        private async Task<bool> SaveClientFileSetState(ClientFileSetState clientFileSetState)
        {
            if (clientFileSetState == null)
                return false;
            string stateFilePath = StateFileRepository.GetStateFilePath(clientFileSetState.FileSetId, clientFileSetState.FileSetState == FileSetState.Active ? ".active" : ".inprogress");
            if (clientFileSetState.RevisionId != 0L)
            {
                try
                {
                    File.WriteAllText(stateFilePath, clientFileSetState.ToJson());
                    return true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception saving StateFile to " + stateFilePath, nameof(SaveClientFileSetState), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                    return false;
                }
            }
            else
            {
                Shared.SafeDelete(stateFilePath);
                return true;
            }
        }

        public async Task<(bool, StateFile)> Get(long fileSetId)
        {
            (bool flag, List<ClientFileSetState> clientFileSetStates) = await this.GetClientFileSetStates(new long?(fileSetId));
            StateFile stateFile = flag ? this.CreateStateFile(clientFileSetStates, fileSetId) : (StateFile)null;
            return (flag, stateFile);
        }

        public async Task<(bool, List<StateFile>)> GetAll()
        {
            List<StateFile> stateFiles = new List<StateFile>();
            (bool flag, List<ClientFileSetState> clientFileSetStateList) = await this.GetClientFileSetStates();
            foreach (long fileSetId in clientFileSetStateList.Select<ClientFileSetState, long>((Func<ClientFileSetState, long>)(x => x.FileSetId)).Distinct<long>())
            {
                StateFile stateFile = this.CreateStateFile(clientFileSetStateList, fileSetId);
                if (stateFile != null)
                    stateFiles.Add(stateFile);
            }
            return (flag, stateFiles);
        }

        private StateFile CreateStateFile(List<ClientFileSetState> clientFileSetStates, long fileSetId)
        {
            ClientFileSetState clientFileSetState1 = clientFileSetStates != null ? clientFileSetStates.FirstOrDefault<ClientFileSetState>((Func<ClientFileSetState, bool>)(x => x.FileSetId == fileSetId && x.FileSetState == FileSetState.Active)) : (ClientFileSetState)null;
            ClientFileSetState clientFileSetState2 = clientFileSetStates != null ? clientFileSetStates.FirstOrDefault<ClientFileSetState>((Func<ClientFileSetState, bool>)(x => x.FileSetId == fileSetId && x.FileSetState != 0)) : (ClientFileSetState)null;
            return clientFileSetState1 == null && clientFileSetState2 == null ? (StateFile)null : new StateFile(fileSetId, clientFileSetState1 != null ? clientFileSetState1.RevisionId : 0L, clientFileSetState2 != null ? clientFileSetState2.RevisionId : 0L, clientFileSetState2 != null ? clientFileSetState2.FileSetState : FileSetState.Active);
        }

        private async Task<(bool, List<ClientFileSetState>)> GetClientFileSetStates(long? fileSetId = null)
        {
            List<ClientFileSetState> stateFiles = new List<ClientFileSetState>();
            if (await StateFileRepository._lock.WaitAsync(this._lockWait))
            {
                try
                {
                    if (!Directory.Exists(Constants.FileSetsRoot))
                    {
                        this._logger.LogErrorWithSource("StateFile directory " + Constants.FileSetsRoot + " does not exist.", nameof(GetClientFileSetStates), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                        return (false, stateFiles);
                    }
                    List<string> stringList = new List<string>();
                    string str = fileSetId.HasValue ? fileSetId.ToString() : "*";
                    stringList.AddRange((IEnumerable<string>)((IEnumerable<string>)Directory.GetFiles(Constants.FileSetsRoot, str + ".active", SearchOption.TopDirectoryOnly)).ToList<string>());
                    stringList.AddRange((IEnumerable<string>)((IEnumerable<string>)Directory.GetFiles(Constants.FileSetsRoot, str + ".inprogress", SearchOption.TopDirectoryOnly)).ToList<string>());
                    foreach (string filePath in stringList)
                    {
                        ClientFileSetState clientFileSetState = await this.Load(filePath);
                        if (clientFileSetState != null)
                            stateFiles.Add(clientFileSetState);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while getting StateFiles.", nameof(GetClientFileSetStates), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                    return (false, stateFiles);
                }
                finally
                {
                    StateFileRepository._lock.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed.", nameof(GetClientFileSetStates), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
            return (true, stateFiles);
        }

        private async Task<ClientFileSetState> Load(string filePath)
        {
            ClientFileSetState result = (ClientFileSetState)null;
            try
            {
                if (!string.IsNullOrEmpty(File.ReadAllText(filePath)))
                {
                    try
                    {
                        result = File.ReadAllText(filePath).ToObject<ClientFileSetState>();
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogErrorWithSource(ex, "Exception while deserializing StateFile " + filePath, nameof(Load), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
                    }
                }
                else
                    this._logger.LogErrorWithSource("StateFile " + filePath + " was null", nameof(Load), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while reading file " + filePath, nameof(Load), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileRepository.cs");
            }
            if (result == null)
                Shared.SafeDelete(filePath);
            return result;
        }

        private static string GetStateFilePath(long fileSetId, string fileExtension)
        {
            return Path.ChangeExtension(Path.Combine(Constants.FileSetsRoot, fileSetId.ToString()), fileExtension);
        }
    }
}
