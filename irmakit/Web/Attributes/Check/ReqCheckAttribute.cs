using System;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public abstract class ReqCheckAttribute : IrmaAttribute
	{
		private int orderNum = 999;
		public int OrderNum
		{
			get { return this.orderNum; }
			set { this.orderNum = value; }
		}

		private bool cache = true;
		public bool Cache
		{
			get { return this.cache; }
			set { this.cache = value; }
		}

		protected virtual bool Check(IContext context)
		{
			return true;
		}

		public bool CheckWrapper(IContext context)
		{
			if (Enter != null)
				Logger.DEBUG("Kit - ReqCheck(OrderNum={0}) entering: {1}", OrderNum, Enter);

			bool ret = false;
			try {
				ret = Check(context);
			} finally {
				if (Leave != null)
					Logger.DEBUG("Kit - ReqCheck(OrderNum={0}) leaved({1}): {2}", OrderNum, ret, Leave);
			}
			return ret;
		}
	}
}
