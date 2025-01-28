namespace UpdateClientService.API.Services.FileSets
{
    public interface IZipDownloadHelper
    {
        bool Extract(string _zipPath, RevisionChangeSetKey revisionChangeSetKey);
    }
}
