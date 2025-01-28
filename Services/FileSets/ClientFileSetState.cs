namespace UpdateClientService.API.Services.FileSets
{
    public class ClientFileSetState
    {
        public long FileSetId { get; set; }

        public long RevisionId { get; set; }

        public FileSetState FileSetState { get; set; }
    }
}
