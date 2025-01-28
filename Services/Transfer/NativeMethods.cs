using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UpdateClientService.API.Services.Transfer
{
    internal static class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertStringSidToSidW(string stringSID, ref IntPtr SID);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupAccountSidW(
          string systemName,
          IntPtr SID,
          StringBuilder name,
          ref long cbName,
          StringBuilder domainName,
          ref long cbDomainName,
          ref int psUse);

        [DllImport("ole32.dll", CharSet = CharSet.Auto)]
        public static extern int CoInitializeSecurity(
          IntPtr pVoid,
          int cAuthSvc,
          IntPtr asAuthSvc,
          IntPtr pReserved1,
          RpcAuthnLevel level,
          RpcImpLevel impers,
          IntPtr pAuthList,
          EoAuthnCap dwCapabilities,
          IntPtr pReserved3);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        internal static extern bool GetFileVersionInfo(
          string sFileName,
          int handle,
          int size,
          byte[] infoBuffer);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        internal static extern int GetFileVersionInfoSize(string sFileName, out int handle);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        internal static extern bool VerQueryValue(
          byte[] pBlock,
          string pSubBlock,
          out string pValue,
          out uint len);

        [DllImport("version.dll", CharSet = CharSet.Auto)]
        internal static extern bool VerQueryValue(
          byte[] pBlock,
          string pSubBlock,
          out IntPtr pValue,
          out uint len);
    }
}
