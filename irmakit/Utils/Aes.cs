using System;
using System.Text;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public sealed class Aes : IAes
	{
		////////////////////////////////////////////////////////////////////
		/// ECB
		////////////////////////////////////////////////////////////////////
		public byte[] EcbEncrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			if (key == null)
				return null;
			if (key.Length != 16 && key.Length != 24 && key.Length != 32)
				return null;
			return ICall.AesEcbEncrypt(key, content, (int)pType);
		}

		public byte[] EcbEncrypt(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return EcbEncrypt(key, Encoding.UTF8.GetBytes(content), pType);
			} catch {
				return null;
			}
		}

		public byte[] EcbDecrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			if (key == null)
				return null;
			if (key.Length != 16 && key.Length != 24 && key.Length != 32)
				return null;
			return ICall.AesEcbDecrypt(key, content, (int)pType);
		}

		public string EcbEncryptToBase64(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return Convert.ToBase64String(EcbEncrypt(key, content, pType));
			} catch {
				return null;
			}
		}

		public string EcbEncryptToBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				byte[] bytes = Encoding.UTF8.GetBytes(content);
				return Convert.ToBase64String(EcbEncrypt(key, content, pType));
			} catch {
				return null;
			}
		}

		public byte[] EcbDecryptFromBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return EcbDecrypt(key, Convert.FromBase64String(content), pType);
			} catch {
				return null;
			}
		}

		public string EcbDecryptFromBase64Str(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				byte[] bytes = EcbDecrypt(key, Convert.FromBase64String(content), pType);
				return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
			} catch {
				return null;
			}
		}


		////////////////////////////////////////////////////////////////////
		/// CBC
		////////////////////////////////////////////////////////////////////
		public byte[] CbcEncrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			if (key == null)
				return null;
			if (key.Length != 16 && key.Length != 24 && key.Length != 32)
				return null;

			/*
			 * NOTE: iv理论上只能是byte[16]，方法采用string，故一定不能类似
			 * 中文字符等，以避免取值错误。另外key长度与可加解密的明文长度无关
			 */
			if (iv == null || iv.Length < 16)
				iv = key.Substring(0, 16);
			else if (iv.Length > 16)
				iv = iv.Substring(0, 16);

			return ICall.AesCbcEncrypt(key, iv, content, (int)pType);
		}

		public byte[] CbcEncrypt(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return CbcEncrypt(key, iv, Encoding.UTF8.GetBytes(content), pType);
			} catch {
				return null;
			}
		}

		public byte[] CbcEncrypt(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcEncrypt(key, null, content, pType);
		}

		public byte[] CbcDecrypt(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			if (key == null)
				return null;
			if (key.Length != 16 && key.Length != 24 && key.Length != 32)
				return null;

			/*
			 * NOTE: iv理论上只能是byte[16]，方法采用string，故一定不能类似
			 * 中文字符等，以避免取值错误。另外key长度与可加解密的明文长度无关
			 */
			if (iv == null || iv.Length < 16)
				iv = key.Substring(0, 16);
			else if (iv.Length > 16)
				iv = iv.Substring(0, 16);

			return ICall.AesCbcDecrypt(key, iv, content, (int)pType);
		}

		public byte[] CbcDecrypt(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcDecrypt(key, null, content, pType);
		}

		public string CbcEncryptToBase64(string key, string iv, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return Convert.ToBase64String(CbcEncrypt(key, iv, content, pType));
			} catch {
				return null;
			}
		}

		public string CbcEncryptToBase64(string key, byte[] content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcEncryptToBase64(key, null, content, pType);
		}

		public string CbcEncryptToBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				byte[] bytes = Encoding.UTF8.GetBytes(content);
				return Convert.ToBase64String(CbcEncrypt(key, iv, content, pType));
			} catch {
				return null;
			}
		}

		public string CbcEncryptToBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcEncryptToBase64(key, null, content, pType);
		}

		public byte[] CbcDecryptFromBase64(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				return CbcDecrypt(key, iv, Convert.FromBase64String(content), pType);
			} catch {
				return null;
			}
		}

		public byte[] CbcDecryptFromBase64(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcDecryptFromBase64(key, null, content, pType);
		}

		public string CbcDecryptFromBase64Str(string key, string iv, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			try {
				byte[] bytes = CbcDecrypt(key, iv, Convert.FromBase64String(content), pType);
				return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
			} catch {
				return null;
			}
		}

		public string CbcDecryptFromBase64Str(string key, string content, PaddingType pType=PaddingType.ZeroPadding)
		{
			return CbcDecryptFromBase64Str(key, null, content, pType);
		}
	}
}
