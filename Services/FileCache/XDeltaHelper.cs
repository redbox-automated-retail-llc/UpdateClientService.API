using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UpdateClientService.API.App;

namespace UpdateClientService.API.Services.FileCache
{
    public static class XDeltaHelper
    {
        private static readonly string _path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Environment.Is64BitOperatingSystem ? "etc\\xdelta3-64.exe" : "etc\\xdelta3.exe");

        private static string FormatApplyArguments(string source, string target, string patch)
        {
            return string.Format("-f -d -s \"{0}\" \"{1}\" \"{2}\"", (object)source, (object)patch, (object)target);
        }

        private static string FormatCreateArguments(string source, string target, string patch)
        {
            return string.Format("-f -e -s \"{0}\" \"{1}\" \"{2}\"", (object)source, (object)target, (object)patch);
        }

        public static List<Error> Apply(string source, string patch, string target)
        {
            if (!File.Exists(XDeltaHelper._path))
                return new List<Error>()
        {
          new Error()
          {
            Message = "XDelta was not found at '" + XDeltaHelper._path + "'"
          }
        };
            List<Error> errorList = new List<Error>();
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    FileName = XDeltaHelper._path,
                    WindowStyle = (ProcessWindowStyle)1,
                    Arguments = XDeltaHelper.FormatApplyArguments(Path.GetFullPath(source), Path.GetFullPath(target), Path.GetFullPath(patch)),
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    string end = ((TextReader)process.StandardError).ReadToEnd();
                    if (!string.IsNullOrEmpty(end))
                        errorList.Add(new Error() { Message = end });
                    ((TextReader)process.StandardError).Close();
                    ((TextWriter)process.StandardInput).Close();
                    ((TextReader)process.StandardOutput).Close();
                }
            }
            catch (Exception ex)
            {
                errorList.Add(new Error()
                {
                    Message = ex.GetFullMessage()
                });
            }
            return errorList;
        }

        public static List<Error> Apply(Stream source, Stream patch, Stream target)
        {
            List<Error> source1 = new List<Error>();
            string tempFileName1 = Path.GetTempFileName();
            string tempFileName2 = Path.GetTempFileName();
            string tempFileName3 = Path.GetTempFileName();
            try
            {
                XDeltaHelper.BufferedWriteToFile(source, tempFileName1);
                XDeltaHelper.BufferedWriteToFile(patch, tempFileName3);
                source1.AddRange((IEnumerable<Error>)XDeltaHelper.Apply(tempFileName1, tempFileName3, tempFileName2));
                if (!source1.Any<Error>())
                    XDeltaHelper.BufferedReadFromFile(tempFileName2, target);
            }
            catch (Exception ex)
            {
                source1.Add(new Error()
                {
                    Message = "X999 Error applying patch " + ex.GetFullMessage()
                });
            }
            finally
            {
                File.Delete(tempFileName1);
                File.Delete(tempFileName2);
                File.Delete(tempFileName3);
            }
            return source1;
        }

        public static List<Error> Create(string source, string target, string patch)
        {
            if (!File.Exists(XDeltaHelper._path))
                return new List<Error>()
        {
          new Error()
          {
            Message = "XDelta was not found at '" + XDeltaHelper._path + "'"
          }
        };
            List<Error> errorList = new List<Error>();
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    FileName = XDeltaHelper._path,
                    WindowStyle = (ProcessWindowStyle)1,
                    Arguments = XDeltaHelper.FormatCreateArguments(Path.GetFullPath(source), Path.GetFullPath(target), Path.GetFullPath(patch)),
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    string end = ((TextReader)process.StandardError).ReadToEnd();
                    if (!string.IsNullOrEmpty(end))
                        errorList.Add(new Error() { Message = end });
                    ((TextReader)process.StandardError).Close();
                    ((TextWriter)process.StandardInput).Close();
                    ((TextReader)process.StandardOutput).Close();
                }
            }
            catch (Exception ex)
            {
                errorList.Add(new Error()
                {
                    Message = ex.GetFullMessage()
                });
            }
            return errorList;
        }

        private static void BufferedWriteToFile(Stream s, string target)
        {
            byte[] numArray = new byte[65536];
            using (FileStream fileStream = File.Create(target))
            {
                for (int index = s.Read(numArray, 0, numArray.Length); index > 0; index = s.Read(numArray, 0, numArray.Length))
                    ((Stream)fileStream).Write(numArray, 0, index);
            }
            if (!s.CanSeek)
                return;
            s.Seek(0L, (SeekOrigin)0);
        }

        private static void BufferedReadFromFile(string source, Stream target)
        {
            byte[] numArray = new byte[65536];
            using (FileStream fileStream = File.OpenRead(source))
            {
                for (int index = ((Stream)fileStream).Read(numArray, 0, numArray.Length); index > 0; index = ((Stream)fileStream).Read(numArray, 0, numArray.Length))
                    target.Write(numArray, 0, index);
            }
            if (!target.CanSeek)
                return;
            target.Seek(0L, (SeekOrigin)0);
        }
    }
}
