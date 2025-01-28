using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct BG_AUTH_CREDENTIALS_UNION
    {
        internal BG_BASIC_CREDENTIALS Basic;
    }
}
