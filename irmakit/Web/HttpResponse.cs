using System;
using System.IO;
using System.Web;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using IRMACore.Net;
using IRMACore.Lower;
using IRMAKit.Utils;

namespace IRMAKit.Web
{
	internal sealed class HttpResponse : IResponse
	{
		private bool disposed = false;

		private string os;

		private HttpRequest request;

		private Encoding encoding;

		private bool routineBreak = false;
		public bool RoutineBreak
		{
			get { return this.routineBreak; }
		}

		private Stream buffer;
		public byte[] Buffer
		{
			get {
				if (this.buffer == null)
					this.buffer = new MemoryStream();
				return ((MemoryStream)this.buffer).ToArray();
			}
		}

		private string cookiePath;
		public string CookiePath
		{
			set {
				if (value != null && value.StartsWith("/"))
					this.cookiePath = value;
			}
			get {
				if (this.cookiePath == null)
					this.cookiePath = this.request.SessionCookiePath;
				return this.cookiePath;
			}
		}

		private HttpCookieCollection cookies;
		public HttpCookieCollection Cookies
		{
			get {
				if (this.cookies == null)
					this.cookies = new HttpCookieCollection();
				return this.cookies;
			}
		}

		public void SetCookie(HttpCookie cookie)
		{
			if (cookies == null)
				cookies = new HttpCookieCollection();
			cookies.Set(cookie);
		}

		public void RemoveCookie(string cookieName)
		{
			if (cookies != null)
				cookies.Remove(cookieName);
		}

		private bool gzipEnable = false;
		public bool GZipEnable
		{
			set {
				if (this.os.Equals("linux"))
					this.gzipEnable = value;
			}
			get { return this.gzipEnable; }
		}

		private string contentType;
		public string ContentType
		{
			set {
				if (this.contentType == null && !string.IsNullOrEmpty(value)) {
					this.contentType = value;
					Http.AddResponseHeader("Content-Type", value);
				}
			}
			get { return this.contentType; }
		}

		private string cacheControl;
		public string CacheControl
		{
			set {
				if (this.cacheControl == null && !string.IsNullOrEmpty(value)) {
					this.cacheControl = value;
					Http.AddResponseHeader("Cache-Control", value);
				}
			}
			get { return this.cacheControl; }
		}

		private string expires;
		public string Expires
		{
			set {
				if (this.expires == null && !string.IsNullOrEmpty(value)) {
					this.expires = value;
					Http.AddResponseHeader("Expires", value);
				}
			}
			get { return this.expires; }
		}

		private string lastModified;
		public string LastModified
		{
			set {
				if (this.lastModified == null && !string.IsNullOrEmpty(value)) {
					this.lastModified = value;
					Http.AddResponseHeader("Last-Modified", value);
				}
			}
			get { return this.lastModified; }
		}

		private string eTag;
		public string ETag
		{
			set {
				if (this.eTag == null && !string.IsNullOrEmpty(value)) {
					this.eTag = value;
					Http.AddResponseHeader("ETag", value);
				}
			}
			get { return this.eTag; }
		}

		private string xFrameOptions;
		public string XFrameOptions
		{
			set {
				if (value == "DENY" || value == "SAMEORIGIN" || value.StartsWith("ALLOW-FROM ")) {
					this.xFrameOptions = value;
					Http.AddResponseHeader("X-Frame-Options", value);
				}
			}
			get { return this.xFrameOptions; }
		}

		public void AppendHeader(string header, string headerValue)
		{
			if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(headerValue))
				return;
			/*
			 * Don't touch the 'Content-Length', which will be
			 * calculated automatically in irmacall level.
			 */
			if (os.Equals("linux") && header.ToLower() == "content-length")
				return;
			Http.AddResponseHeader(header, headerValue);
		}

		public void ClearHeaders()
		{
			Http.ClearResponseHeaders();
		}

		private void AddHeaderOfCookies()
		{
			foreach (string c in Cookies) {
				System.Web.HttpCookie cookie = Cookies[c];
				StringBuilder sb = new StringBuilder();

				if (!string.IsNullOrEmpty(cookie.Name))
					sb.Append(cookie.Name).Append("=");
				if (cookie.Value != null)
					sb.Append(cookie.Value);
				if (!string.IsNullOrEmpty(cookie.Domain))
					sb.Append("; Domain=").Append(cookie.Domain);
				if (!string.IsNullOrEmpty(cookie.Path))
					sb.Append("; Path=").Append(cookie.Path);
				if (cookie.Expires != DateTime.MinValue) {
					DateTime dt = cookie.Expires;
					if ((dt < DateTime.MaxValue.AddDays(-1.0)) && (dt > DateTime.MinValue.AddDays(1.0)))
						dt = dt.ToUniversalTime();
					sb.Append("; Expires=").Append(dt.ToString("ddd, dd-MMM-yyyy HH':'mm':'ss 'GMT'", DateTimeFormatInfo.InvariantInfo));
				}
				if (cookie.Secure)
					sb.Append("; Secure");
				if (cookie.HttpOnly)
					sb.Append("; HttpOnly");

				Http.AddResponseHeader("Set-Cookie", sb.ToString());
			}
		}

