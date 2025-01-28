using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace UpdateClientService.API.Services.Kernel
{
    public static class TimeZoneFunctions
    {
        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        private static extern bool Win32SetSystemTime(ref TimeZoneFunctions.SystemTime sysTime);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetDynamicTimeZoneInformation(
          ref TimeZoneFunctions.DynamicTimeZoneInformation lpTimeZoneInformation);

        private static TimeZoneFunctions.SystemTime ToSystemTime(this TimeZoneInfo.TransitionTime input)
        {
            TimeZoneFunctions.SystemTime systemTime1 = new TimeZoneFunctions.SystemTime();
            systemTime1.Year = (ushort)0;
            systemTime1.Month = (ushort)input.Month;
            systemTime1.Day = input.IsFixedDateRule ? (ushort)input.Day : (ushort)input.Week;
            systemTime1.Hour = (ushort)input.TimeOfDay.Hour;
            ref TimeZoneFunctions.SystemTime local1 = ref systemTime1;
            DateTime timeOfDay = input.TimeOfDay;
            int second = (int)(ushort)timeOfDay.Second;
            local1.Second = (ushort)second;
            ref TimeZoneFunctions.SystemTime local2 = ref systemTime1;
            timeOfDay = input.TimeOfDay;
            int minute = (int)(ushort)timeOfDay.Minute;
            local2.Minute = (ushort)minute;
            ref TimeZoneFunctions.SystemTime local3 = ref systemTime1;
            timeOfDay = input.TimeOfDay;
            int millisecond = (int)(ushort)timeOfDay.Millisecond;
            local3.Millisecond = (ushort)millisecond;
            TimeZoneFunctions.SystemTime systemTime2 = systemTime1;
            switch (input.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    systemTime2.DayOfWeek = (ushort)0;
                    break;
                case DayOfWeek.Monday:
                    systemTime2.DayOfWeek = (ushort)1;
                    break;
                case DayOfWeek.Tuesday:
                    systemTime2.DayOfWeek = (ushort)2;
                    break;
                case DayOfWeek.Wednesday:
                    systemTime2.DayOfWeek = (ushort)3;
                    break;
                case DayOfWeek.Thursday:
                    systemTime2.DayOfWeek = (ushort)4;
                    break;
                case DayOfWeek.Friday:
                    systemTime2.DayOfWeek = (ushort)5;
                    break;
                case DayOfWeek.Saturday:
                    systemTime2.DayOfWeek = (ushort)6;
                    break;
            }
            return systemTime2;
        }

        private static void SetAdjustmentRules(
          ref TimeZoneFunctions.DynamicTimeZoneInformation info,
          TimeZoneInfo.AdjustmentRule currentAdjustmentRule)
        {
            if (currentAdjustmentRule == null)
            {
                TimeZoneFunctions.SystemTime systemTime = new TimeZoneFunctions.SystemTime()
                {
                    Year = 0,
                    Month = 0,
                    DayOfWeek = 0,
                    Day = 0,
                    Hour = 0,
                    Minute = 0,
                    Second = 0,
                    Millisecond = 0
                };
                info.StandardDate = systemTime;
                info.DaylightDate = systemTime;
                info.DaylightBias = 0;
            }
            else
            {
                info.StandardDate = currentAdjustmentRule.DaylightTransitionEnd.ToSystemTime();
                info.DaylightDate = currentAdjustmentRule.DaylightTransitionStart.ToSystemTime();
                info.DaylightBias = (int)currentAdjustmentRule.DaylightDelta.Negate().TotalMinutes;
            }
        }

        public static TimeZoneFunctions.SetTimeResult SetTime(DateTime dateTime)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneFunctions.SetTimeResult setTimeResult;
            if (dateTime.AddMinutes(-5.0) < utcNow && utcNow < dateTime.AddMinutes(5.0))
            {
                setTimeResult = TimeZoneFunctions.SetTimeResult.InRange;
            }
            else
            {
                TimeZoneFunctions.SystemTime sysTime = new TimeZoneFunctions.SystemTime()
                {
                    Year = (ushort)dateTime.Year,
                    Month = (ushort)dateTime.Month,
                    Day = (ushort)dateTime.Day,
                    Hour = (ushort)dateTime.Hour,
                    Minute = (ushort)dateTime.Minute,
                    Second = (ushort)dateTime.Second
                };
                setTimeResult = !TimeZoneFunctions.Win32SetSystemTime(ref sysTime) ? TimeZoneFunctions.SetTimeResult.Errored : TimeZoneFunctions.SetTimeResult.Changed;
            }
            return setTimeResult;
        }

        public static TimeZoneFunctions.SetTimeZoneResult SetTimeZone(TimeZoneInfo timeZoneInfo)
        {
            TimeZoneFunctions.SetTimeZoneResult setTimeZoneResult;
            if (TimeZoneInfo.Local.Equals(timeZoneInfo))
            {
                setTimeZoneResult = TimeZoneFunctions.SetTimeZoneResult.Same;
            }
            else
            {
                TimeZoneInfo.AdjustmentRule[] adjustmentRules = timeZoneInfo.GetAdjustmentRules();
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneFunctions.DynamicTimeZoneInformation timeZoneInformation = new TimeZoneFunctions.DynamicTimeZoneInformation()
                {
                    StandardName = timeZoneInfo.StandardName,
                    DaylightName = timeZoneInfo.DaylightName,
                    StandardBias = 0,
                    Bias = (int)timeZoneInfo.BaseUtcOffset.Negate().TotalMinutes,
                    TimeZoneKeyName = timeZoneInfo.StandardName,
                    DynamicDaylightTimeDisabled = false
                };
                TimeZoneInfo.AdjustmentRule currentAdjustmentRule = ((IEnumerable<TimeZoneInfo.AdjustmentRule>)adjustmentRules).FirstOrDefault<TimeZoneInfo.AdjustmentRule>((Func<TimeZoneInfo.AdjustmentRule, bool>)(ar => ar.DateStart < utcNow && ar.DateEnd > utcNow));
                TimeZoneFunctions.SetAdjustmentRules(ref timeZoneInformation, currentAdjustmentRule);
                TokenPrivilegesAccess.EnablePrivilege("SeTimeZonePrivilege");
                setTimeZoneResult = !TimeZoneFunctions.SetDynamicTimeZoneInformation(ref timeZoneInformation) ? TimeZoneFunctions.SetTimeZoneResult.Errored : TimeZoneFunctions.SetTimeZoneResult.Changed;
                TokenPrivilegesAccess.DisablePrivilege("SeTimeZonePrivilege");
            }
            return setTimeZoneResult;
        }

        private struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DynamicTimeZoneInformation
        {
            [MarshalAs(UnmanagedType.I4)]
            public int Bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string StandardName;
            public TimeZoneFunctions.SystemTime StandardDate;
            [MarshalAs(UnmanagedType.I4)]
            public int StandardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DaylightName;
            public TimeZoneFunctions.SystemTime DaylightDate;
            [MarshalAs(UnmanagedType.I4)]
            public int DaylightBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string TimeZoneKeyName;
            public bool DynamicDaylightTimeDisabled;
        }

        public enum SetTimeResult
        {
            NotSet,
            InRange,
            Changed,
            Errored,
        }

        public enum SetTimeZoneResult
        {
            NotSet,
            Same,
            Changed,
            Errored,
        }
    }
}
