using System;

namespace UpdateClientService.API.Services.Configuration
{
    public class KioskSettingValue
    {
        public long ConfigurationSettingValueId { get; set; }

        public ConfigurationEncryptionType? EncryptionType { get; set; }

        public string Value { get; set; }

        public long SegmentId { get; set; }

        public string SegmentName { get; set; }

        public string SegmentationName { get; set; }

        public long Rank { get; set; }

        public DateTime EffectiveDateTime { get; set; }

        public DateTime? ExpireDateTime { get; set; }
    }
}
