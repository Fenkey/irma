using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using Commons.Collections;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;
using IRMAKit.Web;

namespace IRMAKit.Templates
{
	public sealed class FileTemplateLoader : IRMATemplateLoader
	{
		private Dictionary<string, ITemplate> templates = new Dictionary<string, ITemplate>();

		private string path;

		private string encoding;

		private int expire;

		public FileTemplateLoader()
		{
		}

		public FileTemplateLoader(string path, string encoding, int expire)
		{
			this.path = path;
			this.encoding = encoding;
			this.expire = expire;
		}

		public override ITemplate GetTemplate(string name)
		{
			return templates.ContainsKey(name) ? templates[name] : null;
		}

		public override void Init(ExtendedProperties prop)
		{
			if(!prop.ContainsKey("path"))
				throw new Exception("Property path is null");
			path = prop["path"].ToString();

			if (!prop.ContainsKey("expire"))
				throw new Exception("Property expire is null");
			expire = (int)prop["expire"];

			encoding = prop.ContainsKey("encoding") ? prop["encoding"].ToString() : "UTF-8";
		}

		public override byte[] GetResource(string name, ref DateTime lastModify, bool isRaw)
		{
			ITemplate tpl = null;
			if (expire > 0 && templates.ContainsKey(name)) {
				tpl = templates[name];
				if (tpl.Content != null && ((DateTime.Now - tpl.LastTime).TotalSeconds < expire))
					return tpl.Content;
			}

			/*
			 * FIX:
			 * 1. name采用完整文件名，但建议name采用".tpl"后缀标识（非必须的要求）
			 * 2. name也可以包含路径，但不能以“/”开头，否则本处Path.Combine将视其为从根目录开始；
			 * 反之才会正常和path进行合并。例如："js/1.js"、而非"/js/1.js"
			 */
			string file = Path.Combine(path, name);
			// 为方便非Web应用情况下使用，故需要判断Service.CurrentContext
			if (Service.CurrentContext != null && Service.CurrentContext.OS.Equals("windows"))
				file = file.Replace("/", "\\");

			try {
				using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
					byte[] bytes = new byte[fs.Length];
					fs.Read(bytes, 0, bytes.Length);
					lastModify = Directory.GetLastWriteTime(file);

					if (isRaw && expire > 0) {
						tpl = new Template(bytes, lastModify, DateTime.Now);
						templates[name] = tpl;
					}
					return bytes;
				}
			} catch {}
			return null;
		}

		public override Stream GetResourceStream(string name)
		{
			ITemplate tpl = null;

			DateTime lastModify = DateTime.Now;
			byte[] bytes = GetResource(name, ref lastModify, false);
			if (bytes != null) {
				if (expire > 0) {
					tpl = new Template(bytes, lastModify, DateTime.Now);
					templates[name] = tpl;
				}
				return new MemoryStream(bytes);
			}
			return null;
		}

		public override long GetLastModified(Resource resource)
		{
			return 0;
		}

		public override bool IsSourceModified(Resource resource)
		{
			return false;
		}
	}
}
