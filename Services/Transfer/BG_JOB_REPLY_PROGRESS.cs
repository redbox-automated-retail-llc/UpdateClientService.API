using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct BG_JOB_REPLY_PROGRESS
    {
        internal ulong BytesTotal;
        internal ulong BytesTransferred;
    }
}
