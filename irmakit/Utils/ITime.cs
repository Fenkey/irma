namespace IRMAKit.Utils
{
	public interface ITime
	{
		/// <summary>
		/// Get unix time
		/// </summary>
		long GetUnixTime();

		/// <summary>
		/// Build unix time
		/// </summary>
		long BuildUnixTime(int y, int m, int d, int h, int M, int s);

		/// <summary>
		/// Build gmt time
		/// </summary>
		long BuildGmTime(string str);

		/// <summary>
		/// Convert unix time to gmt time
		/// </summary>
		long UnixTimeToGmTime(long unixTime);

		/// <summary>
		/// Convert unix time to gmt time
		/// </summary>
		long UnixTimeToGmTime(long unixTime, ref string gmtStr);
	}
}
