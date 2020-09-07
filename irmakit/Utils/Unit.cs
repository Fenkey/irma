using System;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public sealed class Unit : IUnit
	{
		public long ParseBytes(string val)
		{
			return ICall.UnitParseBytes(val);
		}

		public long ParseSeconds(string val)
		{
			return ICall.UnitParseSeconds(val);
		}
	}
}
