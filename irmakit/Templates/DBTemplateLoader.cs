using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using Commons.Collections;
using NVelocity.Runtime.Resource;
using NVelocity.Runtime.Resource.Loader;
using IRMAKit.Store;
using IRMAKit.Web;

namespace IRMAKit.Templates
{
	public sealed class DBTemplateLoader : IRMATemplateLoader
	{
		private Dictionary<string, ITemplate> templates = new Dictionary<string, ITemplate>();

		private IDBStore db;

		private string table;

		private string where;

		private string encoding;

		private int expire;

		public DBTemplateLoader()
		{
		}

		public DBTemplateLoader(IDBStore db, string table, string where, string encoding, int expire)
		{
			this.db = db;
			this.table = table;
			this.where = where;
			this.encoding = encoding;
			this.expire = expire;
		}

		public override ITemplate GetTemplate(string name)
		{
			return templates.ContainsKey(name) ? templates[name] : null;
		}

		public override void Init(ExtendedProperties prop)
		{
			if(!prop.ContainsKey("db"))
				throw new Exception("Property db is null");
			db = (IDBStore)prop["db"];

			if(!prop.ContainsKey("table"))
				throw new Exception("Property table is null");
			table = (string)prop["table"];

			if(prop.ContainsKey("where"))
				where = (string)prop["where"];

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
				if (tpl.Content != null && (DateTime.Now - tpl.LastTime).TotalSeconds < expire)
					return tpl.Content;
			}

			try {
				string sql = where == null ?
					string.Format("SELECT Content, ModifyTime FROM {0} WHERE Valid=TRUE AND TplName=@name", table) :
					string.Format("SELECT Content, ModifyTime FROM {0} WHERE Valid=TRUE AND {1} AND TplName=@name", table, where);

				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["name"] = name;
				DbDataReader reader = db.Query(sql, parameters);
				if (reader != null && reader.Read()) {
					byte[] bytes = Encoding.GetEncoding(encoding).GetBytes((string)reader["Content"]);
					lastModify = (DateTime)reader["ModifyTime"];
					if (isRaw && expire > 0) {
						tpl = new Template(bytes, lastModify, DateTime.Now);
						templates[name] = tpl;
					}
					return bytes;
				}
			} catch {
			} finally {
				db.Close();
			}
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
