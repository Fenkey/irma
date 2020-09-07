using System;
using System.Text;
using IRMACore.Lower;

namespace IRMAKit.Utils
{
	public class Md5 : ISummary
	{
		public string Encode(byte[] content)
		{
			return ICall.Md5(content);
		}

		public string Encode(string content)
		{
			return ICall.Md5(Encoding.UTF8.GetBytes(content));
		}

		public bool Equals(byte[] content, string md5)
		{
			if (content == null || string.IsNullOrEmpty(md5))
				return false;
			return ICall.Md5(content) == md5.ToLower();
		}

		public bool Equals(string content, string md5)
		{
			if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(md5))
				return false;
			return ICall.Md5(Encoding.UTF8.GetBytes(content)) == md5.ToLower();
		}
	}
}
