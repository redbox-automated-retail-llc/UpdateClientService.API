namespace UpdateClientService.API.Services.IoT
{
    public class MqttResponse<T>
    {
        public T Data { get; set; }

        public string Error { get; set; }
    }
}
