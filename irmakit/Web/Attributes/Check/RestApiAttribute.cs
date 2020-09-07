using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using IRMAKit.Configure;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
	public class RestApiAttribute : ReqCheckAttribute
	{
		private Regex patternRex;
		private string pattern;
		public string Pattern
		{
			set { this.pattern = value; }
			get { return this.Pattern; }
		}

		private string[] paramsArr;
		private string _params;
		public string Params
		{
			set { this._params = value; }
			get { return this.Params; }
		}

		public RestApiAttribute()
		{
			this.Cache = false;
		}

		public RestApiAttribute(string pattern, string _params=null)
		{
			this.Cache = false;
			this.pattern = pattern;
			this._params = _params;
		}

		public override bool Init(IConfig config, string handlerName, byte methods)
		{
			try {
				if (string.IsNullOrEmpty(pattern))
					throw new Exception();
				patternRex = new Regex(pattern, RegexOptions.Compiled);
			} catch {
				Logger.WARN("Kit - Invalid RestApi Pattern in handler '{0}': '{1}'", handlerName, pattern);
				return false;
			}
			if (!string.IsNullOrEmpty(_params))
				paramsArr = _params.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
			return true;
		}

		protected override bool Check(IContext context)
		{
			Match m = this.patternRex.Match(context.Request.AppLocation);
			if (!m.Success)
				return false;
			IRequest req = context.Request;
			if (req.RestParams == null)
				req.RestParams = new NameValueCollection();
			if (paramsArr != null) {
				for (int i = 1; i < m.Groups.Count && i <= paramsArr.Length; i++)
					req.RestParams.Set(paramsArr[i-1], m.Groups[i].ToString().Trim());
			}
			return true;
		}
	}
}
