using System;

namespace IRMAKit.Web
{
	public abstract class HandlePerformanceAttribute : Attribute
	{
		public virtual void Remark(string handlerName, DateTime checkStart, DateTime checkStop, DateTime handleStart, DateTime handleStop, DateTime endStart, DateTime endStop) {}
	}
}
