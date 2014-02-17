using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager
{
    public static class Utils
    {
        public static string ChangeDateFormatToYYYYMMDD(string date)
        {
            if (string.IsNullOrEmpty(date))
                return null;

            return DateTime.Parse(date).ToString("yyyy-MM-dd");
        }

        public static bool IsOlderThanAMonth(string date)
        {
            DateTime lastMonth = DateTime.Now.AddMonths(-1);

            int result = DateTime.Compare(DateTime.Parse(date), lastMonth);

            if (result < 0)
                return true;

            return false;
        }
    }
}
