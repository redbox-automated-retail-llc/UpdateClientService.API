using System;
using System.Collections.Generic;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;

namespace UpdateClientService.API.Services.DataUpdate
{
    public class DataUpdateRequest
    {
        public Guid RequestId { get; set; }

        public UpdateDataTable TableName { get; set; }

        public long KioskId { get; set; }

        public DataUpdateRequestType RequestType { get; set; }

        public List<RecordRequest> RecordRequests { get; set; }

        public string Query { get; set; }

        public int? PageNumber { get; set; }

        public FileTypeEnum? FileType { get; set; }
    }
}
