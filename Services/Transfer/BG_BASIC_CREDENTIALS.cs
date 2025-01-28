using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct BG_BASIC_CREDENTIALS
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string UserName;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string Password;
    }
}
