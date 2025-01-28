namespace UpdateClientService.API.Services.Kernel
{
    public static class KernelPowershellScripts
    {
        public const string Reboot = "Restart-Computer -Force";
        public const string Shutdown = "Stop-Computer -Force";
    }
}
