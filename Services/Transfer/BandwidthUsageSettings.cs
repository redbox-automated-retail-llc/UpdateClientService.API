using Microsoft.Win32;
using System;

namespace UpdateClientService.API.Services.Transfer
{
    public static class BandwidthUsageSettings
    {
        private const string m_registryPath = "SOFTWARE\\Policies\\Microsoft\\Windows\\BITS";

        public static int MaxBandwidthWhileWithInSchedule
        {
            get => BandwidthUsageSettings.Get<int>("MaxTransferRateOnSchedule");
            set => BandwidthUsageSettings.Set((object)value, "MaxTransferRateOnSchedule");
        }

        public static int MaxBandwidthWhileOutsideOfSchedule
        {
            get => BandwidthUsageSettings.Get<int>("MaxTransferRateOffSchedule");
            set => BandwidthUsageSettings.Set((object)value, "MaxTransferRateOffSchedule");
        }

        public static byte StartOfScheduleInHoursFromMidnight
        {
            get => (byte)BandwidthUsageSettings.Get<int>("MaxBandwidthValidFrom");
            set => BandwidthUsageSettings.Set((object)(int)value, "MaxBandwidthValidFrom");
        }

        public static byte EndOfScheduleInHoursFromMidnight
        {
            get => (byte)BandwidthUsageSettings.Get<int>("MaxBandwidthValidTo");
            set => BandwidthUsageSettings.Set((object)(int)value, "MaxBandwidthValidTo");
        }

        public static bool UseSystemMaxOutsideOfSchedule
        {
            get => BandwidthUsageSettings.Get<bool>("UseSystemMaximum");
            set => BandwidthUsageSettings.Set((object)value, "UseSystemMaximum");
        }

        public static bool EnableMaximumBandwitdthThrottle
        {
            get => BandwidthUsageSettings.Get<bool>("EnableBITSMaxBandwidth");
            set => BandwidthUsageSettings.Set((object)value, "EnableBITSMaxBandwidth");
        }

        static BandwidthUsageSettings()
        {
            BandwidthUsageSettings.EnableMaximumBandwitdthThrottle = true;
            BandwidthUsageSettings.UseSystemMaxOutsideOfSchedule = false;
        }

        private static T Get<T>(string keyName)
        {
            using (RegistryKey subKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\BITS"))
            {
                object obj = subKey.GetValue(keyName);
                return obj == null ? default(T) : (T)Convert.ChangeType(obj, typeof(T));
            }
        }

        private static void Set(object value, string keyName)
        {
            using (RegistryKey subKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\BITS"))
                subKey.SetValue(keyName, value);
        }
    }
}
