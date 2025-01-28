namespace UpdateClientService.API.Controllers.Models
{
    public class UnRegisterRequest : BaseRequest
    {
        public long KioskId { get; set; }

        public string Reason { get; set; }
    }
}
