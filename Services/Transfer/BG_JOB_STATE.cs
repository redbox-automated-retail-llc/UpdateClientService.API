namespace UpdateClientService.API.Services.Transfer
{
    internal enum BG_JOB_STATE
    {
        BG_JOB_STATE_QUEUED = 0,
        BG_JOB_STATE_CONNECTING = 1,
        BG_JOB_STATE_TRANSFERRING = 2,
        BG_JOB_STATE_SUSPENDED = 3,
        BG_JOB_STATE_ERROR = 4,
        BG_JOB_STATE_TRANSIENT_ERROR = 5,
        BG_JOB_STATE_TRANSFERRED = 6,
        BG_JOB_STATE_ACKNOWLEDGED = 7,
        BG_JOB_STATE_CANCELLED = 8,
        BG_JOB_STATE_UNKNOWN = 1001, // 0x000003E9
    }
}
