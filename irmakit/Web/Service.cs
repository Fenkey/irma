using System;
using System.Threading;
using System.Collections.Generic;
using IRMACore.Net;
using IRMACore.Lower;
using IRMAKit.Log;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	public class Service
	{
		private Context context;

		private Dispatcher dispatcher;

		private static object mutex = new object();

		/*
		 * FIX:
		 * Don't use CurrentContext unless SystemInit() is finished !
		 * Especially the case that the class member is static, for example:
		 * private static string name = Service.CurrentContext.User.Value<string>("name");
		 */
		public static IContext CurrentContext
		{
			get {
				try {
					return (IContext)Http.GetGlobalObject();
				} catch {
					return null;
				}
			}
		}

		private void KeepAlive(ref object globalObj)
		{
			if (context != null)
				globalObj = context;
		}

		private void SystemInit(string configFile, ref object globalObj)
		{
			Dictionary<string, object> opt;
			Config config = new Config(configFile, new LoadRoutersJson(this.LoadRouters), new LoadUserJson(this.LoadUser), out opt);
			// NOTE to assign it aeap.
			globalObj = context = new Context(config);
			ConfigAdjustOpt(context, ref opt);
			context.ConfigAdjust(opt);
			dispatcher = new Dispatcher(config);
		}

		protected virtual string LoadRouters(IConfig config)
		{
			/*
			 * Return the JSON string that refers to the 'routers' setting in conf file. For example:
			 * return "[{\"path\":\"^/$\",\"handler\":\"Foo.Web.IndexHandler\",\"methods\": \"*\",\"pf\":true}]";
			 */
			return null;
		}

		protected virtual string LoadUser(IConfig config)
		{
			/*
			 * Return the JSON string that refers to the 'user' setting in conf file. For example:
			 * return "{\"name\":\"FENKEY\"}";
			 */
			return null;
		}

		protected virtual void ConfigAdjustOpt(IContext context, ref Dictionary<string, object> opt)
		{
			/*
			 * Adjust the configuration, for example:
			 * if (context.OS == "windows") opt["app_charset"] = "GB2312";
			 */
		}

		protected virtual void AppInit(IContext context)
		{
		}

		private void Init(string configFile, ref object globalObj, bool succLog)
		{
			try {
				SystemInit(configFile, ref globalObj);
				AppInit(context);
			} catch (Exception e) {
				Logger.FATAL("Kit - Service init failed: {0}", e.Message);
				throw e;
			}
			ICall.Launched();
			if (succLog)
				Logger.EVENT("Kit - Service start ({0})", ICall.GetWorkerIndex());
		}

		// FIX: It's public because it might be invoked directly in windows version.
		public void Init(string configFile, ref object globalObj)
		{
			Init(configFile, ref globalObj, true);
		}

		protected virtual bool AppReloadPermit(IContext context)
		{
			return true;
		}

		protected virtual void AppReloadEnd(IContext context)
		{
		}

		private void Reload(string configFile, ref object globalObj)
		{
			if (AppReloadPermit(context)) {
				Logger.EVENT("Kit - Service reloading ...");
				Finalize(false);
				context = null;
				Init(configFile, ref globalObj, false);
				AppReloadEnd(context);
				Logger.EVENT("Kit - Service reload successfully ({0})", ICall.GetWorkerIndex());
			} else
				ICall.Launched();
		}

		protected virtual void AccessNotFound(IContext context)
		{
			context.Response.NotFound();
		}

		protected virtual void ServiceInvalid(IContext context, Exception e)
		{
			context.Response.Echo("Sorry, service is invalid :-(");
		}

		protected virtual void DoProxy(IContext context, string location, string param)
		{
			/*
			 * DoProxy实现所有[Proxy]标注的对外路由和请求，应用层通常以如下方式重载：
			 * 1. 依据param参数确定该选择代理请求的服务端域名或iP
			 * 2. 组合上述1及location信息，确定最终请求的url
			 * 3. 考虑是否cache优先等措施，提取数据并响应
			 * 4. 采用IFetcher.Proxy()方法透传/代理请求url
			 * 5. 采用IResponse.SendProxy()方法响应上述4结果
			 *
			 * 参考：irma-genapp所生成sample项目内Web/MyService.cs
			 */
			context.Response.Forbidden();
		}

		protected virtual void BeforeDispatch(IContext context)
		{
		}

		protected virtual void AfterDispatch(IContext context)
		{
		}

		private void HandleOnce()
		{
			GC.KeepAlive(context);

			if (Http.CaptureEvent() == 0) {
				Http.OnceOver();
				return;
			}

			try {
				BeforeDispatch(context);

				try {
					dispatcher.Dispatch(context);
				} catch (AccessNotFoundException) {
					AccessNotFound(context);
					return;
				} catch (MethodNotAllowedException e) {
					context.Response.AppendHeader("Allow", e.MAllow);
					context.Response.SendHttp(405);
					Logger.WARN("Kit - Method isn't allowed for handler '{0}': {1}", context.HandlerName, e.Method);
					return;
				} catch (GoToProxyException e) {
					DoProxy(context, e.Location, e.Param);
					return;
				} catch (BodyIsEmptyException e) {
					context.Response.SendHttp(400);
					Logger.WARN("Kit - Request body is empty for handler '{0}': {1}", context.HandlerName, e.Method);
					return;
				} catch (ThreadAbortException) {
					Thread.ResetAbort();
					Logger.WARN("Kit - Catch thread abort exception and ignore it");
				} catch (NullReferenceException e) {
					Logger.ERROR("Kit - Trouble GC in handler '{0}' :-(", context.HandlerName);
					context.Response.ClearHeaders();
					ServiceInvalid(context, e);
				} catch (Exception e) {
					Logger.ERROR("Kit - Catch exception in handler '{0}': {1}", context.HandlerName, e.Message);
					ServiceInvalid(context, e);
				}

				AfterDispatch(context);
			} catch (Exception e) {
				Logger.WARN("Kit - Catch exception out of handler: {0}", e.Message);
			} finally {
				Http.OnceOver();
				context.Refresh();
			}

			GC.KeepAlive(context);
		}

		// FIX: It's public because it might be invoked directly in windows version.
		public void Handle()
		{
			bool isMock = context.OS.Equals("windows");
			if (isMock) {
				lock (Service.mutex) { HandleOnce(); }
			} else {
				for (Http.HandleUnlock();;) HandleOnce();
			}
		}

		protected virtual void AppFinalize(IContext context)
		{
		}

		private void Finalize(bool succLog)
		{
			try {
				AppFinalize(context);
			} catch (NullReferenceException) {
				Logger.ERROR("Kit - Trouble GC in Finalize() :-(");
				return;
			} catch (Exception e) {
				Logger.WARN("Kit - Catch exception in Finalize(): {0}", e.Message);
				throw e;
			}
			if (succLog)
				Logger.EVENT("Kit - Service stop ({0})", ICall.GetWorkerIndex());
		}

		private void Finalize()
		{
			Finalize(true);
		}

		/*
		 * FIX: Compile it as an 'exe'
		 */
		public static void Main(string[] args)
		{
		}
	}
}
