using Newtonsoft.Json;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.DataUpdate
{
    public class DataUpdateResponse : ApiBaseResponse
    {
        public Guid RequestId { get; set; }

        public UpdateDataTable TableName { get; set; }

        public List<RecordResponse> RecordResponses { get; set; } = new List<RecordResponse>();

        public int? PageNumber { get; set; }

        public int? PageCount { get; set; }

        [JsonIgnore]
        public bool IsFirstPage
        {
            get
            {
                if (!this.PageNumber.HasValue)
                    return true;
                int? pageNumber = this.PageNumber;
                int num = 1;
                return pageNumber.GetValueOrDefault() == num & pageNumber.HasValue;
            }
        }

        [JsonIgnore]
        public bool IsLastPage
        {
            get
            {
                if (!this.PageNumber.HasValue)
                    return true;
                if (!this.PageNumber.HasValue)
                    return false;
                int? pageNumber = this.PageNumber;
                int? pageCount = this.PageCount;
                return pageNumber.GetValueOrDefault() == pageCount.GetValueOrDefault() & pageNumber.HasValue == pageCount.HasValue;
            }
        }
    }
}
