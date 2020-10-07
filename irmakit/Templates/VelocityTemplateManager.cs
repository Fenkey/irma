using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;
using IRMAKit.Web;
using IRMAKit.Store;

namespace IRMAKit.Templates
{
	public class VelocityTemplateManager : ITemplateManager
	{
		/*
		 * FIX: VelocityEngine内部采用了static单例信息及运行时处理，如：
		 * NVelocity.Runtime.RuntimeSingleton::ri
		 * NVelocity.Runtime.RuntimeInstance::Init()
		 * 在engine.Init()后可能会对多线程启动应用带来影响，典型如对Service.CurrentContext引用为null
		 */

		VelocityEngine engine = new VelocityEngine();

		private string path;

		private IDBStore db;

		private string table;

		private string where;

		private string encoding;

		private int expire;

		public VelocityTemplateManager(string path, string encoding, int expire)
		{
			// expire: 仅当>0时，内部将渲染结果进行cache
			this.path = path;
			this.encoding = encoding;
			this.expire = expire;

			ExtendedProperties props = new ExtendedProperties();
			props.AddProperty("resource.loader", "irmakitloader");
			// 为方便非Web应用情况下使用，故需要判断Service.CurrentContext
			if (Service.CurrentContext != null && Service.CurrentContext.OS.Equals("windows"))
				props.AddProperty("irmakitloader.resource.loader.class", "IRMAKit.Templates.FileTemplateLoader;IRMAKit-windows");
			else
				props.AddProperty("irmakitloader.resource.loader.class", "IRMAKit.Templates.FileTemplateLoader;IRMAKit");
			props.AddProperty("irmakitloader.resource.loader.path", path);
			props.AddProperty("irmakitloader.resource.loader.encoding", encoding);
			props.AddProperty("irmakitloader.resource.loader.expire", expire);
			props.AddProperty("velocimacro.library", "");
			props.AddProperty(RuntimeConstants.INPUT_ENCODING, encoding);
			this.engine.Init(props);
		}

		public VelocityTemplateManager(string path, int expire) : this(path, "UTF-8", expire)
		{
		}

		public VelocityTemplateManager(IDBStore db, string table, string pwhere, string encoding, int expire)
		{
			// expire: 仅当>0时，内部将渲染结果进行cache
			this.db = db;
			this.table = table;
			this.where = pwhere;
			this.encoding = encoding;
			this.expire = expire;

			ExtendedProperties props = new ExtendedProperties();
			props.AddProperty("resource.loader", "irmakitloader");
			// 为方便非Web应用情况下使用，故需要判断Service.CurrentContext
			if (Service.CurrentContext != null && Service.CurrentContext.OS.Equals("windows"))
				props.AddProperty("irmakitloader.resource.loader.class", "IRMAKit.Templates.DBTemplateLoader;IRMAKit-windows");
			else
				props.AddProperty("irmakitloader.resource.loader.class", "IRMAKit.Templates.DBTemplateLoader;IRMAKit");
			props.AddProperty("irmakitloader.resource.loader.db", db);
			props.AddProperty("irmakitloader.resource.loader.table", table);
			props.AddProperty("irmakitloader.resource.loader.where", pwhere);
			props.AddProperty("irmakitloader.resource.loader.encoding", encoding);
			props.AddProperty("irmakitloader.resource.loader.expire", expire);
			props.AddProperty("velocimacro.library", "");
			props.AddProperty(RuntimeConstants.INPUT_ENCODING, encoding);
			this.engine.Init(props);
		}

		public VelocityTemplateManager(IDBStore db, string table, int expire) : this(db, table, null, "UTF-8", expire)
		{
		}

		public VelocityTemplateManager(IDBStore db, string table, string pwhere, int expire) : this(db, table, pwhere, "UTF-8", expire)
		{
		}

		public byte[] RenderBytes(string tplName, Dictionary<string, object> variables)
		{
			StringWriter sw = new StringWriter();
			VelocityContext context = new VelocityContext();
			if (variables != null) {
				foreach (KeyValuePair<string, object> v in variables)
					context.Put(v.Key, v.Value);
			}
			NVelocity.Template tpl = engine.GetTemplate(tplName);
			tpl.Merge(context, sw);
			return Encoding.GetEncoding(encoding).GetBytes(sw.GetStringBuilder().ToString());
		}

		public byte[] RenderBytes(string tplName)
		{
			return RenderBytes(tplName, null);
		}

		public string Render(string tplName, Dictionary<string, object> variables)
		{
			StringWriter sw = new StringWriter();
			VelocityContext context = new VelocityContext();
			if (variables != null) {
				foreach (KeyValuePair<string, object> v in variables)
					context.Put(v.Key, v.Value);
			}
			NVelocity.Template tpl = engine.GetTemplate(tplName);
			tpl.Merge(context, sw);
			return sw.GetStringBuilder().ToString();
		}

		public string Render(string tplName)
		{
			return Render(tplName, null);
		}
	}
}
