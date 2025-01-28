namespace UpdateClientService.API.Services.Transfer
{
    public enum EoAuthnCap
    {
        None = 0,
        MutualAuth = 1,
        SecureRefs = 2,
        AccessControl = 4,
        AppID = 8,
        Dynamic = 16, // 0x00000010
        StaticCloaking = 32, // 0x00000020
        DynamicCloaking = 64, // 0x00000040
        AnyAuthority = 128, // 0x00000080
        MakeFullSIC = 256, // 0x00000100
        RequireFullSIC = 512, // 0x00000200
        AutoImpersonate = 1024, // 0x00000400
        Default = 2048, // 0x00000800
        DisableAAA = 4096, // 0x00001000
        NoCustomMarshal = 8192, // 0x00002000
    }
}
