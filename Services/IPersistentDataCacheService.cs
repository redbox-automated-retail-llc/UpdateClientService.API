using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services
{
    public interface IPersistentDataCacheService
    {
        Task<PersistentDataWrapper<T>> Read<T>(
          string fileName,
          bool useCache = false,
          string filePath = null,
          bool log = true)
          where T : IPersistentData;

        Task<List<PersistentDataWrapper<T>>> ReadLike<T>(
          string pattern,
          string directory = null,
          bool useCache = true)
          where T : IPersistentData;

        Task<List<PersistentDataWrapper<T>>> ReadLike<T>(
          Regex pattern,
          string directory = null,
          bool useCache = true)
          where T : IPersistentData;

        Task<bool> Write<T>(T persistentData, string fileName, string baseDirectory = null) where T : IPersistentData;

        Task<bool> Delete(string fileName, string filePath = null);

        Task<bool> DeleteLike(string pattern, string directory = null);
    }
}
