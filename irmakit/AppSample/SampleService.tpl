using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using IRMAKit.Log;
using IRMAKit.Web;
using IRMAKit.Store;
using IRMAKit.Utils;

namespace ${appName}.Web
{
	public class MyService : Service
	{
		private void IpWhiteListInit(IContext context)
		{
			string p = context.Config.User.Value<string>("ip_whitelist");
			ICidr cidr = new Cidr(p);
			context["ip_whitelist"] = cidr;
		}

		private void DbsInit(IContext context)
		{
			JObject o = context.Config.User.Value<JObject>("dbs");
			string host = o.Value<string>("host");
			int port = o.Value<int>("port");
			string user = o.Value<string>("user");
			string password = o.Value<string>("password");
			string db = o.Value<string>("db");
			// Here you should double check the validity of parameters and throw exception if failed 
			context["dbs"] = new MySqlStore(host, port, user, password, db);
		}

		private void McInit(IContext context)
		{
			JObject o = context.Config.User.Value<JObject>("mc");
			string servers = o.Value<string>("servers");
			string instance = o.Value<string>("instance");
			// Here you should double check the validity of parameters and throw exception if failed 
			/*
			 * Or use others engine:
			 * context["mc"] = new CMemcachedStore(servers, instance);
			 * context["mc"] = new MemcachedStoreWrapper(servers, instance);
			 */
			context["mc"] = new MemcachedStore(servers, instance);
			long expire = o.Value<long>("expire");
			if (expire < 0)
				expire = 0;
			context["mc_expire"] = expire;
		}

		private void SmtpInit(IContext context)
		{
			JObject o = context.Config.User.Value<JObject>("smtp");
			string server = o.Value<string>("server");
			string user = o.Value<string>("user");
			string password = o.Value<string>("password");
			// Here you should double check the validity of parameters and throw exception if failed 
			context["smtp"] = new Smtp(server, user, password);
		}

		private void OthersInit(IContext context)
		{
			context["unit"] = new Unit();
			context["fetcher"] = new Fetcher();
			context["aes"] = new Aes();
			context["des"] = new Des();
			context["rsa"] = new Rsa("irmakit");
			context["qrcoder"] = new QRCoder();
		}

		protected override void AppInit(IContext context)
		{
			IpWhiteListInit(context);
			DbsInit(context);
			McInit(context);
			OthersInit(context);

			if (context.IsW0)
				Logger.DEBUG("Code in here will be performed once only in the same process.");

			Logger.DEBUG("AppInit success.");
		}

		protected override void DoProxy(IContext context, string location, string param)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;
			IFetcher fetcher = (Fetcher)context["fetcher"];

			// 如果在Windows平台下Mock访问，不建议对网站自身作proxy，因为Mock方式下为单线程lock
			string url = "http://" + req.Host + "/" + context.Config.AppName + location;
			int code = res.SendProxy(fetcher, url, req, "auto proxy test");

			Logger.DEBUG("DoProxy working success. fetcher response code: " + code);
		}
	}
}
