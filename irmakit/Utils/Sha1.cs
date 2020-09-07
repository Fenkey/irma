using System;
using System.Text;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public class Sha1 : ISummary
	{
		public string Encode(byte[] content)
		{
			return ICall.Sha1(content);
		}

		public string Encode(string content)
		{
			return ICall.Sha1(Encoding.UTF8.GetBytes(content));
		}

		public bool Equals(byte[] content, string sha1)
		{
			if (content == null || string.IsNullOrEmpty(sha1))
				return false;
			return ICall.Sha1(content) == sha1.ToLower();
		}

		public bool Equals(string content, string sha1)
		{
			if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(sha1))
				return false;
			return ICall.Sha1(Encoding.UTF8.GetBytes(content)) == sha1.ToLower();
		}
	}
}
