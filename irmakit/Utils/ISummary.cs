namespace IRMAKit.Utils
{
	public interface ISummary
	{
		/// <summary>
		/// Encode
		/// </summary>
		string Encode(byte[] content);

		/// <summary>
		/// Encode
		/// </summary>
		string Encode(string content);

		/// <summary>
		/// Equals
		/// </summary>
		bool Equals(byte[] content, string md5);

		/// <summary>
		/// Equals
		/// </summary>
		bool Equals(string content, string md5);
	}
}
