using System;
using System.IO;

namespace UpdateClientService.API.Services.FileSets
{
    public class Constants
    {
        public static string FileSetsRoot
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create), "Redbox\\UpdateClient\\.store\\.filesets"));
            }
        }

        public static string FileCacheRootPath
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create), "Redbox\\UpdateClient\\.store\\.filecache"));
            }
        }
    }
}
