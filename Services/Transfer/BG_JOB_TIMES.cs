using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct BG_JOB_TIMES
    {
        internal FILETIME CreationTime;
        internal FILETIME ModificationTime;
        internal FILETIME TransferCompletionTime;
    }
}
