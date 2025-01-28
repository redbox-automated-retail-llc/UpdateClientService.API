namespace UpdateClientService.API.Services.FileSets
{
    public enum ChangesetState
    {
        Received = 0,
        Error = 1,
        DownloadingRevision = 10, // 0x0000000A
        DownloadedRevision = 11, // 0x0000000B
        DownloadingFileSet = 12, // 0x0000000C
        DownloadedFileSet = 13, // 0x0000000D
        Staging = 20, // 0x00000014
        Staged = 21, // 0x00000015
        ActivationDependencyCheck = 30, // 0x0000001E
        ActivationPending = 31, // 0x0000001F
        ActivationBeforeActions = 32, // 0x00000020
        Activating = 33, // 0x00000021
        ActivationAfterActions = 34, // 0x00000022
        Activated = 35, // 0x00000023
    }
}
