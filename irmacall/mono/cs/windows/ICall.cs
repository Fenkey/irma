using System;
using System.IO;
using System.Web;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/*
关于Windows环境下的mock方式
原理：通过构建的mock网站将请求信息过渡及提供给实际开发的项目（Foo），由后者所配置的路由及实现的处理器
去处理请求；同时，后者所响应的信息进一步通过mock网站反馈给用户请求端

1. 创建mock驱动项目: vs工具创建一个网站项目FooMock（文件->新建->网站->选择Virtual C#模板内ASP.Net空网站）

2. 修改Web.config配置：
<configuration>
...
<system.webServer>
	<handlers>
	<add name="ProcessRequest" path="*" verb="*" type="FooMock.MockHandler" />
	</handlers>
</system.webServer>
</configuration>

3. 添加库引用：
.
├── Foo.dll //选择和引用正在开发及待跟踪的项目Foo编译结果
├── IRMACore.dll
└── IRMAKit.dll

4. 添加source，FooMock内创建文件夹App_Code，加入源文件App_Code\FooMock.cs
...
using IRMACore.Lower;
using Foo.Web; // Foo为正在开发及待跟踪项目

namespace FooMock
{
	public class FooMock
	{
		public static MyService Service; // MyService 为Foo项目内继承于 IRMAKit.Service 的服务类
		public static void AppInitialize()
		{
			FooMock.Service = new MyService();
			FooMock.Service.Init("C:\\project\\Foo\\conf\\Foo.conf", ref ICall.GlobalObject); // 引用了Foo项目内配置
		}
	}
}

5. App_Code\MockHandler.cs
...
using System.Web;
using IRMACore.Lower;

namespace FooMock
{
	public class MockHandler : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext context)
		{
			FooMock.Service.Handle();
		}
	}
}

6. 可通过irma-genapp工具在生成项目的同时生成对应的mock项目，例如（生成Foo / FooMock）：irma-genapp Foo -m

备注：由于windows vs工具可直接通过端口启动应用（即不需要指定和配置应用名称，例如通过Chrome启动：http://localhost:<port>），
确定采用该方式进行DEBUG的话，可将conf文件system配置内的app_name设置为空（"app_name": ""）；如果已明确通过iis配置了应用名称、
则必须要求app_name非空、且与实际配置情况一样
*/

namespace IRMACore.Lower
{
	public abstract class ICall
	{
		// windows mock方式下需要public引用GolbalObject进行服务初始化
		public static object GlobalObject;

