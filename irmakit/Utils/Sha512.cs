using System;
using System.Text;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public class Sha512 : ISummary
	{
		public string Encode(byte[] content)
		{
			return ICall.Sha512(content);
		}

		public string Encode(string content)
		{
			return ICall.Sha512(Encoding.UTF8.GetBytes(content));
		}

		public bool Equals(byte[] content, string sha512)
		{
			if (content == null || string.IsNullOrEmpty(sha512))
				return false;
			return ICall.Sha512(content) == sha512.ToLower();
		}

		public bool Equals(string content, string sha512)
		{
			if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(sha512))
				return false;
			return ICall.Sha512(Encoding.UTF8.GetBytes(content)) == sha512.ToLower();
		}
	}
}
