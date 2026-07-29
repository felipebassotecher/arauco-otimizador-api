namespace Techer.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime AddWorkdays(this DateTime d, int workDays, DateTime[] holidays = null)
        {
            DateTime tmpDate = d;

            while (workDays != 0)
            {
                int incr = workDays > 0 ? 1 : -1;

                tmpDate = tmpDate.AddDays(incr);

                if (!tmpDate.IsWeekend() && !tmpDate.IsHoliday(holidays))
                    workDays -= incr;
            }

            return tmpDate;
        }

        public static int CountWorkdays(this DateTime d, DateTime endDate, DateTime[] holidays = null)
        {
            int n = 0;

            DateTime tmp = d.AddDays(1);

            while (tmp <= endDate)
            {
                if (!tmp.IsWeekend() && !tmp.IsHoliday(holidays))
                    n++;

                tmp = tmp.AddDays(1);
            }

            return n;
        }

        public static bool IsWeekend(this DateTime d)
        {
            return d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
        }

        public static bool IsHoliday(this DateTime d, DateTime[] holidays)
        {
            bool isHoliday = false;

            if (holidays != null)
                isHoliday = holidays.Any(m => m.Month.Equals(d.Month) && m.Day.Equals(d.Day));

            return isHoliday;
        }

        public static int AsAge(this DateTime d)
        {
            // Save today's date.
            var today = DateTime.Today;
            // Calculate the age.
            var age = today.Year - d.Year;
            // Go back to the year the person was born in case of a leap year
            if (d.Date > today.AddYears(-age)) age--;

            return age;
        }

        public static int ToEpoch(this DateTime date)
        {
            TimeSpan t = date - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        public static long ToEpochMilliseconds(this DateTime date)
        {
            TimeSpan t = date - new DateTime(1970, 1, 1);
            return (int)t.TotalMilliseconds;
        }

        public static DateTime ToDatetimeFromEpochSeconds(this int unixTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static string ToISOString(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            var modTicks = dt.Ticks % d.Ticks;
            var delta = modTicks != 0 ? d.Ticks - modTicks : 0;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }

        public static DateTime RoundDown(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        public static DateTime RoundToNearest(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            bool roundUp = delta > d.Ticks / 2;
            var offset = roundUp ? d.Ticks : 0;

            return new DateTime(dt.Ticks + offset - delta, dt.Kind);
        }
    }

}
