using System;
using System.Collections.Generic;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public abstract class ReqProxyAttribute : IrmaAttribute
	{
		public virtual int Retrieve(string preStr)
		{
			return 0;
		}

		protected virtual string To(IContext context, ref string param)
		{
			return null;
		}

		public string ToWrapper(IContext context, ref string param)
		{
			if (Enter != null)
				Logger.DEBUG("Kit - ReqProxy entering('{0}'): {1}", context.Request.OriAppLocation, Enter);

			string ret = null;
			try {
				ret = To(context, ref param);
			} finally {
				if (Leave != null)
					Logger.DEBUG("Kit - ReqProxy leaved('{0}'): {1}", ret, Leave);
			}
			return ret;
		}
	}
}
