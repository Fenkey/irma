using System;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public sealed class Ver : IVer
	{
		public int Cmp(string v1, string v2, ref string error)
		{
			return ICall.VerCmp(v1, v2, ref error);
		}

		public int Cmp(string v1, string v2)
		{
			string error = null;
			int ret = ICall.VerCmp(v1, v2, ref error);
			if (error != null)
				throw new Exception(error);
			return ret;
		}
	}
}
