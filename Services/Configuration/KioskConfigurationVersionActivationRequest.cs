using System;
using System.ComponentModel.DataAnnotations;

namespace UpdateClientService.API.Services.Configuration
{
    public class KioskConfigurationVersionActivationRequest
    {
        [Required]
        public long KioskId { get; set; }

        [Required]
        public long ConfigurationVersionId { get; set; }

        [Required]
        public DateTime ActivationDateTimeUtc { get; set; }

        [Required]
        public string ModifiedBy { get; set; }
    }
}
