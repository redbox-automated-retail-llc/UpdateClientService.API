using System;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetDependencyState
    {
        public long FileSetId { get; set; }

        public long ActiveRevisionId { get; set; }

        public string ActiveVersion { get; set; }

        public long InProgressRevisionId { get; set; }

        public string InProgressVersion { get; set; }

        public bool IsInProgressStaged { get; set; }

        public bool NeedsDependency { get; set; }

        public bool IsDependencyMet(
          DependencyType dependencyType,
          string minVersion,
          string maxVersion)
        {
            string sourceVersion;
            if (this.InProgressRevisionId > 0L)
            {
                if (!this.IsInProgressStaged)
                    return false;
                sourceVersion = this.InProgressVersion;
            }
            else
            {
                if (this.ActiveRevisionId <= 0L)
                    return false;
                sourceVersion = this.ActiveVersion;
            }
            switch (dependencyType)
            {
                case DependencyType.Version:
                    return this.IsVersionDependencyMet(sourceVersion, minVersion, maxVersion);
                case DependencyType.String:
                    return this.IsStringDependencyMet(sourceVersion, minVersion, maxVersion);
                case DependencyType.Long:
                    return this.IsLongDependencyMet(sourceVersion, minVersion, maxVersion);
                default:
                    return false;
            }
        }

        private bool IsVersionDependencyMet(string sourceVersion, string minVersion, string maxVersion)
        {
            Version version1 = new Version(sourceVersion);
            Version version2 = new Version(minVersion);
            if (version1 < version2)
                return false;
            if (string.IsNullOrEmpty(maxVersion))
                return true;
            Version version3 = new Version(maxVersion);
            return !(version1 > version3);
        }

        private bool IsStringDependencyMet(string sourceVersion, string minVersion, string maxVersion)
        {
            string str = sourceVersion.Trim();
            string strB1 = minVersion.Trim();
            if (str.CompareTo(strB1) < 0)
                return false;
            if (string.IsNullOrEmpty(maxVersion))
                return true;
            string strB2 = maxVersion.Trim();
            return str.CompareTo(strB2) <= 0;
        }

        private bool IsLongDependencyMet(string sourceVersion, string minVersion, string maxVersion)
        {
            long int64_1 = Convert.ToInt64(sourceVersion);
            long int64_2 = Convert.ToInt64(minVersion);
            if (int64_1 < int64_2)
                return false;
            if (string.IsNullOrEmpty(maxVersion))
                return true;
            long int64_3 = Convert.ToInt64(maxVersion);
            return int64_1 <= int64_3;
        }
    }
}
