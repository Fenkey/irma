using System;
using IRMACore.Lower;

namespace IRMAKit.Log
{
	public sealed class Logger
	{
		public static void DEBUG(string str)
		{
			if (str != null)
				ICall.LogDebug(str);
		}

		public static void DEBUG(string str, params object[] args)
		{
			if (str != null)
				ICall.LogDebug(string.Format(str, args));
		}

		public static void EVENT(string str)
		{
			if (str != null)
				ICall.LogEvent(str);
		}

		public static void EVENT(string str, params object[] args)
		{
			if (str != null)
				ICall.LogEvent(string.Format(str, args));
		}

		public static void WARN(string str)
		{
			if (str != null)
				ICall.LogWarn(str);
		}

		public static void WARN(string str, params object[] args)
		{
			if (str != null)
				ICall.LogWarn(string.Format(str, args));
		}

		public static void ERROR(string str)
		{
			if (str != null)
				ICall.LogError(str);
		}

		public static void ERROR(string str, params object[] args)
		{
			if (str != null)
				ICall.LogError(string.Format(str, args));
		}

		public static void FATAL(string str)
		{
			if (str != null)
				ICall.LogFatal(str);
		}

		public static void FATAL(string str, params object[] args)
		{
			if (str != null)
				ICall.LogFatal(string.Format(str, args));
		}

		public static void TC(string str)
		{
			if (str != null)
				ICall.LogTc(str);
		}

		public static void TC(string str, params object[] args)
		{
			if (str != null)
				ICall.LogTc(string.Format(str, args));
		}
	}
}
