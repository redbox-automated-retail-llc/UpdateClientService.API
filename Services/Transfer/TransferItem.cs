namespace UpdateClientService.API.Services.Transfer
{
    public class TransferItem : ITransferItem
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string RemoteURL { get; set; }

        public ulong TotalTransferd { get; set; }

        public ulong TotalBytes { get; set; }

        internal TransferItem(IBackgroundCopyFile file)
        {
            string pVal1;
            file.GetLocalName(out pVal1);
            string pVal2;
            file.GetRemoteName(out pVal2);
            BG_FILE_PROGRESS pVal3;
            file.GetProgress(out pVal3);
            this.Path = System.IO.Path.GetDirectoryName(pVal1);
            this.Name = System.IO.Path.GetFileName(pVal1);
            this.RemoteURL = pVal2;
            this.TotalBytes = pVal3.BytesTotal;
            this.TotalTransferd = pVal3.BytesTransferred;
        }
    }
}
