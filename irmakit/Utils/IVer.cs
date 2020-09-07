namespace IRMAKit.Utils
{
	public interface IVer
	{
		/// <summary>
		/// Cmp
		/// Note there may be differences in results between Windows and Linux.
		/// </summary>
		int Cmp(string v1, string v2, ref string error);

		/// <summary>
		/// Cmp
		/// </summary>
		int Cmp(string v1, string v2);
	}
}
