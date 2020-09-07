using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class HandlePerformance : IHandlePerformance
	{
		public void Remark(string handlerName,
			Nullable<DateTime> checkStart,
			Nullable<DateTime> checkStop,
			Nullable<DateTime> handleStart,
			Nullable<DateTime> handleStop,
			Nullable<DateTime> endStart,
			Nullable<DateTime> endStop)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("handlerName=" + handlerName);

			TimeSpan tsCheck, tsHandle, tsEnd;

			if (checkStart != null && checkStop != null) {
				TimeSpan ts = checkStop.Value - checkStart.Value;
				sb.Append("`tsCheck=" + ts.TotalMilliseconds);
			} else
				sb.Append("`tsCheck=0");

			if (handleStart != null && handleStop != null) {
				TimeSpan ts = handleStop.Value - handleStart.Value;
				sb.Append("`tsHandle=" + ts.TotalMilliseconds);
			} else
				sb.Append("`tsHandle=0");

			if (endStart != null && endStop != null) {
				TimeSpan ts = endStop.Value - endStart.Value;
				sb.Append("`tsEnd=" + ts.TotalMilliseconds);
			} else
				sb.Append("`tsEnd=0");

			Logger.DEBUG(sb.ToString());
		}
	}
}
