namespace IRMAKit.Utils
{
	public enum PaddingType
	{
		ZeroPadding = 0,
		PKCS5Padding,
		PKCS7Padding,
	}

	public interface IEncrypt
	{
		////////////////////////////////////////////////////////////////////
		/// ECB
		////////////////////////////////////////////////////////////////////
		/// <summary>
		/// ECB Encrypt
		/// </summary>
		byte[] EcbEncrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB Encrypt
		/// </summary>
		byte[] EcbEncrypt(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB Decrypt
		/// </summary>
		byte[] EcbDecrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB encrypt to base64
		/// </summary>
		string EcbEncryptToBase64(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB encrypt to base64
		/// </summary>
		string EcbEncryptToBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB decrypt from base64
		/// </summary>
		byte[] EcbDecryptFromBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// ECB decrypt from base64
		/// </summary>
		string EcbDecryptFromBase64Str(string key, string content, PaddingType pType=PaddingType.ZeroPadding);


		////////////////////////////////////////////////////////////////////
		/// CBC
		////////////////////////////////////////////////////////////////////
		/// <summary>
		/// CBC encrypt
		/// </summary>
		byte[] CbcEncrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt
		/// </summary>
		byte[] CbcEncrypt(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt
		/// </summary>
		byte[] CbcEncrypt(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC Decrypt
		/// </summary>
		byte[] CbcDecrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC Decrypt
		/// </summary>
		byte[] CbcDecrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt to base64
		/// </summary>
		string CbcEncryptToBase64(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt to base64
		/// </summary>
		string CbcEncryptToBase64(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt to base64
		/// </summary>
		string CbcEncryptToBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC encrypt to base64
		/// </summary>
		string CbcEncryptToBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC decrypt from base64
		/// </summary>
		byte[] CbcDecryptFromBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC decrypt from base64
		/// </summary>
		byte[] CbcDecryptFromBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC decrypt from base64
		/// </summary>
		string CbcDecryptFromBase64Str(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding);

		/// <summary>
		/// CBC decrypt from base64
		/// </summary>
		string CbcDecryptFromBase64Str(string key, string content, PaddingType pType=PaddingType.ZeroPadding);
	}
}