		public void SendHeader()
		{
			AddHeaderOfCookies();
			Http.SendHeader();
		}

		public void BufferWrite(byte[] content, int offset=0)
		{
			if (content == null || content.Length <= 0 || offset < 0)
				return;
			if (buffer == null)
				buffer = new MemoryStream();
			try {
				buffer.Seek(offset, SeekOrigin.Begin);
				buffer.Write(content, 0, content.Length);
			} catch {}
		}

		public void BufferAppend(byte[] content)
		{
			if (content == null || content.Length <= 0)
				return;
			if (buffer == null)
				buffer = new MemoryStream();
			try {
				buffer.Write(content, 0, content.Length);
			} catch {}
		}

		public void BufferAppend(string content, Encoding encoding)
		{
			if (content == null || content.Length <= 0)
				return;
			if (buffer == null)
				buffer = new MemoryStream();
			byte[] bytes;
			try {
				bytes = encoding.GetBytes(content);
				buffer.Write(bytes, 0, bytes.Length);
			} finally {
				bytes = null;
			}
		}

		public void BufferReset()
		{
			if (buffer != null) {
				buffer.Close();
				buffer = null;
			}
		}

		public void BufferAppend(string content, string charset)
		{
			BufferAppend(content, Encoding.GetEncoding(charset));
		}

		public void BufferAppend(string content)
		{
			BufferAppend(content, encoding);
		}

		private const int zipL0 = 5*1024;
		private const int zipL1 = 500*1024;
		private void ZipCheck(ref byte[] content)
		{
			if (content.Length <= zipL0)
				return;
			if (!gzipEnable && content.Length < zipL1)
				return;
			if (request.AcceptEncoding == null || request.AcceptEncoding.ToLower().IndexOf("gzip") < 0)
				return;
			byte[] bytes = ICall.GZip(content);
			if (bytes != null && bytes.Length > 0 && bytes.Length < content.Length) {
				content = bytes;
				Http.AddResponseHeader("Content-Encoding", "gzip");
			}
			bytes = null;
		}

		public void Send(byte[] content)
		{
			if (content != null)
				Http.Send(content);
		}

		public void SendHttp(int resCode, bool noBody=true)
		{
			byte[] content = null;
			if (!noBody && buffer != null) {
				AddHeaderOfCookies();
				content = ((MemoryStream)buffer).ToArray();
				ZipCheck(ref content);
			}
			Http.SendHttp(resCode, content);
			content = null;
		}

		public void SendHttp(int resCode, byte[] content)
		{
			if (content != null && content.Length > 0) {
				AddHeaderOfCookies();
				ZipCheck(ref content);
				Http.SendHttp(resCode, content);
			}
		}

		public void SendHttp(int resCode, string content, Encoding encoding)
		{
			try {
				SendHttp(resCode, encoding.GetBytes(content));
			} catch {}
		}

		public void SendHttp(int resCode, string content, string charset)
		{
			try {
				Encoding encoding = Encoding.GetEncoding(charset);
				SendHttp(resCode, encoding.GetBytes(content));
			} catch {}
		}

		public void SendHttp(int resCode, string content)
		{
			SendHttp(resCode, content, encoding);
		}

		private string ReplaceCookies(ref string cookiesStr, int saveRemoteCookies)
		{
			string[] kvs = cookiesStr.Split(new char[]{';'});
			for (int i = 0; i < kvs.Length; i++) {
				string p = kvs[i].Trim().ToLower();
				if (p.StartsWith("domain="))
					kvs[i] = "Domain=";
				else if (p.StartsWith("path=")) {
					if (saveRemoteCookies == 1)
						kvs[i] = "Path=" + (CookiePath + request.OriAppLocation).Replace("//", "/");
					else
						kvs[i] = "Path=" + CookiePath;
				}
			}
			cookiesStr = string.Join(";", kvs);
			return cookiesStr;
		}

		public void SendProxy(int resCode, Dictionary<string, string> resHeaders, byte[] resBody, int saveRemoteCookies=0)
		{
			if (resCode < 0)
				return;
			/*
			 * NOTE:
			 * 1. saveRemoteCookies > 0时修改并透传保存远程cookie设置（0时完全不透传该设置）
			 * 2. 透传fetcher响应code及headers（cookie设置信息特殊，参考1）
			 * 3. 仅保留当前必要的cookie设置（当前其余headers信息无效）
			 * 4. 不对body及压缩处理作干预
			 */
			ClearHeaders();
			foreach (KeyValuePair<string, string> h in resHeaders) {
				string v = h.Value;
				if (h.Key.ToUpper() == "SET-COOKIE") {
					if (saveRemoteCookies == 0)
						continue;
					ReplaceCookies(ref v, saveRemoteCookies);
					int i = v.IndexOf(request.SessionCookieName + "=");
					if (i == 0 || (i > 0 && (v[i-1] == ' ' || v[i-1] == ';'))) {
						/*
						 * FIX: 典型场景
						 * 1. 本地要求登录且未登录、或之前登录已过期
						 * 2. 上述1情况下继续允许proxy远程完成登录
						 * 此时以远程session id为最终登录结果并透传返回，留意此时本地SID未改变，
						 * SendProxy()返回后针对session的本地化操作是以未登录方式存在的，不建议
						 */
						Cookies.Remove(request.SessionCookieName);
					}
				}
				AppendHeader(h.Key, v);
			}
			AddHeaderOfCookies();
			Http.SendHttp(resCode, resBody);
		}

