namespace UpdateClientService.API.Services.Configuration
{
    public class TriggerGetConfigChangesRequest
    {
        public long? ExecutionTimeFrameMs { get; set; }

        public long? RequestedConfigurationVersionId { get; set; }
    }
}
