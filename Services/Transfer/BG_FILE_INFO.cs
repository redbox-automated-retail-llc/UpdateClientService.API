using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct BG_FILE_INFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string RemoteName;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string LocalName;
    }
}
