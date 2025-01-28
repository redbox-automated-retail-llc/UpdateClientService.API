using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Utilities;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PowerShellController
    {
        private readonly ICommandLineService _cmd;

        public PowerShellController(ICommandLineService cmd) => this._cmd = cmd;

        [HttpPost("file")]
        [ProducesResponseType(typeof(OkResult), 200)]
        [ProducesResponseType(typeof(StatusCodeResult), 500)]
        public async Task<ActionResult> ExecuteFile(IFormFile file)
        {
            string temp = Path.GetTempFileName().Replace(".tmp", ".ps1");
            using (FileStream s = File.Create(temp))
                await file.CopyToAsync((Stream)s, new CancellationToken());
            bool flag = this._cmd.TryExecutePowerShellScriptFromFile(temp);
            File.Delete(temp);
            return flag ? (ActionResult)new OkResult() : (ActionResult)new StatusCodeResult(500);
        }

        [HttpPost("script")]
        [ProducesResponseType(typeof(OkResult), 200)]
        [ProducesResponseType(typeof(StatusCodeResult), 500)]
        public ActionResult ExecuteScript(string script)
        {
            return !this._cmd.TryExecutePowerShellScript(script) ? (ActionResult)new StatusCodeResult(500) : (ActionResult)new OkResult();
        }
    }
}
