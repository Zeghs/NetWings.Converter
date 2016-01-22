using System;

namespace Zeghs.Utils {
	internal sealed class DateTimeParser {
		internal static DateTime Parse(string date, string time) {
			int iYear = int.Parse(date.Substring(0, 4));
			int iMonth = int.Parse(date.Substring(4, 2));
			int iDay = int.Parse(date.Substring(6, 2));
			int iHour = int.Parse(time.Substring(0, 2));
			int iMinute = int.Parse(time.Substring(2, 2));
			int iSecond = int.Parse(time.Substring(4, 2));

			return new DateTime(iYear, iMonth, iDay, iHour, iMinute, iSecond);
		}
	}
}