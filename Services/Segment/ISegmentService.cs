using Redbox.NetCore.Middleware.Http;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Segment
{
    public interface ISegmentService
    {
        Task<ApiBaseResponse> UpdateKioskSegmentsFromUpdateService();

        Task<KioskSegmentsResponse> GetKioskSegments();

        Task<bool> UpdateKioskSegmentsIfNeeded();
    }
}
