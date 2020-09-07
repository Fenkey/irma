using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using IRMAKit.Utils;

namespace IRMAKit.Web
{
	public interface IResponse : IDisposable
	{
		/// <summary>
		/// Buffer for responsing
		/// </summary>
		byte[] Buffer { get; }

		/// <summary>
		/// CookiePath
		/// </summary>
		string CookiePath { set; get; }

		/// <summary>
		/// HttpCookieCollection
		/// </summary>
		HttpCookieCollection Cookies { get; }

		/// <summary>
		/// SetCookie
		/// </summary>
		void SetCookie(HttpCookie cookie);

		/// <summary>
		/// RemoveCookie
		/// </summary>
		void RemoveCookie(string cookieName);

		/// <summary>
		/// GZipEnable
		/// </summary>
		bool GZipEnable { set; get; }

		/// <summary>
		/// Content-Type
		/// </summary>
		string ContentType { set; get; }

		/// <summary>
		/// Cache-Control
		/// </summary>
		string CacheControl { set; get; }

		/// <summary>
		/// Expires
		/// </summary>
		string Expires { set; get; }

		/// <summary>
		/// Last-Modified
		/// </summary>
		string LastModified { set; get; }

		/// <summary>
		/// ETag
		/// </summary>
		string ETag { set; get; }

		/// <summary>
		/// XFrameOptions
		/// The non-standard header restricts the response content from being sent to other
		/// websites by iframe, so as to prevent the clickjacking attack.
		/// DENY：The page is not allowed to be displayed in a frame, even if it is nested in a page with the same domain name
		/// SAMEORIGIN：The page can be displayed in the frame of the same domain name page
		/// ALLOW-FROM uri：The page can be displayed in the frame of the specified 'uri' page
		/// </summary>
		string XFrameOptions { set; get; }

		/// <summary>
		/// AppendHeader
		/// </summary>
		void AppendHeader(string header, string headerValue);

		/// <summary>
		/// ClearHeaders
		/// </summary>
		void ClearHeaders();

		/// <summary>
		/// SendHeader (send headers only)
		/// </summary>
		void SendHeader();

		/// <summary>
		/// BufferWrite
		/// </summary>
		void BufferWrite(byte[] content, int offset);

		/// <summary>
		/// BufferAppend
		/// </summary>
		void BufferAppend(byte[] content);

		/// <summary>
		/// BufferAppend
		/// </summary>
		void BufferAppend(string content, Encoding encoding);

		/// <summary>
		/// BufferAppend
		/// </summary>
		void BufferAppend(string content, string charset);

		/// <summary>
		/// BufferAppend
		/// </summary>
		void BufferAppend(string content);

		/// <summary>
		/// BufferReset
		/// </summary>
		void BufferReset();

		/// <summary>
		/// Send (send the original packet)
		/// </summary>
		void Send(byte[] content);

		/// <summary>
		/// SendHttp
		/// </summary>
		void SendHttp(int resCode, bool noBody=true);

		/// <summary>
		/// SendHttp
		/// </summary>
		void SendHttp(int resCode, byte[] content);

		/// <summary>
		/// SendHttp
		/// </summary>
		void SendHttp(int resCode, string content, Encoding encoding);

		/// <summary>
		/// SendHttp
		/// </summary>
		void SendHttp(int resCode, string content, string charset);

		/// <summary>
		/// SendHttp
		/// </summary>
		void SendHttp(int resCode, string content);

		/// <summary>
		/// SendProxy
		/// </summary>
		/// <param name="saveRemoteCookies">
		/// 0: Don't transfer the cookies
		/// 1: Transfer the cookies, but its matching path is restricted to the current location
		/// 2: Transfer the cookies and replace its matching path as CookiePath
		/// </param>
		void SendProxy(int resCode, Dictionary<string, string> resHeaders, byte[] resBody, int saveRemoteCookies=0);

		/// <summary>
		/// SendProxy
		/// </summary>
		int SendProxy(IFetcher fetcher, string url, IRequest req, string proxyParam, int saveRemoteCookies);

		/// <summary>
		/// SendProxy
		/// </summary>
		int SendProxy(IFetcher fetcher, string url, IRequest req, string proxyParam);

		/// <summary>
		/// SendProxy
		/// </summary>
		int SendProxy(IFetcher fetcher, string url, IRequest req, int saveRemoteCookies);

		/// <summary>
		/// SendProxy
		/// </summary>
		int SendProxy(IFetcher fetcher, string url, IRequest req);

		/// <summary>
		/// Echo (send Buffer with 200 code)
		/// </summary>
		void Echo();

		/// <summary>
		/// Echo (send content with 200 code)
		/// </summary>
		void Echo(byte[] content);

		/// <summary>
		/// Echo (send content with 200 code)
		/// </summary>
		void Echo(string content, Encoding encoding);

		/// <summary>
		/// Echo (send content with 200 code)
		/// </summary>
		void Echo(string content, string charset);

		/// <summary>
		/// Echo (send content with 200 code)
		/// </summary>
		void Echo(string content);

		/// <summary>
		/// Echo (send content with 200 code)
		/// </summary>
		void Echo(string content, params object[] args);

		/// <summary>
		/// NotModified (304)
		/// </summary>
		void NotModified();

		/// <summary>
		/// Unauthorized (401, Basic scheme)
		/// </summary>
		void Unauthorized(string realm=null);

		/// <summary>
		/// Forbidden (403)
		/// </summary>
		void Forbidden();

		/// <summary>
		/// NotFound (404)
		/// </summary>
		void NotFound(bool accessNotFound=false);

		/// <summary>
		/// Redirect (302)
		/// </summary>
		void Redirect(string locationUrl, bool routineBreak=true);

		/// <summary>
		/// Download, response in the way of downloading
		/// Note to set the necessary ContentType、GZipEnable before calling it
		/// </summary>
		void Download(string file, string fileName=null);

		/// <summary>
		/// Download
		/// </summary>
		void Download(byte[] content, string fileName);
	}
}
