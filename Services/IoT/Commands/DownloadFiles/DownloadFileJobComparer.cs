using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public class DownloadFileJobComparer : IEqualityComparer<DownloadFileJob>
    {
        public bool Equals(DownloadFileJob first, DownloadFileJob second)
        {
            return first?.DownloadFileJobId == second?.DownloadFileJobId;
        }

        public int GetHashCode(DownloadFileJob key)
        {
            long num = 17L * 23L;
            int? hashCode = key?.DownloadFileJobId.ToUpper().GetHashCode();
            long? nullable = hashCode.HasValue ? new long?((long)hashCode.GetValueOrDefault()) : new long?();
            return (int)((nullable.HasValue ? new long?(num + nullable.GetValueOrDefault()) : new long?()) ?? (long)0.GetHashCode());
        }
    }
}
