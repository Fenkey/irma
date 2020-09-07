using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using IRMACore.Net;
using IRMAKit.Web;

namespace IRMAKit.Utils
{
	public sealed class Fetcher : IFetcher
	{
		private string url;
		public string Url
		{
			get { return this.url; }
		}

		private string method = "GET";
		public string Method
		{
			get { return this.method; }
		}

		private Encoding resEncoding = Encoding.UTF8;
		private string resCharset = "UTF-8";
		public string ResCharset
		{
			get { return this.resCharset; }

			set {
				try {
					this.resEncoding = Encoding.GetEncoding(value);
					this.resCharset = value;
				} catch {
					this.resEncoding = Encoding.UTF8;
					this.resCharset = "UTF-8";
				}
			}
		}

		private byte[] resBody;
		public byte[] ResBody
		{
			get {
				if (this.resBody == null)
					this.resBody = Http.FetcherResBody();
				return this.resBody;
			}
		}

		private string resText;
		public string ResText
		{
			get {
				if (this.resText == null) {
					try {
						this.resText = this.resEncoding.GetString(this.ResBody);
					} catch {
						this.resText = null;
					}
				}
				return this.resText;
			}
		}

		private JObject resJson;
		public JObject ResJson
		{
			get {
				if (this.resJson == null) {
					try {
						this.resJson = JObject.Parse(this.ResText);
					} catch {
						this.resJson = null;
					}
				}
				return this.resJson;
			}
		}

		private Dictionary<string, string> resHeaders;
		public Dictionary<string, string> ResHeaders
		{
			get {
				if (this.resHeaders == null)
					this.resHeaders = Http.FetcherResHeaders();
				return this.resHeaders;
			}
		}

		private string error;
		public string Error
		{
			get {
				if (this.error == null)
					this.error = Http.FetcherError();
				return string.IsNullOrEmpty(this.error) ? null : this.error;
			}
		}

		private double timeUsed = -1;
		public double TimeUsed
		{
			get {
				if (this.timeUsed < 0)
					this.timeUsed = Http.FetcherTimeUsed();
				return this.timeUsed;
			}
		}

		private void Reset(string url=null, string method=null)
		{
			url = url;
			method = method;
			resBody = null;
			resText = null;
			resJson = null;
			resHeaders = null;
			error = null;
			timeUsed = -1;
		}

		~Fetcher()
		{
			Reset();
		}

		// FIX：headers key不能出现“_”（例如“X_FOO”是错误的，应该修正为“X-FOO”，被Accept后将自动修改为“HTTP_X_FOO”）
		public int Get(string url, Dictionary<string, string> headers, int timeout)
		{
			Reset(url, "GET");
			if (string.IsNullOrEmpty(url)) {
				error = "Invalid url !";
				return -1;
			}
			return Http.FetcherGet(url, headers, timeout);
		}

		public int Get(string url, Dictionary<string, string> headers)
		{
			return Get(url, headers, 0);
		}

		public int Get(string url, int timeout)
		{
			return Get(url, null, timeout);
		}

		public int Get(string url)
		{
			return Get(url, null, 0);
		}

		// FIX：headers key不能出现“_”（例如“X_FOO”是错误的，应该修正为“X-FOO”，被Accept后将自动修改为“HTTP_X_FOO”）
		public int Post(string url, Dictionary<string, string> headers, byte[] body, int timeout)
		{
			Reset(url, "POST");
			if (string.IsNullOrEmpty(url)) {
				error = "Invalid url !";
				return -1;
			}
			if (headers == null)
				headers = new Dictionary<string, string>();
			if (!headers.ContainsKey("Content-Type"))
				headers["Content-Type"] = "application/x-www-form-urlencoded";
			return Http.FetcherPost(url, headers, body, timeout);
		}

		public int Post(string url, Dictionary<string, string> headers, byte[] body)
		{
			return Post(url, headers, body, 0);
		}

		public int Post(string url, byte[] body, int timeout)
		{
			return Post(url, null, body, timeout);
		}

		public int Post(string url, byte[] body)
		{
			return Post(url, null, body, 0);
		}

		public int Post(string url, int timeout)
		{
			return Post(url, null, null, timeout);
		}

		public int Post(string url)
		{
			return Post(url, null, null, 0);
		}

		public int Post(string url, Dictionary<string, string> headers, int timeout, string bodyStr)
		{
			byte[] body = null;
			if (!string.IsNullOrEmpty(bodyStr))
				body = resEncoding.GetBytes(bodyStr);
			return Post(url, headers, body, timeout);
		}

		public int Post(string url, Dictionary<string, string> headers, string bodyStr)
		{
			return Post(url, headers, 0, bodyStr);
		}

		public int Post(string url, int timeout, string bodyStr)
		{
			return Post(url, null, timeout, bodyStr);
		}

