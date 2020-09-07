using System;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public sealed class Time : ITime
	{
		public long GetUnixTime()
		{
			return ICall.GetUnixTime();
		}

		public long BuildUnixTime(int y, int m, int d, int h, int M, int s)
		{
			if (y < 1900 ||
				m < 1 || m > 12 ||
				d < 1 || d > 31 ||
				h < 0 || h > 23 ||
				M < 0 || M > 59 ||
				s < 0 || s > 60) return -1L;
			return ICall.BuildUnixTime(y, m, d, h, M, s);
		}

		public long BuildGmTime(string str)
		{
			if (string.IsNullOrEmpty(str) || str.IndexOf(" GMT") <= 0)
				return -1L;
			return ICall.BuildGmTime(str);
		}

		public long UnixTimeToGmTime(long unixTime)
		{
			string gmtStr = null;
			return UnixTimeToGmTime(unixTime, ref gmtStr);
		}

		public long UnixTimeToGmTime(long unixTime, ref string gmtStr)
		{
			if (unixTime <= 0)
				return -1L;
			return ICall.UnixTimeToGmTime(unixTime, ref gmtStr);
		}
	}
}
