using System;

namespace UpdateClientService.API.Services
{
    public class PersistentDataWrapper<T> where T : IPersistentData
    {
        public string DataType { get; set; }

        public DateTime Modified { get; set; }

        public T Data { get; set; }
    }
}
