using Microsoft.AspNetCore.Mvc;

namespace UpdateClientService.API.Services.KioskEngine
{
    public static class KioskEngineStatusExtensions
    {
        public static ObjectResult ToObjectResult(this KioskEngineStatus status)
        {
            ObjectResult objectResult = new ObjectResult((object)status);
            switch (status)
            {
                case KioskEngineStatus.Running:
                    objectResult.StatusCode = new int?(200);
                    break;
                case KioskEngineStatus.Stopped:
                    objectResult.StatusCode = new int?(503);
                    break;
                default:
                    objectResult.StatusCode = new int?(500);
                    break;
            }
            return objectResult;
        }
    }
}
