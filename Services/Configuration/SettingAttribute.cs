using System;

namespace UpdateClientService.API.Services.Configuration
{
    public class SettingAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
