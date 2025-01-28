using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct BG_FILE_RANGE
    {
        internal const ulong BG_LENGTH_TO_EOF = 18446744073709551615;
        internal ulong InitialOffset;
        internal ulong Length;
    }
}
