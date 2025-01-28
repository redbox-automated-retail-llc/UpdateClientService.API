namespace UpdateClientService.API.Services.Configuration
{
    public class Operations : BaseCategorySetting
    {
        public bool SyncTimestamp { get; set; }

        public bool SyncTimezone { get; set; }

        public bool SegmentUpdateEnabled { get; set; }

        public int SegmentUpdateFrequencyHours { get; set; } = 26;
    }
}
