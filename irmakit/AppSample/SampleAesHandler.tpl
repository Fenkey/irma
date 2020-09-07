using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class AesHandler : IHandler
	{
		private string Ecb(IAes aes)
		{
			string content = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			// NOTE the length of key must be 16, 24 or 32.
			string key = "0123456789ABCDEF";

			string enc = aes.EcbEncryptToBase64(key, content);
			string dec = aes.EcbDecryptFromBase64Str(key, enc);

			/*
			string enc = aes.EcbEncryptToBase64(key, content, PaddingType.PKCS7Padding);
			string dec = aes.EcbDecryptFromBase64Str(key, enc, PaddingType.PKCS7Padding);
			*/

			return dec;
		}

		private string Cbc(IAes aes)
		{
			string content = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			// NOTE the length of key must be 16, 24 or 32.
			string key = "0123456789ABCDEF";
			string iv = null;

			string enc = aes.CbcEncryptToBase64(key, iv, content);
			string dec = aes.CbcDecryptFromBase64Str(key, iv, enc);

			/*
			string enc = aes.CbcEncryptToBase64(key, null, content, PaddingType.PKCS7Padding);
			string dec = aes.CbcDecryptFromBase64Str(key, null, enc, PaddingType.PKCS7Padding);
			*/

			return dec;
		}

		public void Do(IContext context)
		{
			IAes aes = (Aes)context["aes"];
			IResponse res = context.Response;

			string dec = Ecb(aes);
			res.BufferAppend(string.Format("ECB '{0}': {1}<br/>", dec, dec.Length));

			dec = Cbc(aes);
			res.BufferAppend(string.Format("CBC '{0}': {1}", dec, dec.Length));

			res.Echo();

			Logger.DEBUG("Aes handle success.");
		}
	}
}
