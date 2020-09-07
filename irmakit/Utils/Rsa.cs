using System;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public sealed class Rsa : IRsa
	{
		private string keyPwd = null;
		public string KeyPwd
		{
			get { return this.keyPwd; }
			set { this.keyPwd = value; }
		}

		public Rsa(string keyPwd=null)
		{
			this.keyPwd = keyPwd;
		}

		////////////////////////////////////////////////////////////////////////
		///
		/// key from file
		///
		////////////////////////////////////////////////////////////////////////
		public byte[] Encrypt(string keyFile, string content)
		{
			return ICall.RsaEncrypt(keyFile, keyPwd, content);
		}

		public string Decrypt(string keyFile, byte[] content)
		{
			return ICall.RsaDecrypt(keyFile, keyPwd, content);
		}

		public byte[] Sign(Algorithm algorithm, string keyFile, string content)
		{
			return ICall.RsaSign((int)algorithm, keyFile, keyPwd, content);
		}

		public bool Verify(Algorithm algorithm, string keyFile, string content, byte[] sign)
		{
			return ICall.RsaVerify((int)algorithm, keyFile, keyPwd, content, sign);
		}

		public string EncryptToBase64(string keyFile, string content)
		{
			try {
				return Convert.ToBase64String(ICall.RsaEncrypt(keyFile, keyPwd, content));
			} catch {
				return null;
			}
		}

		public string DecryptFromBase64(string keyFile, string content)
		{
			try {
				return ICall.RsaDecrypt(keyFile, keyPwd, Convert.FromBase64String(content));
			} catch {
				return null;
			}
		}

		public string SignToBase64(Algorithm algorithm, string keyFile, string content)
		{
			try {
				return Convert.ToBase64String(ICall.RsaSign((int)algorithm, keyFile, keyPwd, content));
			} catch {
				return null;
			}
		}

		public bool VerifyFromBase64(Algorithm algorithm, string keyFile, string content, string sign)
		{
			try {
				return ICall.RsaVerify((int)algorithm, keyFile, keyPwd, content, Convert.FromBase64String(sign));
			} catch {
				return false;
			}
		}

		////////////////////////////////////////////////////////////////////////
		///
		/// key from memory
		///
		////////////////////////////////////////////////////////////////////////
		public byte[] MemEncrypt(string key, string content)
		{
			return ICall.RsaMemEncrypt(key, keyPwd, content);
		}

		public string MemDecrypt(string key, byte[] content)
		{
			return ICall.RsaMemDecrypt(key, keyPwd, content);
		}

		public byte[] MemSign(Algorithm algorithm, string key, string content)
		{
			return ICall.RsaMemSign((int)algorithm, key, keyPwd, content);
		}

		public bool MemVerify(Algorithm algorithm, string key, string content, byte[] sign)
		{
			return ICall.RsaMemVerify((int)algorithm, key, keyPwd, content, sign);
		}

		public string MemEncryptToBase64(string key, string content)
		{
			try {
				return Convert.ToBase64String(ICall.RsaMemEncrypt(key, keyPwd, content));
			} catch {
				return null;
			}
		}

		public string MemDecryptFromBase64(string key, string content)
		{
			try {
				return ICall.RsaMemDecrypt(key, keyPwd, Convert.FromBase64String(content));
			} catch {
				return null;
			}
		}

		public string MemSignToBase64(Algorithm algorithm, string key, string content)
		{
			try {
				return Convert.ToBase64String(ICall.RsaMemSign((int)algorithm, key, keyPwd, content));
			} catch {
				return null;
			}
		}

		public bool MemVerifyFromBase64(Algorithm algorithm, string key, string content, string sign)
		{
			try {
				return ICall.RsaMemVerify((int)algorithm, key, keyPwd, content, Convert.FromBase64String(sign));
			} catch {
				return false;
			}
		}
	}
}
