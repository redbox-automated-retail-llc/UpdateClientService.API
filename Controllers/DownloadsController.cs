using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.DownloadService.Responses;
using UpdateClientService.API.Services.IoT.DownloadFiles;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadsController : ControllerBase
    {
        private readonly IDownloadService _downloadService;
        private readonly IDownloadFilesService _downloadFilesService;
        private readonly ILogger<DownloadsController> _logger;
        private readonly IMemoryCache _cache;

        public DownloadsController(
          IDownloadService downloadService,
          IDownloadFilesService downloadFilesService,
          ILogger<DownloadsController> logger,
          IMemoryCache cache)
        {
            this._downloadService = downloadService;
            this._downloadFilesService = downloadFilesService;
            this._logger = logger;
            this._cache = cache;
        }

        [HttpHead("s3/proxy/{key}/{type}")]
        [ProducesResponseType(typeof(RedirectResult), 301)]
        public async Task<ActionResult> ProxyForS3Head(string key, DownloadPathType type)
        {
            key = Uri.UnescapeDataString(key);
            string proxiedS3Url;
            if (!CacheExtensions.TryGetValue<string>(this._cache, string.Format("{0}/{1}/head", key, type), out proxiedS3Url))
            {
                proxiedS3Url = await this._downloadService.GetProxiedS3Url(key, type, true);
                if (!string.IsNullOrWhiteSpace(proxiedS3Url) && Uri.TryCreate(proxiedS3Url, UriKind.Absolute, out Uri _))
                    CacheExtensions.Set<string>(this._cache, string.Format("{0}/{1}/head", key, type), proxiedS3Url, TimeSpan.FromMinutes(30.0));
            }
            return new RedirectResult(proxiedS3Url, true);
        }

        [HttpGet("s3/proxy/{key}/{type}")]
        [ProducesResponseType(typeof(RedirectResult), 301)]
        public async Task<RedirectResult> ProxyForS3Get(string key, DownloadPathType type)
        {
            key = Uri.UnescapeDataString(key);
            string proxiedS3Url;
            if (!CacheExtensions.TryGetValue<string>(this._cache, string.Format("{0}/{1}/get", key, type), out proxiedS3Url))
            {
                proxiedS3Url = await this._downloadService.GetProxiedS3Url(key, type, true);
                if (!string.IsNullOrWhiteSpace(proxiedS3Url) && Uri.TryCreate(proxiedS3Url, UriKind.Absolute, out Uri _))
                    CacheExtensions.Set<string>(this._cache, string.Format("{0}/{1}/get", key, type), proxiedS3Url, TimeSpan.FromMinutes(30.0));
            }
            return new RedirectResult(proxiedS3Url, true);
        }

        [HttpPost]
        [ProducesResponseType(typeof(GetFileResponse), 202)]
        [ProducesResponseType(typeof(GetFileResponse), 500)]
        public async Task<ActionResult<GetFileResponse>> StartDownload(Uri url, string hash = null)
        {
            return (ActionResult<GetFileResponse>)(await this._downloadService.AddDownload(hash, url.ToString(), DownloadPriority.Normal)).ToObjectResult();
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetDownloadStatusesResponse), 200)]
        public async Task<ActionResult> GetDownloadStatuses()
        {
            return (ActionResult)(await this._downloadService.GetDownloadsResponse()).ToObjectResult();
        }

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(GetDownloadStatusResponse), 200)]
        [ProducesResponseType(typeof(GetDownloadStatusResponse), 404)]
        public async Task<ActionResult> GetDownloadStatus(string key)
        {
            return (ActionResult)(await this._downloadService.GetDownloadResponse(key)).ToObjectResult();
        }

        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(DeleteDownloadResponse), 200)]
        [ProducesResponseType(typeof(DeleteDownloadResponse), 500)]
        public async Task<ActionResult> CancelDownload(string key)
        {
            return (ActionResult)(await this._downloadService.CancelDownload(key)).ToObjectResult();
        }

        [HttpPost("{key}")]
        [ProducesResponseType(typeof(CompleteDownloadResponse), 200)]
        [ProducesResponseType(typeof(CompleteDownloadResponse), 500)]
        public async Task<ActionResult<CompleteDownloadResponse>> CompleteDownload(string key)
        {
            return (ActionResult<CompleteDownloadResponse>)(ActionResult)(await this._downloadService.CompleteDownload(key)).ToObjectResult();
        }
    }
}
