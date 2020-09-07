using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class DesHandler : IHandler
	{
		private string Ecb(IDes des)
		{
			string content = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			// NOTE the length of key must be 16, 24 or 32.
			string key = "0123456789ABCDEF";

			string enc = des.EcbEncryptToBase64(key, content);
			string dec = des.EcbDecryptFromBase64Str(key, enc);

			/*
			string enc = des.EcbEncryptToBase64(key, content, PaddingType.PKCS7Padding);
			string dec = des.EcbDecryptFromBase64Str(key, enc, PaddingType.PKCS7Padding);
			*/

			return dec;
		}

		private string Cbc(IDes des)
		{
			string content = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			// NOTE the length of key must be 16, 24 or 32.
			string key = "0123456789ABCDEF";
			string iv = null;

			string enc = des.CbcEncryptToBase64(key, iv, content);
			string dec = des.CbcDecryptFromBase64Str(key, iv, enc);

			/*
			string enc = des.CbcEncryptToBase64(key, null, content, PaddingType.PKCS7Padding);
			string dec = des.CbcDecryptFromBase64Str(key, null, enc, PaddingType.PKCS7Padding);
			*/

			return dec;
		}

		private string NCbc(IDes des)
		{
			string content = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			// NOTE the length of key must be 16, 24 or 32.
			string key = "0123456789ABCDEF";
			string iv = null;

			string enc = des.NCbcEncryptToBase64(key, iv, content);
			string dec = des.NCbcDecryptFromBase64Str(key, iv, enc);

			/*
			string enc = des.NCbcEncryptToBase64(key, null, content, PaddingType.PKCS7Padding);
			string dec = des.NCbcDecryptFromBase64Str(key, null, enc, PaddingType.PKCS7Padding);
			*/

			return dec;
		}

		public void Do(IContext context)
		{
			IDes des = (Des)context["des"];
			IResponse res = context.Response;

			string dec = Ecb(des);
			res.BufferAppend(string.Format("ECB '{0}': {1}<br/>", dec, dec.Length));

			dec = Cbc(des);
			res.BufferAppend(string.Format("CBC '{0}': {1}<br/>", dec, dec.Length));

			dec = NCbc(des);
			res.BufferAppend(string.Format("NCBC '{0}': {1}", dec, dec.Length));

			res.Echo();

			Logger.DEBUG("Des handle success.");
		}
	}
}
