using System;
using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("54B50739-686F-45EB-9DFF-D6A9A0FAA9AF")]
    [ComImport]
    internal interface IBackgroundCopyJob2
    {
        void AddFileSet(uint cFileCount, [MarshalAs(UnmanagedType.LPArray)] BG_FILE_INFO[] pFileSet);

        void AddFile([MarshalAs(UnmanagedType.LPWStr)] string RemoteUrl, [MarshalAs(UnmanagedType.LPWStr)] string LocalName);

        void EnumFiles([MarshalAs(UnmanagedType.Interface)] out IEnumBackgroundCopyFiles pEnum);

        void Suspend();

        void Resume();

        void Cancel();

        void Complete();

        void GetId(out Guid pVal);

        void GetType(out BG_JOB_TYPE pVal);

        void GetProgress(out BG_JOB_PROGRESS pVal);

        void GetTimes(out BG_JOB_TIMES pVal);

        void GetState(out BG_JOB_STATE pVal);

        void GetError([MarshalAs(UnmanagedType.Interface)] out IBackgroundCopyError ppError);

        void GetOwner([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Val);

        void GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string Val);

        void GetDescription([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void SetPriority(BG_JOB_PRIORITY Val);

        void GetPriority(out BG_JOB_PRIORITY pVal);

        void SetNotifyFlags([MarshalAs(UnmanagedType.U4)] BG_JOB_NOTIFICATION_TYPE Val);

        void GetNotifyFlags(out uint pVal);

        void SetNotifyInterface([MarshalAs(UnmanagedType.IUnknown)] object Val);

        void GetNotifyInterface([MarshalAs(UnmanagedType.IUnknown)] out object pVal);

        void SetMinimumRetryDelay(uint Seconds);

        void GetMinimumRetryDelay(out uint Seconds);

        void SetNoProgressTimeout(uint Seconds);

        void GetNoProgressTimeout(out uint Seconds);

        void GetErrorCount(out ulong Errors);

        void SetProxySettings(BG_JOB_PROXY_USAGE ProxyUsage, [MarshalAs(UnmanagedType.LPWStr)] string ProxyList, [MarshalAs(UnmanagedType.LPWStr)] string ProxyBypassList);

        void GetProxySettings(
          out BG_JOB_PROXY_USAGE pProxyUsage,
          [MarshalAs(UnmanagedType.LPWStr)] out string pProxyList,
          [MarshalAs(UnmanagedType.LPWStr)] out string pProxyBypassList);

        void TakeOwnership();

        void SetNotifyCmdLine([MarshalAs(UnmanagedType.LPWStr)] string Program, [MarshalAs(UnmanagedType.LPWStr)] string Parameters);

        void GetNotifyCmdLine([MarshalAs(UnmanagedType.LPWStr)] out string pProgram, [MarshalAs(UnmanagedType.LPWStr)] out string pParameters);

        void GetReplyProgress(out BG_JOB_REPLY_PROGRESS pProgress);

        void GetReplyData(IntPtr ppBuffer, out ulong pLength);

        void SetReplyFileName([MarshalAs(UnmanagedType.LPWStr)] string ReplyFileName);

        void GetReplyFileName([MarshalAs(UnmanagedType.LPWStr)] out string pReplyFileName);

        void SetCredentials([In] ref BG_AUTH_CREDENTIALS Credentials);

        void RemoveCredentials(BG_AUTH_TARGET Target, BG_AUTH_SCHEME Scheme);
    }
}
