using System;
using System.Linq;
using System.Collections.Generic;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
	public class ParamsCheckAttribute : ReqCheckAttribute
	{
		private bool allowEmpty = false;
		public bool AllowEmpty
		{
			set { this.allowEmpty = value; }
			get { return this.allowEmpty; }
		}

		private string[] getMust;
		public string GetMust
		{
			set {
				try {
					this.getMust = value.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				} catch {
					Logger.WARN("Kit - Invalid ParamsCheck GetMust: '{0}'", value);
				}
			}
			get { return this.GetMust; }
		}

		private string[] postMust;
		public string PostMust
		{
			set {
				try {
					this.postMust = value.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				} catch {
					Logger.WARN("Kit - Invalid ParamsCheck PostMust: '{0}'", value);
				}
			}
			get { return this.PostMust; }
		}

		private string[] paramsMust;
		public string ParamsMust
		{
			set {
				try {
					this.paramsMust = value.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				} catch {
					Logger.WARN("Kit - Invalid ParamsCheck ParamsMust: '{0}'", value);
				}
			}
			get { return this.ParamsMust; }
		}

		private bool Check0(IContext context)
		{
			IRequest req = context.Request;

			if (getMust != null) {
				foreach (string p in getMust) {
					if (!req.GetParams.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Get parameter '{0}' is not found", p);
						return false;
					}
				}
			}
			if (postMust != null) {
				foreach (string p in postMust) {
					if (!req.PostParams.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Post parameter '{0}' is not found", p);
						return false;
					}
				}
			}
			if (paramsMust != null) {
				foreach (string p in paramsMust) {
					if (!req.Params.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Request parameter '{0}' is not found", p);
						return false;
					}
				}
			}
			return true;
		}

		private bool Check1(IContext context)
		{
			IRequest req = context.Request;

			if (getMust != null) {
				foreach (string p in getMust) {
					if (!req.GetParams.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Get parameter '{0}' is not found", p);
						return false;
					} else if (string.IsNullOrEmpty(req.GetParams.Get(p))) {
						Logger.WARN("Kit - Get parameter '{0}' is empty", p);
						return false;
					}
				}
			}
			if (postMust != null) {
				foreach (string p in postMust) {
					if (!req.PostParams.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Post parameter '{0}' is not found", p);
						return false;
					} else if (string.IsNullOrEmpty(req.PostParams.Get(p))) {
						Logger.WARN("Kit - Post parameter '{0}' is empty", p);
						return false;
					}
				}
			}
			if (paramsMust != null) {
				foreach (string p in paramsMust) {
					if (!req.Params.AllKeys.Contains(p)) {
						Logger.WARN("Kit - Request parameter '{0}' is not found", p);
						return false;
					} else if (string.IsNullOrEmpty(req.Params.Get(p))) {
						Logger.WARN("Kit - Request parameter '{0}' is empty", p);
						return false;
					}
				}
			}
			return true;
		}

		protected override bool Check(IContext context)
		{
			return allowEmpty ? Check0(context) : Check1(context);
		}
	}
}
