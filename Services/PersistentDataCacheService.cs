using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services
{
    public class PersistentDataCacheService : IPersistentDataCacheService
    {
        private readonly ILogger<PersistentDataCacheService> _logger;
        private readonly IStoreService _storeService;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private const int _lockWait = 2000;
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private string _dataCachePath;

        public PersistentDataCacheService(
          ILogger<PersistentDataCacheService> logger,
          IStoreService storeService)
        {
            this._logger = logger;
            this._storeService = storeService;
        }

        public async Task<PersistentDataWrapper<T>> Read<T>(
          string fileName,
          bool useCache = false,
          string baseDirectory = null,
          bool log = true)
          where T : IPersistentData
        {
            PersistentDataWrapper<T> result = new PersistentDataWrapper<T>();
            try
            {
                if (await PersistentDataCacheService._lock.WaitAsync(2000))
                {
                    try
                    {
                        result = await this.InnerRead<T>(fileName, useCache, baseDirectory, log);
                    }
                    finally
                    {
                        PersistentDataCacheService._lock.Release();
                    }
                }
                else
                    this._logger.LogErrorWithSource("Unable to get lock for reading filename " + fileName + " with baseDirectory " + baseDirectory + ".", nameof(Read), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return result;
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "Exception while reading filename: " + fileName + " with baseDirectory: " + baseDirectory, nameof(Read), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return result;
            }
        }

        private async Task<PersistentDataWrapper<T>> InnerRead<T>(
          string fileName,
          bool useCache = false,
          string baseDirectory = null,
          bool log = true)
          where T : IPersistentData
        {
            PersistentDataWrapper<T> result = new PersistentDataWrapper<T>();
            try
            {
                if (log)
                {
                    ILogger<PersistentDataCacheService> logger = this._logger;
                    if (logger != null)
                        this._logger.LogInfoWithSource(string.Format("Read({0},useCache={1})", (object)fileName, (object)useCache), nameof(InnerRead), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                }
                object obj;
                if (useCache && this._cache.TryGetValue(fileName, out obj) && obj != null)
                {
                    if (log)
                    {
                        ILogger<PersistentDataCacheService> logger = this._logger;
                        if (logger != null)
                            this._logger.LogInformation("Found cached value", Array.Empty<object>());
                    }
                    return obj as PersistentDataWrapper<T>;
                }
                string path = baseDirectory == null ? this.GetDataFilePath(fileName) : Path.Combine(baseDirectory, fileName);
                if (File.Exists(path))
                {
                    using (FileStream file = File.Open(path, (FileMode)3, (FileAccess)1, (FileShare)3))
                    {
                        using (StreamReader reader = new StreamReader((Stream)file))
                            result = JsonConvert.DeserializeObject<PersistentDataWrapper<T>>(await ((TextReader)reader).ReadToEndAsync());
                    }
                }
                if (useCache)
                    this._cache[fileName] = (object)result;
                return result;
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "Exception while reading filename: " + fileName + " with baseDirectory: " + baseDirectory, nameof(InnerRead), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return result;
            }
        }

        public async Task<List<PersistentDataWrapper<T>>> ReadLike<T>(
          string pattern,
          string directory = null,
          bool useCache = true)
          where T : IPersistentData
        {
            List<PersistentDataWrapper<T>> result = new List<PersistentDataWrapper<T>>();
            try
            {
                directory = directory ?? this.DataCachePath;
                if (Directory.Exists(directory))
                {
                    if (await PersistentDataCacheService._lock.WaitAsync(2000))
                    {
                        try
                        {
                            string[] strArray = Directory.GetFiles(directory, "*" + pattern + "*");
                            for (int index = 0; index < strArray.Length; ++index)
                            {
                                string path = strArray[index];
                                List<PersistentDataWrapper<T>> persistentDataWrapperList = result;
                                persistentDataWrapperList.Add(await this.InnerRead<T>(Path.GetFileName(path), useCache, Path.GetDirectoryName(path), false));
                                persistentDataWrapperList = (List<PersistentDataWrapper<T>>)null;
                            }
                            strArray = (string[])null;
                        }
                        finally
                        {
                            PersistentDataCacheService._lock.Release();
                        }
                    }
                    else
                        this._logger.LogErrorWithSource("Unable to get lock for reading with pattern " + pattern + " in directory " + directory + ".", nameof(ReadLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                }
                return result;
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "unhandled exception occurred calling ReadLike(" + pattern + " in directory: " + directory + ")", nameof(ReadLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return result;
            }
        }

        public async Task<List<PersistentDataWrapper<T>>> ReadLike<T>(
          Regex pattern,
          string directory = null,
          bool useCache = true)
          where T : IPersistentData
        {
            List<PersistentDataWrapper<T>> result = new List<PersistentDataWrapper<T>>();
            try
            {
                directory = directory ?? this.DataCachePath;
                if (Directory.Exists(directory))
                {
                    if (await PersistentDataCacheService._lock.WaitAsync(2000))
                    {
                        try
                        {
                            string[] strArray = ((IEnumerable<string>)Directory.GetFiles(directory, "*.*")).Where<string>((Func<string, bool>)(f => pattern.IsMatch(Path.GetFileName(f)))).ToArray<string>();
                            for (int index = 0; index < strArray.Length; ++index)
                            {
                                string path = strArray[index];
                                List<PersistentDataWrapper<T>> persistentDataWrapperList = result;
                                persistentDataWrapperList.Add(await this.InnerRead<T>(Path.GetFileName(path), useCache, Path.GetDirectoryName(path), false));
                                persistentDataWrapperList = (List<PersistentDataWrapper<T>>)null;
                            }
                            strArray = (string[])null;
                        }
                        finally
                        {
                            PersistentDataCacheService._lock.Release();
                        }
                    }
                    else
                        this._logger.LogErrorWithSource(string.Format("Unable to get lock for reading with regex pattern {0} in directory {1}.", (object)pattern, (object)directory), nameof(ReadLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                }
                return result;
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, string.Format("unhandled exception occurred calling ReadLike({0} in directory {1})", (object)pattern, (object)directory), nameof(ReadLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return result;
            }
        }

        public async Task<bool> Write<T>(T data, string fileName, string baseDirectory = null) where T : IPersistentData
        {
            try
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogInfoWithSource("Write(" + fileName + ")", nameof(Write), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                string json = JsonConvert.SerializeObject((object)new PersistentDataWrapper<T>()
                {
                    Data = data,
                    Modified = DateTime.Now,
                    DataType = typeof(T).FullName
                }, (Formatting)1, new JsonSerializerSettings()
                {
                    NullValueHandling = (NullValueHandling)1
                });
                string filePath = baseDirectory == null ? this.GetDataFilePath(fileName) : Path.Combine(baseDirectory, fileName);
                if (baseDirectory != null)
                    Directory.CreateDirectory(baseDirectory);
                if (await PersistentDataCacheService._lock.WaitAsync(2000))
                {
                    try
                    {
                        File.WriteAllText(filePath, json);
                        this.ClearCache(fileName);
                    }
                    finally
                    {
                        PersistentDataCacheService._lock.Release();
                    }
                    return true;
                }
                this._logger.LogErrorWithSource("Unable to get lock for writing to fileName " + fileName + " with baseDiretory " + baseDirectory + ".", nameof(Write), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return false;
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "Exception while writing to filename: " + fileName + " with baseDirectory " + baseDirectory, nameof(Write), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return false;
            }
        }

        public async Task<bool> Delete(string fileName, string baseDirectory = null)
        {
            try
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogInfoWithSource("Delete(" + fileName + ")", nameof(Delete), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                string filePath = baseDirectory == null ? this.GetDataFilePath(fileName) : Path.Combine(baseDirectory, fileName);
                if (await PersistentDataCacheService._lock.WaitAsync(2000))
                {
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        this.ClearCache(fileName);
                        return true;
                    }
                    finally
                    {
                        PersistentDataCacheService._lock.Release();
                    }
                }
                else
                {
                    this._logger.LogErrorWithSource("Unable to get lock for deleting to fileName " + fileName + " with baseDiretory " + baseDirectory + ".", nameof(Delete), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "Exception while deleting filename " + fileName + " with baseDirectory " + baseDirectory, nameof(Delete), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return false;
            }
        }

        public async Task<bool> DeleteLike(string pattern, string directory = null)
        {
            try
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogInfoWithSource("DeleteLike(" + pattern + ")", nameof(DeleteLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                directory = directory ?? this.DataCachePath;
                if (await PersistentDataCacheService._lock.WaitAsync(2000))
                {
                    try
                    {
                        if (Directory.Exists(directory))
                        {
                            foreach (string file in Directory.GetFiles(directory, "*" + pattern + "*"))
                                File.Delete(file);
                        }
                        return true;
                    }
                    finally
                    {
                        PersistentDataCacheService._lock.Release();
                    }
                }
                else
                {
                    this._logger.LogErrorWithSource("Unable to get lock for deleting files with pattern " + pattern + " in diretory " + directory + ".", nameof(DeleteLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ILogger<PersistentDataCacheService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource(ex, "Exception while deleting with pattern: " + pattern + " and directory: " + directory, nameof(DeleteLike), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
                return false;
            }
        }

        private void ClearCache(string fileName)
        {
            if (!this._cache.ContainsKey(fileName))
                return;
            ILogger<PersistentDataCacheService> logger = this._logger;
            if (logger != null)
                this._logger.LogInfoWithSource("Clearing cached value " + fileName, nameof(ClearCache), "/sln/src/UpdateClientService.API/Services/PersistentDataCacheService.cs");
            this._cache.TryRemove(fileName, out object _);
        }

        private string GetDataFilePath(string fileName)
        {
            return Path.Combine(this.DataCachePath, Path.ChangeExtension(fileName, "json"));
        }

        private string DataCachePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this._dataCachePath))
                {
                    this._dataCachePath = Path.GetFullPath(Path.Combine(this._storeService.DataPath, "DataCache"));
                    Directory.CreateDirectory(this._dataCachePath);
                }
                return this._dataCachePath;
            }
        }
    }
}
