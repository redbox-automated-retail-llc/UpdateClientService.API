using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.IO;

namespace UpdateClientService.API.Services
{
    public class StoreService : IStoreService
    {
        private ILogger<StoreService> _logger;
        private long _kioskId;
        private string _market;
        private string _banner;
        public string _dataPath;

        public StoreService(ILogger<StoreService> logger) => this._logger = logger;

        public long KioskId
        {
            get
            {
                try
                {
                    string str1 = "ID";
                    string str2 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Redbox\\REDS\\Kiosk Engine\\Store";
                    string str3 = Registry.GetValue(str2, str1, (object)"")?.ToString();
                    if (string.IsNullOrEmpty(str3))
                    {
                        this._logger.LogInformation("Kiosk ID is not set in the registry at {0}\\{1}", new object[2]
                        {
              (object) str2,
              (object) str1
                        });
                        return 0;
                    }
                    this._kioskId = (long)Convert.ToInt32(str3);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Unhandled exception occurred in StoreManagerService.GetKioskId", Array.Empty<object>());
                }
                return this._kioskId;
            }
        }

        public string Market
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(this._market))
                    {
                        string str1 = nameof(Market);
                        string str2 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Redbox\\REDS\\Kiosk Engine\\Store";
                        object obj = Registry.GetValue(str2, str1, (object)null);
                        if (obj == null)
                        {
                            this._logger.LogInformation("Market is not set in the registry at {0}\\{1}", new object[2]
                            {
                (object) str2,
                (object) str1
                            });
                            return (string)null;
                        }
                        this._market = ((string)obj).Trim();
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Unhandled exception occurred in StoreManagerService.GetMarket", Array.Empty<object>());
                }
                return this._market;
            }
        }

        public string Banner
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(this._banner))
                    {
                        string str1 = nameof(Banner);
                        string str2 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Redbox\\REDS\\Kiosk Engine\\Store";
                        object obj = Registry.GetValue(str2, str1, (object)null);
                        if (obj == null)
                        {
                            this._logger.LogInformation("Banner is not set in the registry at {0}\\{1}", new object[2]
                            {
                (object) str2,
                (object) str1
                            });
                            return (string)null;
                        }
                        this._banner = ((string)obj).Trim();
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Unhandled exception occurred in StoreManagerService.GetMarket", Array.Empty<object>());
                }
                return this._banner;
            }
        }

        public string DataPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this._dataPath))
                    return this._dataPath;
                this._dataPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create), "Redbox\\UpdateClient"));
                return this._dataPath;
            }
        }

        public string RunningPath => Path.GetDirectoryName(typeof(StoreService).Assembly.Location);
    }
}
