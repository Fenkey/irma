using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using IRMAKit.Templates;

public class AppGenerator
{
	private string _samplePath;

	private string _appDir;

	private string _appName;

	private ITemplateManager _tm;

	public AppGenerator(string samplePath, string appDir, string appName)
	{
		this._samplePath = samplePath;
		this._appDir = appDir;
		this._appName = appName;
		this._tm = new VelocityTemplateManager(samplePath, 6000);
	}

	private void GenHandlePerformance()
	{
		string tpl = string.Format("{0}/SampleHandlePerformance.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/HandlePerformance.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenIpCheckAttribute()
	{
		string tpl = string.Format("{0}/SampleIpCheckAttribute.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/IpCheckAttribute.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenPermissionCheckAttribute()
	{
		string tpl = string.Format("{0}/SamplePermissionCheckAttribute.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/PermissionCheckAttribute.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenUserConfigHandler()
	{
		string tpl = string.Format("{0}/SampleUserConfigHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/UserConfigHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenLoginHandler()
	{
		string tpl = string.Format("{0}/SampleLoginHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/LoginHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenSecLoginHandler()
	{
		string tpl = string.Format("{0}/SampleSecLoginHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/SecLoginHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenLoginCheckAttribute()
	{
		string tpl = string.Format("{0}/SampleLoginCheckAttribute.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/LoginCheckAttribute.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenLoginCheckHandler()
	{
		string tpl = string.Format("{0}/SampleLoginCheckHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/LoginCheckHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenLogoutHandler()
	{
		string tpl = string.Format("{0}/SampleLogoutHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/LogoutHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenStaticHandler()
	{
		string tpl = string.Format("{0}/SampleStaticHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/StaticHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenCStaticHandler()
	{
		string tpl = string.Format("{0}/SampleCStaticHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/CStaticHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenIndexHandler()
	{
		string tpl = string.Format("{0}/SampleIndexHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/IndexHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenIndexHtml()
	{
		string tpl = string.Format("{0}/SampleIndexHtml.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/documents/html/n/index.html", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenJsonApiHandler()
	{
		string tpl = string.Format("{0}/SampleJsonApiHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/JsonApiHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenParamsCheckHandler()
	{
		string tpl = string.Format("{0}/SampleParamsCheckHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/ParamsCheckHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenNoBlockHandler()
	{
		string tpl = string.Format("{0}/SampleNoBlockHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/NoBlockHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRestApiHandler()
	{
		string tpl = string.Format("{0}/SampleRestApiHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RestApiHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenExceptionHandler()
	{
		string tpl = string.Format("{0}/SampleExceptionHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/ExceptionHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenFuseCheckHandler()
	{
		string tpl = string.Format("{0}/SampleFuseCheckHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/FuseCheckHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenFetcherHandler()
	{
		string tpl = string.Format("{0}/SampleFetcherHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/FetcherHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenQRHandler()
	{
		string tpl = string.Format("{0}/SampleQRHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/QRHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRefCHandler()
	{
		string tpl = string.Format("{0}/SampleRefCHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RefCHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRefRHandler()
	{
		string tpl = string.Format("{0}/SampleRefRHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RefRHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRefXHandler()
	{
		string tpl = string.Format("{0}/SampleRefXHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RefXHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenProxyHandler()
	{
		string tpl = string.Format("{0}/SampleProxyHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/ProxyHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenAutoProxyCHandler()
	{
		string tpl = string.Format("{0}/SampleAutoProxyCHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/AutoProxyCHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenAutoProxyRHandler()
	{
		string tpl = string.Format("{0}/SampleAutoProxyRHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/AutoProxyRHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenAutoProxyXHandler()
	{
		string tpl = string.Format("{0}/SampleAutoProxyXHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/AutoProxyXHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenCrossOriginHandler()
	{
		string tpl = string.Format("{0}/SampleCrossOriginHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/CrossOriginHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenDownloadHandler()
	{
		string tpl = string.Format("{0}/SampleDownloadHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/DownloadHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenMemcachedHandler()
	{
		string tpl = string.Format("{0}/SampleMemcachedHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/MemcachedHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenLocalApiHandler()
	{
		string tpl = string.Format("{0}/SampleLocalApiHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/LocalApiHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenIpCheckHandler()
	{
		string tpl = string.Format("{0}/SampleIpCheckHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/IpCheckHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenPermissionCheckHandler()
	{
		string tpl = string.Format("{0}/SamplePermissionCheckHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/PermissionCheckHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenPermissionCheckHandler2()
	{
		string tpl = string.Format("{0}/SamplePermissionCheckHandler2.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/PermissionCheckHandler2.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenDesHandler()
	{
		string tpl = string.Format("{0}/SampleDesHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/DesHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenAesHandler()
	{
		string tpl = string.Format("{0}/SampleAesHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/AesHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRsaHandler()
	{
		string tpl = string.Format("{0}/SampleRsaHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RsaHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenRequestParamsHandler()
	{
		string tpl = string.Format("{0}/SampleRequestParamsHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/RequestParamsHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenPseudoDNSHandler()
	{
		string tpl = string.Format("{0}/SamplePseudoDNSHandler.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/PseudoDNSHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenService()
	{
		string tpl = string.Format("{0}/SampleService.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Web/MyService.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenAssemblyInfo()
	{
		string tpl = string.Format("{0}/SampleAssemblyInfo.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/Properties/AssemblyInfo.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenMakefile()
	{
		string tpl = string.Format("{0}/SampleMakefile.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName},
			{"sourceFiles", "Properties/AssemblyInfo.cs Web/*.cs"}
		});
		string file = string.Format("{0}/Makefile", _appDir);
		// Note to encode the Makefile as ASCII instead of UTF-8
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.ASCII)) {
			sw.Write(source);
		}
	}

	private void GenConf()
	{
		string tpl = string.Format("{0}/Sample.conf.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/conf/{1}.conf", _appDir, _appName);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenStartSh()
	{
		string tpl = string.Format("{0}/start.sh.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/start.sh", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true)) {
			sw.Write(source);
		}
	}

	private void GenStopSh()
	{
		string tpl = string.Format("{0}/stop.sh.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/stop.sh", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true)) {
			sw.Write(source);
		}
	}

	private void GenReloadSh()
	{
		string tpl = string.Format("{0}/reload.sh.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/reload.sh", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true)) {
			sw.Write(source);
		}
	}

	private void GenXlogbaseSh()
	{
		string tpl = string.Format("{0}/xlogbase.sh.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/xlogbase.sh", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true)) {
			sw.Write(source);
		}
	}

	private void GenRuntimeSh()
	{
		string tpl = string.Format("{0}/runtime.sh.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}/runtime.sh", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true)) {
			sw.Write(source);
		}
	}

	private void GenWeb()
	{
		GenHandlePerformance();
		GenService();
		GenAssemblyInfo();
		GenMakefile();
		GenConf();

		GenIpCheckAttribute();
		GenPermissionCheckAttribute();
		GenUserConfigHandler();
		GenLoginHandler();
		GenSecLoginHandler();
		GenLoginCheckAttribute();
		GenLoginCheckHandler();
		GenLogoutHandler();
		GenStaticHandler();
		GenCStaticHandler();
		GenIndexHandler();
		GenIndexHtml();
		GenRequestParamsHandler();
		GenLocalApiHandler();
		GenIpCheckHandler();
		GenPermissionCheckHandler();
		GenPermissionCheckHandler2();
		GenJsonApiHandler();
		GenParamsCheckHandler();
		GenNoBlockHandler();
		GenRestApiHandler();
		GenPseudoDNSHandler();
		GenExceptionHandler();
		GenFuseCheckHandler();
		GenFetcherHandler();
		GenQRHandler();
		GenRefCHandler();
		GenRefRHandler();
		GenRefXHandler();
		GenProxyHandler();
		GenAutoProxyCHandler();
		GenAutoProxyRHandler();
		GenAutoProxyXHandler();
		GenCrossOriginHandler();
		GenDownloadHandler();
		GenMemcachedHandler();
		GenDesHandler();
		GenAesHandler();
		GenRsaHandler();

		GenStartSh();
		GenStopSh();
		GenReloadSh();
		GenXlogbaseSh();
		GenRuntimeSh();
	}

	private void GenMockAppCode()
	{
		// xxxMock.cs
		string tpl = string.Format("{0}/mock/SampleMock.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}Mock/App_Code/{1}Mock.cs", _appDir, _appName);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}

		// MockHandler.cs
		tpl = string.Format("{0}/mock/SampleMockHandler.tpl", _samplePath);
		source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		file = string.Format("{0}Mock/App_Code/MockHandler.cs", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenMockWebConfig()
	{
		string tpl = string.Format("{0}/mock/SampleWeb.config.tpl", _samplePath);
		string source = _tm.Render(tpl, new Dictionary<string, object>() {
			{"appName", _appName}
		});
		string file = string.Format("{0}Mock/Web.config", _appDir);
		using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8)) {
			sw.Write(source);
		}
	}

	private void GenMock()
	{
		GenMockAppCode();
		GenMockWebConfig();
	}

	public void Gen(int mock)
	{
		GenWeb();
		if (mock > 0)
			GenMock();
	}

	public static void Main(string[] args)
	{
		AppGenerator ag = new AppGenerator(args[0], args[1], args[2]);
		ag.Gen(int.Parse(args[3]));
	}
}
