using System.Collections.Generic;

namespace UpdateClientService.API.Services.Transfer
{
    public interface ITransferCallbackParameters
    {
        string Path { get; set; }

        string Executable { get; set; }

        List<string> Arguments { get; set; }
    }
}
