using System;
using System.Collections.Generic;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public abstract class ReqRefAttribute : IrmaAttribute
	{
		public virtual int Retrieve(string preStr)
		{
			return 0;
		}

		protected virtual string To(IContext context)
		{
			return null;
		}

		public string ToWrapper(IContext context)
		{
			if (Enter != null)
				Logger.DEBUG("Kit - ReqRef entering('{0}'): {1}", context.Request.OriAppLocation, Enter);

			string ret = null;
			try {
				ret = To(context);
			} finally {
				if (Leave != null)
					Logger.DEBUG("Kit - ReqRef leaved('{0}'): {1}", ret, Leave);
			}
			return ret;
		}
	}
}
