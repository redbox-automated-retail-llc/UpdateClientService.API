// This was added because we are using .NET Framework 4.7
using System.IO;
using System;
using System.Text;

namespace UpdateClientService.API.App
{
    public static class PathHelpers
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
            string[] relativeToSegments = relativeTo.Split(Path.DirectorySeparatorChar);
            string[] pathSegments = path.Split(Path.DirectorySeparatorChar);

            int commonPrefixLength = 0;
            int minLength = Math.Min(relativeToSegments.Length, pathSegments.Length);

            while (commonPrefixLength < minLength &&
                   string.Equals(relativeToSegments[commonPrefixLength], pathSegments[commonPrefixLength], StringComparison.OrdinalIgnoreCase))
            {
                commonPrefixLength++;
            }

            if (commonPrefixLength == 0)
            {
                return path;
            }

            var relativePath = new StringBuilder();

            for (int i = commonPrefixLength; i < relativeToSegments.Length; i++)
            {
                if (relativePath.Length > 0)
                {
                    relativePath.Append(Path.DirectorySeparatorChar);
                }
                relativePath.Append("..");
            }

            for (int i = commonPrefixLength; i < pathSegments.Length; i++)
            {
                if (relativePath.Length > 0)
                {
                    relativePath.Append(Path.DirectorySeparatorChar);
                }
                relativePath.Append(pathSegments[i]);
            }

            return relativePath.ToString();
        }
    }
}
