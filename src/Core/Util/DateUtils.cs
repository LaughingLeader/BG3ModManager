namespace ModManager.Util;

public static class DateUtils
{
	public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
	{
		// Unix timestamp is seconds past epoch
		System.DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		var unixTimeStampInTicks = (long)(unixTimeStamp * TimeSpan.TicksPerSecond);
		return new DateTime(dtDateTime.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
	}
}
