using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Transfer
{
    [Guid("01B7BD23-FB88-4A77-8490-5891D3E4653A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IBackgroundCopyFile
    {
        void GetRemoteName([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void GetLocalName([MarshalAs(UnmanagedType.LPWStr)] out string pVal);

        void GetProgress(out BG_FILE_PROGRESS pVal);
    }
}
