using System.Collections.Generic;

namespace UpdateClientService.API.Services.Transfer
{
    public class TransferCallbackParameters : ITransferCallbackParameters
    {
        public string Path { get; set; }

        public string Executable { get; set; }

        public List<string> Arguments { get; set; }
    }
}
