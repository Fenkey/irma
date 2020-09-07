using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class JsonApiAttribute : ReqEndAttribute
    {
		private JsonSerializerSettings settings;
		public JsonSerializerSettings Settings
		{
			get {
				if (this.settings == null) {
					this.settings = new JsonSerializerSettings();
					this.settings.NullValueHandling = NullValueHandling.Ignore;
					this.settings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
					this.settings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
				}
				return this.settings;
			}

			set { this.settings = value; }
		}

        protected override void End(IContext context)
        {
            string json = "{\"success\": false}";
            Dictionary<string, object> d = (Dictionary<string, object>)context.FM["data"];
            if (d != null) {
                if (!d.ContainsKey("success"))
                    d["success"] = true;
                json = JsonConvert.SerializeObject(d, Settings);
            }
            context.Response.Echo(json);
        }
    }

	[JsonApi]
	public class JsonApiHandler : IHandler
	{
		public void Do(IContext context)
		{
			List<Dictionary<string, object>> kids = new List<Dictionary<string, object>>();

			Dictionary<string, object> k1 = new Dictionary<string, object>() {
				{"name", "Marie"},
				{"age", 10},
				{"child", "girl"}
			};
			kids.Add(k1);

			Dictionary<string, object> k2 = new Dictionary<string, object>() {
				{"name", "Baillie"},
				{"age", 8},
				{"child", "boy"}
			};
			kids.Add(k2);

			Dictionary<string, object> k3 = new Dictionary<string, object>() {
				{"name", "Kaley"},
				{"age", 6},
				{"child", "girl"}
			};
			kids.Add(k3);

			Dictionary<string, object> d = new Dictionary<string, object>() {
				{"name", "Tom"},
				{"birth", DateTime.Now},
				{"nation", "USA"},
				{"kids", kids}
			};

			context.FM["data"] = d;

			Logger.DEBUG("Json api handle success.");
		}
	}
}
