using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;

        public ConfigurationController(IConfigurationService configurationService)
        {
            this._configurationService = configurationService;
        }

        [HttpGet("settingValues/changes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKioskConfigurationSettingChanges(
          long? requestedConfigurationVersionId)
        {
            return (IActionResult)new StatusCodeResult((int)(await this._configurationService.GetKioskConfigurationSettingChanges(requestedConfigurationVersionId)).StatusCode);
        }

        [HttpGet("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetConfigurationStatus()
        {
            return (IActionResult)(await this._configurationService.GetConfigurationStatus()).ToObjectResult();
        }

        [HttpPost("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateConfigurationStatus()
        {
            return (IActionResult)(await this._configurationService.UpdateConfigurationStatus()).ToObjectResult();
        }
    }
}
