using System;
using System.Text;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public class Sha256 : ISummary
	{
		public string Encode(byte[] content)
		{
			return ICall.Sha256(content);
		}

		public string Encode(string content)
		{
			return ICall.Sha256(Encoding.UTF8.GetBytes(content));
		}

		public bool Equals(byte[] content, string sha256)
		{
			if (content == null || string.IsNullOrEmpty(sha256))
				return false;
			return ICall.Sha256(content) == sha256.ToLower();
		}

		public bool Equals(string content, string sha256)
		{
			if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(sha256))
				return false;
			return ICall.Sha256(Encoding.UTF8.GetBytes(content)) == sha256.ToLower();
		}
	}
}
