namespace UpdateClientService.API.Services.FileSets
{
    public class StateFile
    {
        private long _fileSetId;
        private long _activeRevisionId;
        private long _inProgressRevisionId;
        private FileSetState _inProgressFileSetState;

        public StateFile(
          long fileSetId,
          long activeRevisionId,
          long inProgressRevisionId,
          FileSetState inProgressFileSetState)
        {
            this._fileSetId = fileSetId;
            this._activeRevisionId = activeRevisionId;
            this._inProgressRevisionId = inProgressRevisionId;
            this._inProgressFileSetState = inProgressFileSetState;
        }

        public long FileSetId => this._fileSetId;

        public long ActiveRevisionId => this._activeRevisionId;

        public long InProgressRevisionId
        {
            get => this._inProgressRevisionId;
            set => this._inProgressRevisionId = value;
        }

        public long CurrentRevisionId
        {
            get
            {
                return !this.IsRevisionDownloadInProgress ? this._activeRevisionId : this._inProgressRevisionId;
            }
        }

        public FileSetState InProgressFileSetState
        {
            get => this._inProgressFileSetState;
            set
            {
                this._inProgressFileSetState = value;
                if (this._inProgressFileSetState != FileSetState.Active)
                    return;
                this._activeRevisionId = this._inProgressRevisionId;
                this._inProgressRevisionId = 0L;
            }
        }

        public FileSetState CurrentFileSetState
        {
            get
            {
                return !this.IsRevisionDownloadInProgress ? FileSetState.Active : this._inProgressFileSetState;
            }
        }

        public bool IsRevisionDownloadInProgress => this._inProgressRevisionId > 0L;

        public bool HasActiveRevision => this._activeRevisionId > 0L;
    }
}
