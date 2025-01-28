using System.Collections.Generic;

namespace UpdateClientService.API.Controllers.Models
{
    public class ValidationError
    {
        public IEnumerable<string> Errors { get; set; }
    }
}
