namespace UpdateClientService.API.Services.FileSets
{
    public class RevisionChangeSetKey
    {
        public long FileSetId { get; set; }

        public long RevisionId { get; set; }

        public long PatchRevisionId { get; set; }
    }
}
