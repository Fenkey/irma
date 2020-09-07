namespace IRMAKit.Utils
{
	public interface IDes : IEncrypt
	{
		////////////////////////////////////////////////////////////////////
		/// NCBC
		////////////////////////////////////////////////////////////////////
		/// <summary>
		/// NCBC加密方法
		/// </summary>
		byte[] NCbcEncrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt
		/// </summary>
		byte[] NCbcEncrypt(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt
		/// </summary>
		byte[] NCbcEncrypt(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt
		/// </summary>
		byte[] NCbcDecrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt
		/// </summary>
		byte[] NCbcDecrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt to base64
		/// </summary>
		string NCbcEncryptToBase64(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt to base64
		/// </summary>
		string NCbcEncryptToBase64(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt to base64
		/// </summary>
		string NCbcEncryptToBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC encrypt to base64
		/// </summary>
		string NCbcEncryptToBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt from base64
		/// </summary>
		byte[] NCbcDecryptFromBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt from base64
		/// </summary>
		byte[] NCbcDecryptFromBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt from base64
		/// </summary>
		string NCbcDecryptFromBase64Str(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// NCBC decrypt from base64
		/// </summary>
		string NCbcDecryptFromBase64Str(string key, string content, PaddingType pType=PaddingType.ZeroPadding);
	}
}
