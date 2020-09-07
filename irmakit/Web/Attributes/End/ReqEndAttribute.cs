using System;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public abstract class ReqEndAttribute : IrmaAttribute
	{
		private int orderNum = 999;
		public int OrderNum
		{
			get { return this.orderNum; }
			set { this.orderNum = value; }
		}

		protected virtual void End(IContext context) {}

		public void EndWrapper(IContext context)
		{
			if (Enter != null)
				Logger.DEBUG("Kit - ReqEnd(OrderNum={0}) entering: {1}", OrderNum, Enter);

			try {
				End(context);
			} finally {
				if (Leave != null)
					Logger.DEBUG("Kit - ReqEnd(OrderNum={0}) leaved: {1}", OrderNum, Leave);
			}
		}
	}
}
