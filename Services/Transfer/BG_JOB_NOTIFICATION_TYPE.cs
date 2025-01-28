using System;

namespace UpdateClientService.API.Services.Transfer
{
    [Flags]
    internal enum BG_JOB_NOTIFICATION_TYPE : uint
    {
        BG_NOTIFY_JOB_TRANSFERRED = 1,
        BG_NOTIFY_JOB_ERROR = 2,
        BG_NOTIFY_DISABLE = 4,
        BG_NOTIFY_JOB_MODIFICATION = 8,
    }
}
