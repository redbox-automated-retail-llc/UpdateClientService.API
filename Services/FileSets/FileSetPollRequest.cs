namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetPollRequest
    {
        public long FileSetId { get; set; }

        public long FileSetRevisionId { get; set; }

        public FileSetState FileSetState { get; set; }
    }
}
