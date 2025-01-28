using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct BG_FILE_PROGRESS
    {
        internal ulong BytesTotal;
        internal ulong BytesTransferred;
        internal int Completed;
    }
}
