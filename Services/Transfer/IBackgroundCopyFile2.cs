using System;
using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [Guid("83E81B93-0873-474D-8A8C-F2018B1A939C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IBackgroundCopyFile2
    {
        void GetRemoteName([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void GetLocalName([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void GetProgress(out BG_FILE_PROGRESS pVal);

        void GetFileRanges(out uint RangeCount, out IntPtr Ranges);

        void SetRemoteName([MarshalAs(UnmanagedType.LPWStr)] string RemoteName);
    }
}
