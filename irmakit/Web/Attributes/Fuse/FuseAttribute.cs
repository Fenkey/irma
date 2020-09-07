using System;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=false)]
	public class FuseCheckAttribute : IrmaAttribute
	{
	}
}
