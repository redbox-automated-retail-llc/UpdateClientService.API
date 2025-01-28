namespace UpdateClientService.API.Services.Segment
{
    public class KioskSegmentModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string SegmentationName { get; set; }

        public int Rank { get; set; }
    }
}