		// for Fetcher
		private static HttpClient fetcher = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip|DecompressionMethods.Deflate });
		private static MultipartFormDataContent fetcherMultiContent;
		private static HttpResponseMessage fetcherResponse;
		private static byte[] fetcherResBody = null;
		private static string fetcherError;

		// for System
		public static void LaunchInfo(string appName, string version, long bodyMax, string url)
		{
		}

		public static void Launched()
		{
		}

		// for Log
		private static void Log(string dir, string content)
		{
			try {
				string logDir = ICall.GetCurrentPath() + "log";
				if (Directory.Exists(logDir) == false)
					Directory.CreateDirectory(logDir);

				logDir += "\\" + dir;
				if (Directory.Exists(logDir) == false)
					Directory.CreateDirectory(logDir);

				string filePath = logDir + "\\irma_" + DateTime.Now.ToString("yyyyMMddHH") + ".log";
				File.AppendAllText(filePath, DateTime.Now.ToString("[HH:mm:ss,ffffff] ") + content + "\r\n");
			} catch {}
		}

		public static void LogDebug(string content)
		{
			Log("debug", content);
		}

		public static void LogEvent(string content)
		{
			Log("event", content);
		}

		public static void LogWarn(string content)
		{
			Log("warn", content);
		}

		public static void LogError(string content)
		{
			Log("error", content);
		}

		public static void LogFatal(string content)
		{
			Log("fatal", content);
		}

		public static void LogTc(string content)
		{
			Log("tc", content);
		}

		// for Http
		public static void HandleUnlock()
		{
		}

		public static object GetGlobalObject()
		{
			return ICall.GlobalObject;
		}

		public static int RequestIsMock()
		{
			return 0;
		}

		public static int RequestAccept()
		{
			HttpContext.Current.Response.ClearHeaders();
			HttpContext.Current.Response.ClearContent();
			return HttpContext.Current.Request == null ? 0 : 1;
		}

		public static bool FuseCheck(string handler)
		{
			return false;
		}

		public static void OnceOver()
		{
		}

		public static string GetRequestMethod()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.HttpMethod;
		}

		public static string GetRequestUri()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.RawUrl;
		}

		public static string GetRequestQueryString()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.ServerVariables.Get("QUERY_STRING");
		}

		public static string GetRequestContentType()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.ServerVariables.Get("CONTENT_TYPE");
		}

		public static string GetAllRequestHeaders()
		{
			HttpRequest req = HttpContext.Current.Request;
			StringBuilder sb = new StringBuilder();
			foreach (string key in req.ServerVariables.AllKeys) {
				if (key.StartsWith("HTTP_"))
					sb.Append(key + "=" + req.ServerVariables.Get(key) + "\r\n");
			}
			return sb.ToString();
		}

		public static string GetRequestParam(string paramName)
		{
			HttpRequest req = HttpContext.Current.Request;
			if (paramName.Equals("REQUEST_URI"))
				return req.RawUrl;
			else if (paramName.Equals("SERVER_ADDR"))
				paramName = "LOCAL_ADDR";
			else if (paramName.Equals("SCRIPT_FILENAME"))
				paramName = "SCRIPT_NAME";
			else if (paramName.Equals("HTTP_SSL_DN"))
				paramName = "CERT_SUBJECT";
			return req.ServerVariables.Get(paramName);
		}

		public static int GetRequestGetParamsCount()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.QueryString == null ? 0 : req.QueryString.Count;
		}

		public static byte[] GetRequestGetParam(int index, ref string paramName)
		{
			HttpRequest req = HttpContext.Current.Request;
			if (req.QueryString == null || index < 0 || index >= req.QueryString.Count)
				return null;
			paramName = req.QueryString.AllKeys[index];
			return Encoding.UTF8.GetBytes(req.QueryString[index]);
		}

		public static int GetRequestPostParamsCount()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.Form == null ? 0 : req.Form.Count;
		}

		public static byte[] GetRequestPostParam(int index, ref string paramName)
		{
			HttpRequest req = HttpContext.Current.Request;
			if (req.Form == null || index < 0 || index >= req.Form.Count)
				return null;
			paramName = req.Form.AllKeys[index];
			return Encoding.UTF8.GetBytes(req.Form[index]);
		}

		public static int GetRequestFileParamsCount()
		{
			HttpRequest req = HttpContext.Current.Request;
			return req.Files == null ? 0 : req.Files.Count;
		}

		public static byte[] GetRequestFileParam(int index, ref string paramName, ref string fileName, ref string contentType)
		{
			HttpRequest req = HttpContext.Current.Request;
			if (req.Files == null || index < 0 || index >= req.Files.Count)
				return null;
			paramName = req.Files.AllKeys[index];
			HttpPostedFile f = req.Files[index];
			fileName = f.FileName;
			contentType = f.ContentType;

			Stream s = f.InputStream;
			byte[] bytes = new byte[s.Length];
			s.Read(bytes, 0, bytes.Length);
			s.Seek(0, SeekOrigin.Begin);
			return bytes;
		}

		public static byte[] GetRequestBody()
		{
			HttpRequest req = HttpContext.Current.Request;
			Stream s = req.InputStream;
			byte[] bytes = new byte[s.Length];
			s.Read(bytes, 0, bytes.Length);
			s.Seek(0, SeekOrigin.Begin);
			return bytes;
		}

		public static byte[] RequestDump()
		{
			return null;
		}

		public static void AddResponseHeader(string header, string headerValue)
		{
			// 例如Set-Cookie是允许多次设置的，故不能考虑通过Dictionary<>方式中间承接header信息，而是直接AddHeader
			string h = header.ToLower();
			if (h != "transfer-encoding" && h != "content-length") {
				HttpResponse res = HttpContext.Current.Response;
				res.AddHeader(header, headerValue);
			}
		}

		public static void ClearResponseHeaders()
		{
			// FIX: 不建议采用res.Clear()方式，而是明确分开两个clear动作（特别是在proxy情况下有差异）
			HttpResponse res = HttpContext.Current.Response;
			res.ClearHeaders();
			res.ClearContent();
		}

		public static void SendHeader()
		{
		}

		public static void Redirect(string location)
		{
			HttpResponse res = HttpContext.Current.Response;
			/*
			 * FIX：如果指定true为endResponse参数，此方法调用End原始请求，将引发方法ThreadAbortException完成时的异常。
			 * 此异常有不利的影响对 Web 应用程序性能，这正是传递false为endResponse建议参数
			 * -- from MSDN
			 */
			res.Redirect(location, false);
			res.Flush();
		}

		public static void Send(byte[] content)
		{
			// Not support.
		}

		public static void SendHttp(int resCode, byte[] content)
		{
			// StatusCode在AddHeader之前设置好
			HttpResponse res = HttpContext.Current.Response;
			res.StatusCode = resCode;
			if (content != null && content.Length > 0) {
				res.AddHeader("Content-Length", content.Length.ToString());
				res.BinaryWrite(content);
			}
			res.Flush();
		}

		public static void Echo(byte[] content)
		{
			ICall.SendHttp(200, content);
		}

		public static int SmtpMail(string server, string user, string password, string subject, string to, string content, string a0, string a1, string a2, int hideTo, ref string error)
		{
			return -1;
		}

		public static int FetcherAppendHeader(string header)
		{
			try {
				string[] h = header.Split(new char[] { ':' }, 2);
				fetcher.DefaultRequestHeaders.Add(h[0], h[1]);
			} catch {
				return -1;
			}
			return 0;
		}

		public static int FetcherClearHeaders()
		{
			fetcherError = null;
			fetcherResBody = null;
			fetcherResponse = null;
			fetcher.DefaultRequestHeaders.Clear();
			return 0;
		}

		public static int FetcherAppendFormPostKv(string k, string v)
		{
			try {
				ByteArrayContent bc = new ByteArrayContent(Encoding.UTF8.GetBytes(v));
				bc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name=k };
				fetcherMultiContent.Add(bc);
				return 0;
			} catch {
				return -1;
			}
		}

		public static int FetcherAppendFormPostFile(string name, string file, string contentType)
		{
			try {
				return FetcherAppendFormPostFileBuf(name, Path.GetFileName(file), File.ReadAllBytes(file), contentType);
			} catch {
				return -1;
			}
		}

		public static int FetcherAppendFormPostFileBuf(string name, string file, byte[] body, string contentType)
		{
			try {
				ByteArrayContent bc = new ByteArrayContent(body);
				bc.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
				bc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name=(name == null) ? file : name, FileName=file};
				fetcherMultiContent.Add(bc);
				return 0;
			} catch {
				return -1;
			}
		}

		public static int FetcherClearFormPost()
		{
			fetcherMultiContent = null;
			fetcherMultiContent = new MultipartFormDataContent();
			return 0;
		}

		public static int FetcherGet(string url, int timeout)
		{
			try {
				// 只能在第一次请求前设置timeout
				// fetcher.Timeout = TimeSpan.FromSeconds(timeout);
				System.Threading.Tasks.Task<HttpResponseMessage> res = fetcher.GetAsync(url);
				res.Wait();
				fetcherResponse = res.Result;
				return (int)fetcherResponse.StatusCode;
			} catch (Exception e) {
				fetcherError = e.Message;
				return -1;
			}
		}

		public static int FetcherPost(string url, byte[] body, int timeout)
		{
			try {
				// 只能在第一次请求前设置timeout
				// fetcher.Timeout = TimeSpan.FromSeconds(timeout);
				using (MemoryStream ms = new MemoryStream(body)) {
					using (HttpContent content = new StreamContent(ms)) {
						content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
						System.Threading.Tasks.Task<HttpResponseMessage> res = fetcher.PostAsync(url, content);
						res.Wait();
						fetcherResponse = res.Result;
					}
				}
				return (int)fetcherResponse.StatusCode;
			} catch (Exception e) {
				fetcherError = e.Message;
				return -1;
			}
		}

		public static int FetcherPostForm(string url, int timeout)
		{
			try {
				// 只能在第一次请求前设置timeout
				// fetcher.Timeout = TimeSpan.FromSeconds(timeout);
				System.Threading.Tasks.Task<HttpResponseMessage> res = fetcher.PostAsync(url, fetcherMultiContent);
				res.Wait();
				fetcherResponse = res.Result;
				return (int)fetcherResponse.StatusCode;
			} catch (Exception e) {
				fetcherError = e.Message;
				return -1;
			}
		}

		public static int FetcherPut(string url, byte[] body, int timeout)
		{
			try {
				// 只能在第一次请求前设置timeout
				// fetcher.Timeout = TimeSpan.FromSeconds(timeout);
				using (MemoryStream ms = new MemoryStream(body)) {
					using (HttpContent content = new StreamContent(ms)) {
						content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
						System.Threading.Tasks.Task<HttpResponseMessage> res = fetcher.PutAsync(url, content);
						res.Wait();
						fetcherResponse = res.Result;
					}
				}
				return (int)fetcherResponse.StatusCode;
			} catch (Exception e) {
				fetcherError = e.Message;
				return -1;
			}
		}

		public static int FetcherDelete(string url, int timeout)
		{
			try {
				//fetcher.Timeout = TimeSpan.FromSeconds(timeout);
				System.Threading.Tasks.Task<HttpResponseMessage> res = fetcher.DeleteAsync(url);
				res.Wait();
				fetcherResponse = res.Result;
				return (int)fetcherResponse.StatusCode;
			} catch (Exception e) {
				fetcherError = e.Message;
				return -1;
			}
		}

		public static byte[] FetcherResBody()
		{
			if (fetcherResBody == null && fetcherResponse != null) {
				System.Threading.Tasks.Task<byte[]> res = fetcherResponse.Content.ReadAsByteArrayAsync();
				res.Wait();
				fetcherResBody = res.Result;
			}
			return fetcherResBody;
		}

		public static string FetcherResHeaders()
		{
			string headers = string.Empty;
			if (fetcherResponse == null)
				return headers;
			// 响应头部来源于两部分（和内容相关的头部、常规头部）
			IEnumerator<KeyValuePair<string, IEnumerable<string>>> kv = fetcherResponse.Headers.GetEnumerator();
			while (kv.MoveNext())
				headers += string.Format("{0}:{1}\r\n", kv.Current.Key, string.Join(";", kv.Current.Value.ToList()));
			kv = fetcherResponse.Content.Headers.GetEnumerator();
			while (kv.MoveNext())
				headers += string.Format("{0}:{1}\r\n", kv.Current.Key, string.Join(";", kv.Current.Value.ToList()));
			return headers;
		}

		public static string FetcherError()
		{
			return fetcherError;
		}

		public static double FetcherTimeUsed()
		{
			return 0.0;
		}

		// for Sys
		public static string GetOS()
		{
			return "windows";
		}

		public static int GetWorkerIndex()
		{
			return 0;
		}

		public static long GetUnixTime()
		{
			DateTime st = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
			DateTime nt = DateTime.Now;
			return (long)Math.Round((nt - st).TotalMilliseconds / 1000, MidpointRounding.AwayFromZero);
		}

		public static long BuildUnixTime(int y, int m, int d, int h, int M, int s)
		{
			DateTime st = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
			DateTime nt = new DateTime(y, m, d, h, M, s);
			return (long)Math.Round((nt - st).TotalMilliseconds / 1000, MidpointRounding.AwayFromZero);
		}

		public static long BuildGmTime(string str)
		{
			DateTime st = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
			DateTime nt;
			bool s = DateTime.TryParseExact(str, "r", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out nt);
			return s ? (long)Math.Round((nt - st).TotalMilliseconds / 1000, MidpointRounding.AwayFromZero) : -1;
		}

		public static long UnixTimeToGmTime(long unixTime, ref string gmtStr)
		{
			DateTime st = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
			DateTime nt = st.AddSeconds(unixTime);
			if ((nt < DateTime.MaxValue.AddDays(-1.0)) && (nt > DateTime.MinValue.AddDays(1.0)))
				nt = nt.ToUniversalTime();
			gmtStr = nt.ToString("r");
			return (long)Math.Round((nt - st).TotalMilliseconds / 1000, MidpointRounding.AwayFromZero);
		}

		public static string GetCurrentPath()
		{
			/*
			HttpRequest req = HttpContext.Current.Request;
			return req.MapPath("/");
			*/
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static string ShellExecute(string cmd)
		{
			return null;
		}

		// for DES
		public static byte[] DesEcbEncrypt(string key, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] DesEcbDecrypt(string key, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] DesCbcEncrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] DesCbcDecrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] DesNCbcEncrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] DesNCbcDecrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		// for AES
		public static byte[] AesEcbEncrypt(string key, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] AesEcbDecrypt(string key, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] AesCbcEncrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		public static byte[] AesCbcDecrypt(string key, string iv, byte[] content, int pType)
		{
			return content;
		}

		// for RSA
		public static byte[] RsaEncrypt(string keyFile, string keyPwd, string content)
		{
			return null;
		}

		public static string RsaDecrypt(string keyFile, string keyPwd, byte[] content)
		{
			return null;
		}

		public static byte[] RsaSign(int type, string keyFile, string keyPwd, string content)
		{
			return null;
		}

		public static bool RsaVerify(int type, string keyFile, string keyPwd, string content, byte[] sign)
		{
			return true;
		}

		public static byte[] RsaMemEncrypt(string key, string keyPwd, string content)
		{
			return null;
		}

		public static string RsaMemDecrypt(string key, string keyPwd, byte[] content)
		{
			return null;
		}

		public static byte[] RsaMemSign(int type, string key, string keyPwd, string content)
		{
			return null;
		}

		public static bool RsaMemVerify(int type, string key, string keyPwd, string content, byte[] sign)
		{
			return true;
		}

		// for Kvs
		public static long MemcachedNew(string servers, string instance, long zipMin)
		{
			return -1L;
		}

		public static long RedisNew(string server, int port, string instance)
		{
			return -1L;
		}

		public static bool KvsExpire(long L, string key, long expire)
		{
			return false;
		}

		public static bool KvsExists(long L, string key)
		{
			return false;
		}

		public static bool KvsDelete(long L, string key)
		{
			return false;
		}

		public static bool KvsSetNx(long L, string key, byte[] value, long expire)
		{
			return false;
		}

		public static bool KvsSetEx(long L, string key, byte[] value, long expire)
		{
			return false;
		}

		public static bool KvsSet(long L, string key, byte[] value, long expire)
		{
			return false;
		}

		public static bool KvsAdd(long L, string key, byte[] value, long expire)
		{
			return false;
		}

		public static bool KvsReplace(long L, string key, byte[] value, long expire)
		{
			return false;
		}

		public static bool KvsPrepend(long L, string key, byte[] value)
		{
			return false;
		}

		public static bool KvsAppend(long L, string key, byte[] value)
		{
			return false;
		}

		public static byte[] KvsGet(long L, string key)
		{
			return null;
		}

		public static byte[][] KvsMGet(long L, string[] keys)
		{
			return null;
		}

		public static bool KvsSetCounter(long L, string key, long value)
		{
			return false;
		}

		public static long KvsGetCounter(long L, string key)
		{
			return -1L;
		}

		public static long KvsIncr(long L, string key, long value)
		{
			return -1L;
		}

		public static long KvsDecr(long L, string key, long value)
		{
			return -1L;
		}

		public static long KvsLLen(long L, string key)
		{
			return -1L;
		}

		public static long KvsRPush(long L, string key, byte[][] vals)
		{
			return -1L;
		}

		public static byte[] KvsLPop(long L, string key)
		{
			return null;
		}

		// for Summary
		public static string Md5(byte[] content)
		{
			try {
				System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
				byte[] b = md5.ComputeHash(content);
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < b.Length; i++)
					sb.Append(b[i].ToString("x2"));
				return sb.ToString();
			} catch {
				return null;
			}
		}

		public static string Sha1(byte[] content)
		{
			try {
				System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
				byte[] b = sha1.ComputeHash(content);
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < b.Length; i++)
					sb.Append(b[i].ToString("x2"));
				return sb.ToString();
			} catch {
				return null;
			}
		}

		public static string Sha256(byte[] content)
		{
			try {
				System.Security.Cryptography.SHA256 sha256 = new System.Security.Cryptography.SHA256CryptoServiceProvider();
				byte[] b = sha256.ComputeHash(content);
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < b.Length; i++)
					sb.Append(b[i].ToString("x2"));
				return sb.ToString();
			} catch {
				return null;
			}
		}

		public static string Sha512(byte[] content)
		{
			try {
				System.Security.Cryptography.SHA512 sha512 = new System.Security.Cryptography.SHA512CryptoServiceProvider();
				byte[] b = sha512.ComputeHash(content);
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < b.Length; i++)
					sb.Append(b[i].ToString("x2"));
				return sb.ToString();
			} catch {
				return null;
			}
		}

		// for Unit
		public static long UnitParseBytes(string value)
		{
			try {
				if (string.IsNullOrEmpty(value))
					return 0L;

				char u = char.ToLower(value[value.Length - 1]);
				if (u >= '0' && u <= '9')
					return long.Parse(value);

				long v = long.Parse(value.Substring(0, value.Length - 1));

				if (u == 'b')
					return v;
				else if (u == 'k')
					return v * 1024;
				else if (u == 'm')
					return v * 1024 * 1024;
				else if (u == 'g')
					return v * 1024 * 1024 * 1024;
				else
					return -1L;
			} catch {
				return -1L;
			}
		}

		public static long UnitParseSeconds(string value)
		{
			try {
				if (string.IsNullOrEmpty(value))
					return 0L;

				char u = char.ToLower(value[value.Length - 1]);
				if (u >= '0' && u <= '9')
					return long.Parse(value);

				long v = long.Parse(value.Substring(0, value.Length - 1));

				if (u == 's')
					return v;
				else if (u == 'm')
					return v * 60;
				else if (u == 'h')
					return v * 60 * 60;
				else if (u == 'd')
					return v * 60 * 60 * 24;
				else if (u == 'w')
					return v * 60 * 60 * 24 * 7;
				else if (u == 'M')
					return v * 60 * 60 * 24 * 30;
				else if (u == 'y')
					return v * 60 * 60 * 24 * 365;
				else
					return -1L;
			} catch {
				return -1L;
			}
		}

		// for GZip
		public static byte[] GZip(byte[] data)
		{
			// do nothing
			return data;
		}

		public static byte[] GUnZip(byte[] data)
		{
			// do nothing
			return data;
		}

		// for Ver
		public static int VerCmp(string v1, string v2, ref string error)
		{
			error = null;
			try {
				Version _v1 = new Version(v1);
				Version _v2 = new Version(v2);
				return _v1.CompareTo(_v2);
			} catch (Exception e) {
				error = e.Message;
				return -1;
			}
		}
	}
}
