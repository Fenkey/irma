using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Commons.Collections;
using IRMAKit.Web;
using IRMAKit.Store;

namespace IRMAKit.Templates
{
	public class RawTemplateManager : ITemplateManager
	{
		private string path;

		private IDBStore db;

		private string table;

		private string where;

		private string encoding;

		private IRMATemplateLoader loader;

		public RawTemplateManager(string path, string encoding)
		{
			this.path = path;
			this.encoding = encoding;
			this.loader = new FileTemplateLoader(path, encoding, 0);
		}

		public RawTemplateManager(string path) : this(path, "UTF-8")
		{
		}

		public RawTemplateManager(IDBStore db, string table, string pwhere, string encoding)
		{
			this.db = db;
			this.table = table;
			this.where = pwhere;
			this.encoding = encoding;
			this.loader = new DBTemplateLoader(db, table, pwhere, encoding, 0);
		}

		public RawTemplateManager(IDBStore db, string table) : this(db, table, null, "UTF-8")
		{
		}

		public RawTemplateManager(IDBStore db, string table, string pwhere) : this(db, table, pwhere, "UTF-8")
		{
		}

		public byte[] RenderBytes(string tplName, Dictionary<string, object> variables)
		{
			DateTime lastModify = DateTime.Now;
			return loader.GetResource(tplName, ref lastModify, true);
		}

		public byte[] RenderBytes(string tplName)
		{
			return RenderBytes(tplName, null);
		}

		public string Render(string tplName, Dictionary<string, object> variables)
		{
			DateTime lastModify = DateTime.Now;
			byte[] bytes = loader.GetResource(tplName, ref lastModify, true);
			string content = Encoding.GetEncoding(encoding).GetString(bytes);
			return content;
		}

		public string Render(string tplName)
		{
			return Render(tplName, null);
		}
	}
}
