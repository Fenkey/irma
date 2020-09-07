namespace IRMAKit.Utils
{
	public interface IUnit
	{
		/// <summary>
		/// Parse bytes, support (ignore case): k / m / g, e.g. 100M
		/// </summary>
		long ParseBytes(string val);

		/// <summary>
		/// Parse seconds, support (ignore case): s / m / h / d / w(week) / M(Month) / y(year), e.g. 100D
		/// </summary>
		long ParseSeconds(string val);
	}
}
