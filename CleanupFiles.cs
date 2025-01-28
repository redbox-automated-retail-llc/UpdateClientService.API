namespace UpdateClientService.API
{
    public class CleanupFiles
    {
        public string Path { get; set; }

        public int MaxAge { get; set; } = 90;
    }
}
