using System;
using System.Collections.Generic;
using System.Linq;

namespace UpdateClientService.API.Services.FileSets
{
    public class ClientFileSetRevision : RevisionChangeSetKey
    {
        public string FileSetName { get; set; }

        public int RetentionDays { get; set; }

        public int RetentionRevisions { get; set; }

        public string RevisionName { get; set; }

        public string RevisionVersion { get; set; }

        public string SetPath { get; set; }

        public int SetCompressionType { get; set; }

        public string SetFileHash { get; set; }

        public long SetFileSize { get; set; }

        public string PatchSetPath { get; set; }

        public int PatchSetCompressionType { get; set; }

        public string PatchSetFileHash { get; set; }

        public long PatchSetFileSize { get; set; }

        public IEnumerable<ClientFileSetFile> Files { get; set; }

        public IEnumerable<ClientPatchFileSetFile> PatchFiles { get; set; }

        public virtual IEnumerable<ClientFileSetRevisionDependency> Dependencies { get; set; }

        public ClientFileSetRevision Clone()
        {
            ClientFileSetRevision clientFileSetRevision1 = new ClientFileSetRevision();
            clientFileSetRevision1.FileSetId = this.FileSetId;
            clientFileSetRevision1.FileSetName = this.FileSetName;
            clientFileSetRevision1.RetentionDays = this.RetentionDays;
            clientFileSetRevision1.RetentionRevisions = this.RetentionRevisions;
            clientFileSetRevision1.RevisionId = this.RevisionId;
            clientFileSetRevision1.RevisionName = this.RevisionName;
            clientFileSetRevision1.RevisionVersion = this.RevisionVersion;
            clientFileSetRevision1.PatchRevisionId = this.PatchRevisionId;
            clientFileSetRevision1.SetPath = this.SetPath;
            clientFileSetRevision1.SetCompressionType = this.SetCompressionType;
            clientFileSetRevision1.SetFileHash = this.SetFileHash;
            clientFileSetRevision1.SetFileSize = this.SetFileSize;
            clientFileSetRevision1.PatchSetPath = this.PatchSetPath;
            clientFileSetRevision1.PatchSetCompressionType = this.PatchSetCompressionType;
            clientFileSetRevision1.PatchSetFileHash = this.PatchSetFileHash;
            clientFileSetRevision1.PatchSetFileSize = this.PatchSetFileSize;
            ClientFileSetRevision clientFileSetRevision2 = clientFileSetRevision1;
            List<ClientFileSetFile> files = new List<ClientFileSetFile>();
            if (this.Files != null)
                this.Files.ToList<ClientFileSetFile>().ForEach((Action<ClientFileSetFile>)(item => files.Add(item)));
            clientFileSetRevision2.Files = (IEnumerable<ClientFileSetFile>)files;
            List<ClientPatchFileSetFile> patchFiles = new List<ClientPatchFileSetFile>();
            if (this.PatchFiles != null)
                this.PatchFiles.ToList<ClientPatchFileSetFile>().ForEach((Action<ClientPatchFileSetFile>)(item => patchFiles.Add(item)));
            clientFileSetRevision2.PatchFiles = (IEnumerable<ClientPatchFileSetFile>)patchFiles;
            List<ClientFileSetRevisionDependency> dependencies = new List<ClientFileSetRevisionDependency>();
            if (this.Dependencies != null)
                this.Dependencies.ToList<ClientFileSetRevisionDependency>().ForEach((Action<ClientFileSetRevisionDependency>)(item => dependencies.Add(item)));
            clientFileSetRevision2.Dependencies = (IEnumerable<ClientFileSetRevisionDependency>)dependencies;
            return clientFileSetRevision2;
        }
    }
}
