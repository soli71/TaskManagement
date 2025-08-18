using System.Globalization;

namespace TaskManagementMvc.Helpers
{
    public static class PersianDateHelper
    {
        private static readonly PersianCalendar PersianCalendar = new PersianCalendar();

        public static string ToPersianDate(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return "";

            var year = PersianCalendar.GetYear(dateTime);
            var month = PersianCalendar.GetMonth(dateTime);
            var day = PersianCalendar.GetDayOfMonth(dateTime);

            return $"{year:0000}/{month:00}/{day:00}";
        }

        public static string ToPersianDateTime(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return "";

            var year = PersianCalendar.GetYear(dateTime);
            var month = PersianCalendar.GetMonth(dateTime);
            var day = PersianCalendar.GetDayOfMonth(dateTime);
            var hour = dateTime.Hour;
            var minute = dateTime.Minute;

            return $"{year:0000}/{month:00}/{day:00} {hour:00}:{minute:00}";
        }

        public static string ToPersianDate(this DateTime? dateTime)
        {
            if (dateTime == null || dateTime == DateTime.MinValue)
                return "";

            return dateTime.Value.ToPersianDate();
        }

        public static string ToPersianDateTime(this DateTime? dateTime)
        {
            if (dateTime == null || dateTime == DateTime.MinValue)
                return "";

            return dateTime.Value.ToPersianDateTime();
        }

        public static string GetPersianDayOfWeek(this DateTime dateTime)
        {
            var dayOfWeek = PersianCalendar.GetDayOfWeek(dateTime);
            return dayOfWeek switch
            {
                DayOfWeek.Saturday => "شنبه",
                DayOfWeek.Sunday => "یکشنبه",
                DayOfWeek.Monday => "دوشنبه",
                DayOfWeek.Tuesday => "سه‌شنبه",
                DayOfWeek.Wednesday => "چهارشنبه",
                DayOfWeek.Thursday => "پنج‌شنبه",
                DayOfWeek.Friday => "جمعه",
                _ => ""
            };
        }

        public static string GetPersianMonthName(int month)
        {
            return month switch
            {
                1 => "فروردین",
                2 => "اردیبهشت",
                3 => "خرداد",
                4 => "تیر",
                5 => "مرداد",
                6 => "شهریور",
                7 => "مهر",
                8 => "آبان",
                9 => "آذر",
                10 => "دی",
                11 => "بهمن",
                12 => "اسفند",
                _ => ""
            };
        }

        public static string ToPersianDateWithMonthName(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return "";

            var year = PersianCalendar.GetYear(dateTime);
            var month = PersianCalendar.GetMonth(dateTime);
            var day = PersianCalendar.GetDayOfMonth(dateTime);
            var monthName = GetPersianMonthName(month);

            return $"{day} {monthName} {year}";
        }

        public static string ToPersianDateWithMonthName(this DateTime? dateTime)
        {
            if (dateTime == null || dateTime == DateTime.MinValue)
                return "";

            return dateTime.Value.ToPersianDateWithMonthName();
        }
    }
}
