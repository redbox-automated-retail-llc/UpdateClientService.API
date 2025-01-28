using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal struct BG_AUTH_CREDENTIALS
    {
        internal BG_AUTH_TARGET Target;
        internal BG_AUTH_SCHEME Scheme;
        internal BG_AUTH_CREDENTIALS_UNION Credentials;
    }
}
