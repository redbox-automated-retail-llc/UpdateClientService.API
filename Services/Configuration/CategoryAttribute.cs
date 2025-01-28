using System;

namespace UpdateClientService.API.Services.Configuration
{
    public class CategoryAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
