using System;

namespace IRMAKit.Templates
{
	public class Template : ITemplate
	{
		private byte[] content;
		public byte[] Content
		{
			get { return this.content; }
		}

		private DateTime lastModify;
		public DateTime LastModify
		{
			get { return this.lastModify; }
		}

		private DateTime lastTime;
		public DateTime LastTime
		{
			get { return this.lastTime; }
		}

		public Template(byte[] content, DateTime lastModify, DateTime lastTime)
		{
			this.content = content;
			this.lastModify = lastModify;
			this.lastTime = lastTime;
		}
	}
}
