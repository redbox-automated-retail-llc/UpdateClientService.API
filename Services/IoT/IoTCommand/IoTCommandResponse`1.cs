namespace UpdateClientService.API.Services.IoT.IoTCommand
{
    public class IoTCommandResponse<T>
    {
        public int StatusCode { get; set; }

        public T Payload { get; set; }
    }
}