		public int Post(string url, string bodyStr)
		{
			return Post(url, null, 0, bodyStr);
		}

		public int Post(string url, Dictionary<string, string> headers, int timeout, Dictionary<string, string> postParams)
		{
			byte[] body = null;
			if (postParams != null) {
				List<string> ls = new List<string>();
				foreach (KeyValuePair<string, string> p in postParams)
					ls.Add(p.Key + "=" + HttpUtility.UrlEncode(p.Value, resEncoding));
				body = resEncoding.GetBytes(string.Join("&", ls.ToArray()));
			}
			return Post(url, headers, body, timeout);
		}

		public int Post(string url, Dictionary<string, string> headers, Dictionary<string, string> postParams)
		{
			return Post(url, headers, 0, postParams);
		}

		public int Post(string url, int timeout, Dictionary<string, string> postParams)
		{
			return Post(url, null, timeout, postParams);
		}

		public int Post(string url, Dictionary<string, string> postParams)
		{
			return Post(url, null, 0, postParams);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams, int timeout)
		{
			Reset(url, "POST");
			if (string.IsNullOrEmpty(url)) {
				error = "Invalid url !";
				return -1;
			}
			return Http.FetcherPostForm(url, headers, name, fileName, fileBody, fileContentType, formParams, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams)
		{
			return PostForm(url, headers, name, fileName, fileBody, fileContentType, formParams, 0);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, int timeout)
		{
			return PostForm(url, headers, name, fileName, fileBody, fileContentType, null, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType)
		{
			return PostForm(url, headers, name, fileName, fileBody, fileContentType, null, 0);
		}

		public int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams, int timeout)
		{
			return PostForm(url, null, name, fileName, fileBody, fileContentType, formParams, timeout);
		}

		public int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams)
		{
			return PostForm(url, null, name, fileName, fileBody, fileContentType, formParams, 0);
		}

		public int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, int timeout)
		{
			return PostForm(url, null, name, fileName, fileBody, fileContentType, null, timeout);
		}

		public int PostForm(string url, string name, string fileName, byte[] fileBody, int timeout)
		{
			return PostForm(url, null, name, fileName, fileBody, null, null, timeout);
		}

