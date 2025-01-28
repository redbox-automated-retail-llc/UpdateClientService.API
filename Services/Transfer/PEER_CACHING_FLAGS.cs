using System;

namespace UpdateClientService.API.Services.Transfer
{
    [Flags]
    internal enum PEER_CACHING_FLAGS : uint
    {
        BG_JOB_ENABLE_PEERCACHING_CLIENT = 1,
        BG_JOB_ENABLE_PEERCACHING_SERVER = 2,
    }
}
