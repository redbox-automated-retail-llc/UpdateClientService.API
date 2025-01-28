namespace UpdateClientService.API.Services.Transfer
{
    public enum RpcAuthnLevel
    {
        Default,
        None,
        Connect,
        Call,
        Pkt,
        PktIntegrity,
        PktPrivacy,
    }
}
