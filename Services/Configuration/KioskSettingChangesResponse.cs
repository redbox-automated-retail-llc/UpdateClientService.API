using Newtonsoft.Json;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.Configuration
{
    public class KioskSettingChangesResponse
    {
        public int? PageNumber { get; set; }

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

        public int? PageCount { get; set; }

        public bool VersionIsCurrent { get; set; }

        public string ConfigurationVersionHash { get; set; }

        public IEnumerable<long> RemovedConfigurationSettingValueIds { get; set; }

        public bool? RemoveAllExistingConfigurationSettingValues { get; set; }

        public long? OriginalConfigurationVersionId { get; set; }

        public GetKioskConfigurationSettingValues NewConfigurationSettingValues { get; set; }
    }
}
