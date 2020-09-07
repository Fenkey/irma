using System;

namespace IRMAKit.Web
{
	public interface IHandlePerformance
	{
		/// <summary>
		/// Remark
		/// </summary>
		void Remark(string handlerName,
			Nullable<DateTime> checkStart,
			Nullable<DateTime> checkStop,
			Nullable<DateTime> handleStart,
			Nullable<DateTime> handleStop,
			Nullable<DateTime> endStart,
			Nullable<DateTime> endStop);
	}

	public interface IHandler
	{
		/// <summary>
		/// Do
		/// </summary>
		void Do(IContext context);
	}
}
