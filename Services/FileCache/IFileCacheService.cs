using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.FileCache
{
    public interface IFileCacheService
    {
        bool DoesFileExist(long fileSetId, long fileId, long fileRevisionId);

        bool IsFileHashValid(long fileSetId, long fileId, long fileRevisionId, string fileHash);

        bool IsFileHashValid(string filePath, string hash);

        bool DoesRevisionExist(RevisionChangeSetKey revisionChangeSetKey);

        bool AddFile(long fileSetId, long fileId, long fileRevisionId, byte[] data, string info);

        bool AddFilePatch(
          long fileSetId,
          long fileId,
          long fileRevisionId,
          long filePatchRevisionId,
          byte[] data,
          string info);

        bool AddRevision(RevisionChangeSetKey revisionChangeSetKey, byte[] data, string info);

        void DeleteFile(long fileSetId, long fileId, long fileRevisionId, bool force = false);

        void DeleteRevision(RevisionChangeSetKey revisionChangeSetKey);

        Task<FileSetFileInfo> GetFileInfo(string filePath);

        Task<List<FileSetFileInfo>> GetFileInfos(long fileSetId);

        bool GetFile(long fileSetId, long fileId, long fileRevisionId, out byte[] data);

        bool CopyFileToPath(long fileSetId, long fileId, long fileRevisionId, string copyToPath);

        Task<ClientFileSetRevision> GetClientFileSetRevision(string filePath);

        Task<ClientFileSetRevision> GetAnyClientFileSetRevision(long fileSetId, long revisionId);

        Task<ClientFileSetRevision> GetClientFileSetRevision(RevisionChangeSetKey revisionChangeSetKey);

        string GetRevisionFileName(long fileId, long fileRevisionId);

        List<string> GetClientFileSetRevisionFilePaths(long fileSetId);

        List<string> GetClientFileSetRevisionFilePaths(long fileSetId, long revisionId);

        DateTime GetRevisionCreationDate(long fileSetId, long revisionId);

        List<long> GetFileSetIds();

        List<long> GetFileSetRevisionIds(long fileSetId);

        List<string> GetFileInfoPaths(long fileSetId);
    }
}
