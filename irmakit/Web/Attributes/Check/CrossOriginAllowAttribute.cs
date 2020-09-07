using System;
using System.Collections.Generic;
using IRMAKit.Configure;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=false)]
	public class CrossOriginAllowAttribute : ReqCheckAttribute
	{
		/*
		 * 设置为null时将自动返回当前请求的HTTP_ORIGIN
		 * 典型情况如移动端无法确定来源地址和端口等
		 */
		private string allowOrigin = "*";
		public string AllowOrigin
		{
			set { this.allowOrigin = value; }
			get { return this.allowOrigin; }
		}

		private string allowHeaders = "X-Requested-With,Origin,Content-Type";
		public string AllowHeaders
		{
			set { if (!string.IsNullOrEmpty(value)) this.allowHeaders = value; }
			get { return this.allowHeaders; }
		}

		private string allowMethods;
		public string AllowMethods
		{
			get { return this.allowMethods; }
		}

		private bool allowCredentials = false;
		public bool AllowCredentials
		{
			set { this.allowCredentials = value; }
			get { return this.allowCredentials; }
		}

		public override bool Init(IConfig config, string handlerName, byte methods)
		{
			List<string> lm = new List<string>();
			if ((methods & (byte)Method.HEAD) > 0)
				lm.Add("HEAD");
			if ((methods & (byte)Method.GET) > 0)
				lm.Add("GET");
			if ((methods & (byte)Method.POST) > 0)
				lm.Add("POST");
			if ((methods & (byte)Method.PUT) > 0)
				lm.Add("PUT");
			if ((methods & (byte)Method.SET) > 0)
				lm.Add("SET");
			if ((methods & (byte)Method.DELETE) > 0)
				lm.Add("DELETE");
			if ((methods & (byte)Method.OPTIONS) > 0)
				lm.Add("OPTIONS");
			allowMethods = string.Join(",", lm.ToArray());
			return true;
		}

		protected override bool Check(IContext context)
		{
			/*
			 * 存在如下跨域应用情况（"->"代表Ref引用跳转）：
			 * [CrossOriginAllow]A：允许
			 * [CrossOriginAllow]A -> B：允许
			 * [CrossOriginAllow]A -> [CrossOriginAllow]B：允许，采用的是A接口跨域属性
			 * A -> [CrossOriginAllow]B：不允许
			 * A -> B：不允许
			 * 综上，仅当首入接口挂载跨域属性情况下才生效（接口安全考虑）
			 */
			IRequest req = context.Request;
			if (!req.IsRef && req.Origin != null) {
				IResponse res = context.Response;
				res.AppendHeader("Access-Control-Allow-Origin", allowOrigin == null ? req.Origin : allowOrigin);
				res.AppendHeader("Access-Control-Allow-Headers", allowHeaders);
				res.AppendHeader("Access-Control-Allow-Methods", allowMethods);
				if (allowCredentials) {
					// FIX: true时如果AllowOrigin为null，则违反浏览器跨域访问规则而无效，但我们不作此判断和限制
					res.AppendHeader("Access-Control-Allow-Credentials", "true");
				}
			}
			return true;
		}
	}
}
