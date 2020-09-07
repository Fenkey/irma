using System;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
	public abstract class IrmaAttribute : Attribute
	{
		private string enter;
		public string Enter
		{
			get { return this.enter; }
			set { this.enter = value; }
		}

		private string leave;
		public string Leave
		{
			get { return this.leave; }
			set { this.leave = value; }
		}

		public virtual bool Init(IConfig config, string handlerName, byte methods) { return true; }
	}
}