		public int SendProxy(IFetcher fetcher, string url, IRequest req, string proxyParam, int saveRemoteCookies)
		{
			if (fetcher == null || string.IsNullOrEmpty(url) || req == null)
				return -1;
			int code = fetcher.Proxy(url, req, proxyParam);
			SendProxy(code, fetcher.ResHeaders, fetcher.ResBody, saveRemoteCookies);
			return code;
		}

		public int SendProxy(IFetcher fetcher, string url, IRequest req, string proxyParam)
		{
			return SendProxy(fetcher, url, req, proxyParam, 0);
		}

		public int SendProxy(IFetcher fetcher, string url, IRequest req, int saveRemoteCookies)
		{
			return SendProxy(fetcher, url, req, null, saveRemoteCookies);
		}

		public int SendProxy(IFetcher fetcher, string url, IRequest req)
		{
			return SendProxy(fetcher, url, req, null, 0);
		}

		public void Echo()
		{
			if (buffer != null) {
				AddHeaderOfCookies();
				byte[] content = ((MemoryStream)buffer).ToArray();
				ZipCheck(ref content);
				Http.Echo(content);
				content = null;
			}
		}

		public void Echo(byte[] content)
		{
			if (content != null && content.Length > 0) {
				AddHeaderOfCookies();
				ZipCheck(ref content);
				Http.Echo(content);
			}
		}

		public void Echo(string content, Encoding encoding)
		{
			try {
				Echo(encoding.GetBytes(content));
			} catch {}
		}

		public void Echo(string content, string charset)
		{
			try {
				Encoding encoding = Encoding.GetEncoding(charset);
				Echo(encoding.GetBytes(content));
			} catch {}
		}

		public void Echo(string content)
		{
			Echo(content, encoding);
		}

		/*
		 * FIX: 由于存在其他2个参数的重载方法，当args是单个参数时，可能会被理解
		 * 为其他重载方法而导致Echo出错，例如：Echo("Welcome {0} !", name);
		 * 此时应该修正为该方式：Echo("Welcome {0} !", new object[] { name });
		 */
		public void Echo(string content, params object[] args)
		{
			if (content != null)
				Echo(string.Format(content, args), encoding);
		}

		public void NotModified()
		{
			SendHttp(304);
		}

		public void Unauthorized(string realm=null)
		{
			if (!string.IsNullOrEmpty(realm))
				Http.AddResponseHeader("WWW-Authenticate", "Basic realm=\"" + realm + "\"");
			SendHttp(401);
		}

		public void Forbidden()
		{
			SendHttp(403);
		}

		public void NotFound(bool accessNotFound=false)
		{
			if (accessNotFound)
				throw new AccessNotFoundException(request.OriAppLocation);
			SendHttp(404);
		}

		public void Redirect(string locationUrl, bool routineBreak=true)
		{
			if (!string.IsNullOrEmpty(locationUrl))
				Http.Redirect(locationUrl);
			if (routineBreak)
				this.routineBreak = true;
		}

		public void Download(string file, string fileName=null)
		{
			if (string.IsNullOrEmpty(file))
				return;
			if (string.IsNullOrEmpty(fileName))
				fileName = Path.GetFileName(file);
			FileInfo fi = new FileInfo(file);
			if (fi.Length <= 0)
				return;
			byte[] content = new byte[fi.Length];
			try {
				using (FileStream fs = fi.OpenRead()) {
					int offset = 0;
					long len = fi.Length;
					do {
						int n = fs.Read(content, offset, (int)len);
						if (n > 0) {
							offset += n;
							len -= n;
						}
					} while (len > 0)
						;
				}
				Http.AddResponseHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
				// ContentType、GZipEnable 请在此前根据需要设置好
				Echo(content);
			} finally {
				content = null;
			}
		}

		public void Download(byte[] content, string fileName)
		{
			if (content == null || content.Length <= 0)
				return;
			if (string.IsNullOrEmpty(fileName))
				return;
			Http.AddResponseHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
			// ContentType、GZipEnable 请在此前根据需要设置好
			Echo(content);
		}

		public HttpResponse(string os, HttpRequest request)
		{
			this.os = os;
			this.request = request;
			this.encoding = request.Encoding;
		}

		public void Dispose()
		{
			if (disposed)
				return;

			if (buffer != null) {
				buffer.Close();
				buffer = null;
			}
			cookies = null;

			disposed = true;
			GC.SuppressFinalize(this);
		}

		~HttpResponse()
		{
			Dispose();
		}
	}
}
