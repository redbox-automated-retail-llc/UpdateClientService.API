namespace UpdateClientService.API.Services.Transfer
{
    public enum TransferStatus : byte
    {
        Queued,
        Connecting,
        Transfering,
        Suspended,
        Error,
        TransientError,
        Transferred,
        Acknowledged,
        Cancelled,
    }
}
