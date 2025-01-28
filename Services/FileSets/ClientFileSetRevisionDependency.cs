namespace UpdateClientService.API.Services.FileSets
{
    public class ClientFileSetRevisionDependency
    {
        public long FileSetRevisionDependencyId { get; set; }

        public long DependsOnFileSetId { get; set; }

        public DependencyType DependencyType { get; set; }

        public string MinimumVersion { get; set; }

        public string MaximumVersion { get; set; }
    }
}
