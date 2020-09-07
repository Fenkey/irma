using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class OnePermissionCheckAttribute : ReqCheckAttribute
	{
		protected override bool Check(IContext context)
		{
			Logger.DEBUG("One permission check success.");
			return true;
		}
	}

	public class TwoPermissionCheckAttribute : ReqCheckAttribute
	{
		private string owner;
		public string Owner
		{
			set { this.owner = value; }
			get { return this.owner; }
		}

		protected override bool Check(IContext context)
		{
			Logger.DEBUG("Two permission check success. Owner('{0}')", this.owner);
			return true;
		}
	}

	public class ThreePermissionCheckAttribute : ReqCheckAttribute
	{
		protected override bool Check(IContext context)
		{
			Logger.DEBUG("Three permission check success.");
			return true;
		}
	}
}
