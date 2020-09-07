namespace IRMAKit.Utils
{
	public enum Algorithm
	{
		MD5 = 0,
		SHA1,
		SHA256,
		SHA512
	}

	public interface IRsa
	{
		////////////////////////////////////////////////////////////////////////
		///
		/// key from file
		///
		////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Encrypt (use public key)
		/// </summary>
		byte[] Encrypt(string keyFile, string content);

		/// <summary>
		/// Decrypt (use private key)
		/// </summary>
		string Decrypt(string keyFile, byte[] content);

		/// <summary>
		/// Sign (use private key)
		/// </summary>
		byte[] Sign(Algorithm algorithm, string keyFile, string content);

		/// <summary>
		/// Verify (use public key)
		/// </summary>
		bool Verify(Algorithm algorithm, string keyFile, string content, byte[] sign);

		/// <summary>
		/// Encrypt to base64 (use public key)
		/// </summary>
		string EncryptToBase64(string keyFile, string content);

		/// <summary>
		/// Decrypt from base64 (use private key)
		/// </summary>
		string DecryptFromBase64(string keyFile, string content);

		/// <summary>
		/// Sign to base64 (use private key)
		/// </summary>
		string SignToBase64(Algorithm algorithm, string keyFile, string content);

		/// <summary>
		/// Verify form base64 (use public key)
		/// </summary>
		bool VerifyFromBase64(Algorithm algorithm, string keyFile, string content, string sign);

		////////////////////////////////////////////////////////////////////////
		///
		/// key from memory
		///
		////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Encrypt (use public key)
		/// </summary>
		byte[] MemEncrypt(string key, string content);

		/// <summary>
		/// Decrypt (use private key)
		/// </summary>
		string MemDecrypt(string key, byte[] content);

		/// <summary>
		/// Sign (use private key)
		/// </summary>
		byte[] MemSign(Algorithm algorithm, string key, string content);

		/// <summary>
		/// Verify (use public key)
		/// </summary>
		bool MemVerify(Algorithm algorithm, string key, string content, byte[] sign);

		/// <summary>
		/// Encrypt to base64 (use public key)
		/// </summary>
		string MemEncryptToBase64(string key, string content);

		/// <summary>
		/// Decrypt from base64 (use private key)
		/// </summary>
		string MemDecryptFromBase64(string key, string content);

		/// <summary>
		/// Sign to base64 (use private key)
		/// </summary>
		string MemSignToBase64(Algorithm algorithm, string key, string content);

		/// <summary>
		/// Verify from base64 (use public key)
		/// </summary>
		bool MemVerifyFromBase64(Algorithm algorithm, string key, string content, string sign);
	}
}
