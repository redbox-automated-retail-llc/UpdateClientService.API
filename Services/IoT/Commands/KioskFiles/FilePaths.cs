using System;
using System.Collections.Generic;
using System.IO;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public static class FilePaths
    {
        public static Dictionary<FileTypeEnum, string> TypeMappings = new Dictionary<FileTypeEnum, string>()
    {
      {
        FileTypeEnum.KioskEngineLogs,
        "C:\\Program Files\\Redbox\\reds\\Kiosk Engine\\logs"
      },
      {
        FileTypeEnum.KioskClientServiceLogs,
        "C:\\ProgramData\\Redbox\\KioskClient\\Logs"
      },
      {
        FileTypeEnum.UpdateClientServiceLogs,
        "C:\\ProgramData\\Redbox\\UpdateClient\\Logs"
      },
      {
        FileTypeEnum.UpdateManagerLogs,
        "C:\\Program Files\\Redbox\\REDS\\Update Manager\\logs"
      },
      {
        FileTypeEnum.DeviceServiceLogs,
        "C:\\Program Files\\Redbox\\REDS\\DeviceService\\logs"
      },
      {
        FileTypeEnum.HALLogs,
        "C:\\Program Files\\Redbox\\KioskLogs\\Service"
      },
      {
        FileTypeEnum.Vend,
        "C:\\Program Files\\Redbox\\KioskLogs\\vend"
      },
      {
        FileTypeEnum.Unknowns,
        "C:\\Program Files\\Redbox\\KioskLogs\\Unknowns"
      },
      {
        FileTypeEnum.Sync,
        "C:\\Program Files\\Redbox\\KioskLogs\\Sync"
      },
      {
        FileTypeEnum.Return,
        "C:\\Program Files\\Redbox\\KioskLogs\\Return"
      },
      {
        FileTypeEnum.ErrorLogs,
        "C:\\Program Files\\Redbox\\KioskLogs\\ErrorLogs"
      },
      {
        FileTypeEnum.VMZ,
        "C:\\Program Files\\Redbox\\KioskLogs\\VMZ"
      },
      {
        FileTypeEnum.KioskEngineAnalyticsSessions,
        "C:\\Program Files\\Redbox\\reds\\Kiosk Engine\\data\\AnalyticsSessions"
      },
      {
        FileTypeEnum.Configuration,
        "C:\\ProgramData\\Redbox\\Configuration"
      },
      {
        FileTypeEnum.KioskEngineDataDirectory,
        "C:\\Program Files\\Redbox\\reds\\Kiosk Engine\\data"
      },
      {
        FileTypeEnum.Segment,
        "C:\\ProgramData\\Redbox\\Segmentation"
      },
      {
        FileTypeEnum.ProfileData,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Redbox\\Data\\Profile")
      },
      {
        FileTypeEnum.Certificates,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Redbox\\UpdateClient\\Certificates\\")
      }
    };
    }
}
