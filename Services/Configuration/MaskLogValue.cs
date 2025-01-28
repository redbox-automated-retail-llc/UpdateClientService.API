using System;

namespace UpdateClientService.API.Services.Configuration
{
    public class MaskLogValue : Attribute
    {
        public int VisibleChars { get; set; }
    }
}
