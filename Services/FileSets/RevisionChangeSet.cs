using Newtonsoft.Json;
using System;
using UpdateClientService.API.Services.DownloadService;

namespace UpdateClientService.API.Services.FileSets
{
    public class RevisionChangeSet : RevisionChangeSetKey
    {
        public DateTime? Received { get; set; }

        public DateTime? Downloaded { get; set; }

        public DateTime? Staged { get; set; }

        public DateTime? Activated { get; set; }

        public ChangesetState State { get; set; }

        public string Message { get; set; }

        public int RetryCount { get; set; }

        public string Path { get; set; }

        public int CompressionType { get; set; }

        public string ContentHash { get; set; }

        public string FileHash { get; set; }

        public DateTime DownloadOn { get; set; }

        public DownloadPriority DownloadPriority { get; set; }

        public DateTime ActiveOn { get; set; }

        public string ActivateStartTime { get; set; }

        public string ActivateEndTime { get; set; }

        public string DownloadUrl { get; set; }

        public string StateText => Enum.GetName(typeof(ChangesetState), (object)this.State);

        [JsonIgnore]
        public bool IsStaged
        {
            get => this.State != ChangesetState.Error && this.State >= ChangesetState.Staged;
        }

        public static RevisionChangeSet Create(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet = new RevisionChangeSet();
            revisionChangeSet.FileSetId = clientFileSetRevisionChangeSet.FileSetId;
            revisionChangeSet.RevisionId = clientFileSetRevisionChangeSet.RevisionId;
            revisionChangeSet.Received = new DateTime?(DateTime.Now);
            revisionChangeSet.Downloaded = new DateTime?();
            revisionChangeSet.Staged = new DateTime?();
            revisionChangeSet.Activated = new DateTime?();
            revisionChangeSet.State = ChangesetState.Received;
            revisionChangeSet.Message = string.Empty;
            revisionChangeSet.RetryCount = 0;
            revisionChangeSet.Path = clientFileSetRevisionChangeSet.Path;
            revisionChangeSet.CompressionType = clientFileSetRevisionChangeSet.CompressionType;
            revisionChangeSet.ContentHash = clientFileSetRevisionChangeSet.ContentHash;
            revisionChangeSet.FileHash = clientFileSetRevisionChangeSet.FileHash;
            revisionChangeSet.PatchRevisionId = clientFileSetRevisionChangeSet.PatchRevisionId;
            revisionChangeSet.DownloadOn = clientFileSetRevisionChangeSet.DownloadOn.ToLocalTime();
            revisionChangeSet.DownloadPriority = clientFileSetRevisionChangeSet.DownloadPriority;
            revisionChangeSet.ActiveOn = clientFileSetRevisionChangeSet.ActiveOn.ToLocalTime();
            revisionChangeSet.ActivateStartTime = clientFileSetRevisionChangeSet.ActivateStartTime;
            revisionChangeSet.ActivateEndTime = clientFileSetRevisionChangeSet.ActivateEndTime;
            revisionChangeSet.DownloadUrl = clientFileSetRevisionChangeSet.DownloadUrl;
            return revisionChangeSet;
        }
    }
}
