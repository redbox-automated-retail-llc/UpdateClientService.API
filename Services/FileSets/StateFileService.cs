using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public class StateFileService : IStateFileService
    {
        private readonly IStateFileRepository _stateFileRepository;
        private readonly ILogger<StateFileService> _logger;

        public StateFileService(
          ILogger<StateFileService> logger,
          IStateFileRepository stateFileRepository)
        {
            this._logger = logger;
            this._stateFileRepository = stateFileRepository;
        }

        public async Task<bool> Delete(long fileSetId)
        {
            try
            {
                return await this._stateFileRepository.Delete(fileSetId);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while deleting StateFile for FileSetId {0}", (object)fileSetId), nameof(Delete), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                return false;
            }
        }

        public async Task<bool> DeleteInProgress(long fileSetId)
        {
            try
            {
                return await this._stateFileRepository.DeleteInProgress(fileSetId);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while deleting StateFile for FileSetId {0}", (object)fileSetId), nameof(DeleteInProgress), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                return false;
            }
        }

        public async Task<StateFileResponse> Save(StateFile stateFile)
        {
            try
            {
                HttpStatusCode httpStatusCode = await this._stateFileRepository.Save(stateFile) ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                StateFileResponse stateFileResponse = new StateFileResponse();
                stateFileResponse.StatusCode = httpStatusCode;
                stateFileResponse.StateFile = stateFile;
                return stateFileResponse;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while saving StateFile for FileSetId {0}", (object)stateFile?.FileSetId), nameof(Save), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                StateFileResponse stateFileResponse = new StateFileResponse();
                stateFileResponse.StatusCode = HttpStatusCode.InternalServerError;
                return stateFileResponse;
            }
        }

        public async Task<StateFileResponse> Get(long fileSetId)
        {
            try
            {
                (bool flag, StateFile stateFile) = await this._stateFileRepository.Get(fileSetId);
                HttpStatusCode httpStatusCode = !flag || stateFile == null ? (!flag || stateFile != null ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound) : HttpStatusCode.OK;
                StateFileResponse stateFileResponse = new StateFileResponse();
                stateFileResponse.StatusCode = httpStatusCode;
                stateFileResponse.StateFile = stateFile;
                return stateFileResponse;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while getting StateFile for FileSetId {0}", (object)fileSetId), nameof(Get), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                StateFileResponse stateFileResponse = new StateFileResponse();
                stateFileResponse.StatusCode = HttpStatusCode.InternalServerError;
                return stateFileResponse;
            }
        }

        public async Task<StateFilesResponse> GetAll()
        {
            try
            {
                (bool flag, List<StateFile> stateFileList) = await this._stateFileRepository.GetAll();
                HttpStatusCode httpStatusCode = !flag || stateFileList.Count <= 0 ? (!flag || stateFileList.Count != 0 ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound) : HttpStatusCode.OK;
                StateFilesResponse all = new StateFilesResponse();
                all.StatusCode = httpStatusCode;
                all.StateFiles = stateFileList;
                return all;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting StateFiles", nameof(GetAll), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                StateFilesResponse all = new StateFilesResponse();
                all.StatusCode = HttpStatusCode.InternalServerError;
                return all;
            }
        }

        public async Task<StateFilesResponse> GetAllInProgress()
        {
            try
            {
                (bool, List<StateFile>) all = await this._stateFileRepository.GetAll();
                bool flag = all.Item1;
                List<StateFile> list = all.Item2.Where<StateFile>((Func<StateFile, bool>)(x => x.IsRevisionDownloadInProgress)).ToList<StateFile>();
                HttpStatusCode httpStatusCode = !flag || list.Count <= 0 ? (!flag || list.Count != 0 ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound) : HttpStatusCode.OK;
                StateFilesResponse allInProgress = new StateFilesResponse();
                allInProgress.StatusCode = httpStatusCode;
                allInProgress.StateFiles = list;
                return allInProgress;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting in progress StateFiles", nameof(GetAllInProgress), "/sln/src/UpdateClientService.API/Services/FileSets/StateFileService.cs");
                StateFilesResponse allInProgress = new StateFilesResponse();
                allInProgress.StatusCode = HttpStatusCode.InternalServerError;
                return allInProgress;
            }
        }
    }
}