		public int PostForm(string url, string name, string fileName, byte[] fileBody)
		{
			return PostForm(url, null, name, fileName, fileBody, null, null, 0);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string file, string contentType, Dictionary<string, string> formParams, int timeout)
		{
			return PostForm(url, headers, name, file, null, contentType, formParams, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string file, string contentType, Dictionary<string, string> formParams)
		{
			return PostForm(url, headers, name, file, null, contentType, formParams, 0);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string file, string contentType, int timeout)
		{
			return PostForm(url, headers, name, file, null, contentType, null, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> headers, string name, string file, string contentType)
		{
			return PostForm(url, headers, name, file, null, contentType, null, 0);
		}

		public int PostForm(string url, string name, string file, string contentType, Dictionary<string, string> formParams, int timeout)
		{
			return PostForm(url, null, name, file, null, contentType, formParams, timeout);
		}

		public int PostForm(string url, string name, string file, string contentType, Dictionary<string, string> formParams)
		{
			return PostForm(url, null, name, file, null, contentType, formParams, 0);
		}

		public int PostForm(string url, string name, string file, string contentType, int timeout)
		{
			return PostForm(url, null, name, file, null, contentType, null, timeout);
		}

		public int PostForm(string url, string name, string file, string contentType)
		{
			return PostForm(url, null, name, file, null, contentType, null, 0);
		}

		public int PostForm(string url, string name, string file, int timeout)
		{
			return PostForm(url, null, name, file, null, null, null, timeout);
		}

		public int PostForm(string url, string name, string file)
		{
			return PostForm(url, null, name, file, null, null, null, 0);
		}

		public int PostForm(string url, Dictionary<string, string> headers, Dictionary<string, string> formParams, int timeout)
		{
			return PostForm(url, headers, null, null, null, null, formParams, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> headers, Dictionary<string, string> formParams)
		{
			return PostForm(url, headers, null, null, null, null, formParams, 0);
		}

		public int PostForm(string url, Dictionary<string, string> formParams, int timeout)
		{
			return PostForm(url, null, null, null, null, null, formParams, timeout);
		}

		public int PostForm(string url, Dictionary<string, string> formParams)
		{
			return PostForm(url, null, null, null, null, null, formParams, 0);
		}

		// FIX：headers key不能出现“_”（例如“X_FOO”是错误的，应该修正为“X-FOO”，被Accept后将自动修改为“HTTP_X_FOO”）
		public int Put(string url, Dictionary<string, string> headers, byte[] body, int timeout)
		{
			Reset(url, "PUT");
			if (string.IsNullOrEmpty(url)) {
				error = "Invalid url !";
				return -1;
			}
			if (body == null || body.Length <= 0) {
				error = "Invalid body !";
				return -1;
			}
			if (headers == null)
				headers = new Dictionary<string, string>();
			if (!headers.ContainsKey("Content-Type"))
				headers["Content-Type"] = "application/x-www-form-urlencoded";
			return Http.FetcherPut(url, headers, body, timeout);
		}

		public int Put(string url, Dictionary<string, string> headers, byte[] body)
		{
			return Put(url, headers, body, 0);
		}

		public int Put(string url, byte[] body, int timeout)
		{
			return Put(url, null, body, timeout);
		}

		public int Put(string url, byte[] body)
		{
			return Put(url, null, body, 0);
		}

		public int Put(string url, int timeout)
		{
			return Put(url, null, null, timeout);
		}

		public int Put(string url)
		{
			return Put(url, null, null, 0);
		}

		public int Put(string url, Dictionary<string, string> headers, int timeout, string bodyStr)
		{
			byte[] body = null;
			if (!string.IsNullOrEmpty(bodyStr)) {
				body = resEncoding.GetBytes(bodyStr);
			}
			return Put(url, headers, body, timeout);
		}

		public int Put(string url, Dictionary<string, string> headers, string bodyStr)
		{
			return Put(url, headers, 0, bodyStr);
		}

		public int Put(string url, int timeout, string bodyStr)
		{
			return Put(url, null, timeout, bodyStr);
		}

		public int Put(string url, string bodyStr)
		{
			return Put(url, null, 0, bodyStr);
		}

		public int Put(string url, Dictionary<string, string> headers, int timeout, Dictionary<string, string> putParams)
		{
			byte[] body = null;
			if (putParams != null) {
				List<string> ls = new List<string>();
				foreach (KeyValuePair<string, string> p in putParams)
					ls.Add(p.Key + "=" + HttpUtility.UrlEncode(p.Value, resEncoding));
				body = resEncoding.GetBytes(string.Join("&", ls.ToArray()));
			}
			return Put(url, headers, body, timeout);
		}

		public int Put(string url, Dictionary<string, string> headers, Dictionary<string, string> putParams)
		{
			return Put(url, headers, 0, putParams);
		}

		public int Put(string url, int timeout, Dictionary<string, string> putParams)
		{
			return Put(url, null, timeout, putParams);
		}

		public int Put(string url, Dictionary<string, string> putParams)
		{
			return Put(url, null, 0, putParams);
		}

		// FIX：headers key不能出现“_”（例如“X_FOO”是错误的，应该修正为“X-FOO”，被Accept后将自动修改为“HTTP_X_FOO”）
		public int Delete(string url, Dictionary<string, string> headers, int timeout)
		{
			Reset(url, "DELETE");
			if (string.IsNullOrEmpty(url)) {
				error = "Invalid url !";
				return -1;
			}
			return Http.FetcherDelete(url, headers, timeout);
		}

		public int Delete(string url, Dictionary<string, string> headers)
		{
			return Delete(url, headers, 0);
		}

		public int Delete(string url, int timeout)
		{
			return Delete(url, null, timeout);
		}

		public int Delete(string url)
		{
			return Delete(url, null, 0);
		}

		public int Proxy(string url, IRequest req, string xParam, int timeout)
		{
			if (string.IsNullOrEmpty(url))
				return -1;

			// Combine the QueryString.
			if (url.IndexOf("?") > 0) {
				if (req.QueryString != null)
					url += "&" + req.QueryString;
			} else if (req.QueryString != null)
					url += "?" + req.QueryString;

			// Clone the Headers.
			Dictionary<string, string> headers = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> h in req.Headers)
				headers[h.Key.Replace("HTTP_", "").Replace("_", "-")] = h.Value;
			headers["X-IRMAKIT-PROXY"] = xParam == null ? "IRMAKit" : xParam;

			int code = -1;
			switch (req.Method) {
			case Web.Method.GET:
				code = Get(url, headers, timeout);
				break;
			case Web.Method.POST:
				code = Post(url, headers, req.Body, timeout);
				break;
			case Web.Method.PUT:
				code = Put(url, headers, req.Body, timeout);
				break;
			case Web.Method.DELETE:
				code = Delete(url, headers, timeout);
				break;
			}

			headers = null;
			return code;
		}

		public int Proxy(string url, IRequest req, string xParam)
		{
			return Proxy(url, req, xParam, 0);
		}

		public int Proxy(string url, IRequest req, int timeout)
		{
			return Proxy(url, req, null, timeout);
		}

		public int Proxy(string url, IRequest req)
		{
			return Proxy(url, req, null, 0);
		}
	}
}
