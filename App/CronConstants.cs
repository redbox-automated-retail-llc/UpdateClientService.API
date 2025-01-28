using System;

namespace UpdateClientService.API.App
{
    public class CronConstants
    {
        public static readonly string AtRandomMinuteEvery12thHour = string.Format("{0} */12 * * *", (object)new Random().Next(0, 59));
        public const string AtMinute0Every12thHour = "0 */12 * * *";

        public static string EveryXHours(int hours) => string.Format("0 */{0} * * *", (object)hours);

        public static string AtRandomMinuteEveryXHours(int hours)
        {
            return string.Format("{0} */{1} * * *", (object)new Random().Next(0, 59), (object)hours);
        }
    }
}
