using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using IRMAKit.Store;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	public class MemcachedHandler : IHandler
	{
		private class C
		{
			public int age;
			public int Age { get { return this.age; } }
			public string Name;
			public string Nation;
			public string Family;

			public C(string name, string nation, string family=null)
			{
				Name = name;
				Nation = nation;
				Family = family;
				age = 99;
			}
		}

		private void F0(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F0";
			mc[key] = "It's just a pure string";
			context.Response.Echo(key + ": " + (string)mc[key]);
		}

		private void F1(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F1";
			int num = 999;
			mc[key] = num.ToString();
			context.Response.Echo(key + ": " + (int.Parse(mc[key]) + 1));
		}

		private void F2(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F2";
			byte[] bytes = { 99, 88, 77 };
			mc[key] = JsonConvert.SerializeObject(bytes);
			bytes = JsonConvert.DeserializeObject<byte[]>(mc[key]);
			context.Response.Echo(key + " [1]: " + bytes[1]);
		}

		private void F3(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F3";
			Dictionary<string, object> d = new Dictionary<string, object>() {
				{"name", "Jack"},
				{"age", 100},
				{"nation", "USA"},
				{"favourite", new Dictionary<string, object>() { {"book", "the Bible"}, {"number", "6"}, {"animal", "dog"} }}
			};
			mc[key] = JsonConvert.SerializeObject(d);
			d = JsonConvert.DeserializeObject<Dictionary<string, object>>(mc[key]);
			Dictionary<string, object> d2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(d["favourite"].ToString());
			context.Response.Echo("{0}: {1} like the number {2}", key, d["name"], d2["number"]);
		}

		private void F4(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F4";
			Dictionary<string, object> d = new Dictionary<string, object>() {
				{"name", "Tom"},
				{"age", 90},
				{"nation", "USA"},
				{"favourite", new Dictionary<string, object>() { {"book", "the Bible"}, {"number", "2"}, {"animal", "cat"} }}
			};
			mc.Set(key, d);
			d = (Dictionary<string, object>)mc.Get(key);
			Dictionary<string, object> d2 = (Dictionary<string, object>)d["favourite"];
			context.Response.Echo("{0}: {1} like the number {2}", key, d["name"], d2["number"]);
		}

		private void F5(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string[] keys = { "F0", "F1", "F2", "F3", "F4" };
			byte[][] ret = mc.MGetBytes(keys);
			context.Response.Echo("F5 ret key count: " + (ret == null ? 0 : ret.Length));
			if (ret != null) {
				for (int i = 0; i < ret.Length; i++)
					Logger.DEBUG("[{0}].Length: {1}", i, ret[i].Length);
			}
		}

		private void F6(IContext context)
		{
			/*
			 * NOTE：可应用在如点赞/投票等场景
			 */
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F6";
			mc.SetCounter(key, 6L);
			mc.Increment(key);
			mc.Increment(key, 8L);
			context.Response.Echo(key + ": " + mc.GetCounter(key));
		}

		private void F7(IContext context)
		{
			IKeyValueStore mc = (IKeyValueStore)context["mc"];
			string key = "F7";
			mc[key] = JsonConvert.SerializeObject(new C("Brian", "USA"));
			C c = JsonConvert.DeserializeObject<C>(mc[key]);
			context.Response.Echo(key + ": " + c.Name + ", Age+1: " + (c.Age + 1));
		}

		public void Do(IContext context)
		{
			F0(context);
			//F1(context);
			//F2(context);
			//F3(context);
			//F4(context);
			//F5(context);
			//F6(context);
			//F7(context);
			Logger.DEBUG("Memcached handle success.");
		}
	}
}
