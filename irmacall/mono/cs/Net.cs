using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IRMACore.Lower;

namespace IRMACore.Net
{
	public sealed class Http
	{
		public static void HandleUnlock()
		{
			ICall.HandleUnlock();
		}

		public static object GetGlobalObject()
		{
			return ICall.GetGlobalObject();
		}

		public static int CaptureEvent()
		{
			int ret = ICall.RequestAccept();
			if (ret < 0)
				throw new Exception("Exit");
			return ret;
		}

		public static void OnceOver()
		{
			ICall.OnceOver();
		}

		public static bool RequestIsMock()
		{
			return ICall.RequestIsMock() > 0;
		}

		public static string GetRequestMethod()
		{
			return ICall.GetRequestMethod();
		}

		public static string GetRequestUri()
		{
			return ICall.GetRequestUri();
		}

		public static string GetRequestQueryString()
		{
			return ICall.GetRequestQueryString();
		}

		public static string GetRequestContentType()
		{
			return ICall.GetRequestContentType();
		}

		public static Dictionary<string, string> GetAllRequestHeaders()
		{
			Dictionary<string, string> headers = new Dictionary<string, string>();
			string hStr = ICall.GetAllRequestHeaders();
			if (!string.IsNullOrEmpty(hStr)) {
				string[] hList = hStr.Split(new string[]{"\r\n"}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				foreach (string h in hList) {
					string[] sList = h.Split(new char[]{'='}, 2);
					headers[sList[0]] = sList.Length == 2 ? sList[1] : "";
				}
			}
			return headers;
		}

		public static string GetRequestParam(string paramName)
		{
			return ICall.GetRequestParam(paramName);
		}

		public static Dictionary<string, byte[]> GetAllRequestGetParams()
		{
			Dictionary<string, byte[]> allParams = new Dictionary<string, byte[]>();
			string paramName = null;
			byte[] paramValue;
			int count = ICall.GetRequestGetParamsCount();
			for (int i = 0; i < count; i++) {
				paramValue = ICall.GetRequestGetParam(i, ref paramName);
				if (!string.IsNullOrEmpty(paramName))
					allParams[paramName] = paramValue;
			}
			return allParams;
		}

		public static Dictionary<string, byte[]> GetAllRequestPostParams()
		{
			Dictionary<string, byte[]> allParams = new Dictionary<string, byte[]>();
			string paramName = null;
			byte[] paramValue;
			int count = ICall.GetRequestPostParamsCount();
			for (int i = 0; i < count; i++) {
				paramValue = ICall.GetRequestPostParam(i, ref paramName);
				if (!string.IsNullOrEmpty(paramName))
					allParams[paramName] = paramValue;
			}
			return allParams;
		}

		public static Dictionary<string, byte[]> GetAllRequestFileParams()
		{
			Dictionary<string, byte[]> allParams = new Dictionary<string, byte[]>();
			string paramName = null;
			string fileName = null;
			string contentType = null;
			byte[] paramValue;
			int count = ICall.GetRequestFileParamsCount();
			for (int i = 0; i < count; i++) {
				paramValue = ICall.GetRequestFileParam(i, ref paramName, ref fileName, ref contentType);
				if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(fileName))
					allParams[paramName + "\r\n" + fileName + "\r\n" + contentType] = paramValue;
			}
			return allParams;
		}

		public static byte[] GetRequestBody()
		{
			return ICall.GetRequestBody();
		}

		public static byte[] RequestDump()
		{
			return ICall.RequestDump();
		}

		public static void AddResponseHeader(string header, string headerValue)
		{
			ICall.AddResponseHeader(header, headerValue);
		}

		public static void ClearResponseHeaders()
		{
			ICall.ClearResponseHeaders();
		}

		public static void SendHeader()
		{
			ICall.SendHeader();
		}

		public static void Redirect(string location)
		{
			ICall.Redirect(location);
		}

		public static void Send(byte[] content)
		{
			ICall.Send(content);
		}

		public static void SendHttp(int resCode, byte[] content)
		{
			ICall.SendHttp(resCode, content);
		}

		public static void Echo(byte[] content)
		{
			ICall.Echo(content);
		}

		public static bool SmtpMail(string server, string user, string password, string subject, string to, string content, string a0, string a1, string a2, bool hideTo, ref string error)
		{
			return ICall.SmtpMail(server, user, password, subject, to, content, a0, a1, a2, hideTo ? 1 : 0, ref error) < 0 ? false : true;
		}

		public static bool SmtpMail(string server, string user, string password, string subject, List<string> to, string content, string a0, string a1, string a2, bool hideTo, ref string error)
		{
			return ICall.SmtpMail(server, user, password, subject, string.Join(",", to.ToArray()), content, a0, a1, a2, hideTo ? 1 : 0, ref error) < 0 ? false : true;
		}

		public static int FetcherGet(string url, Dictionary<string, string> headers, int timeout)
		{
			ICall.FetcherClearHeaders();
			if (headers != null) {
				foreach (string k in headers.Keys)
					ICall.FetcherAppendHeader(string.Format("{0}:{1}", k, headers[k]));
			}
			return ICall.FetcherGet(url, timeout);
		}

		public static int FetcherPost(string url, Dictionary<string, string> headers, byte[] body, int timeout)
		{
			ICall.FetcherClearHeaders();
			if (headers != null) {
				foreach (string k in headers.Keys)
					ICall.FetcherAppendHeader(string.Format("{0}:{1}", k, headers[k]));
			}
			return ICall.FetcherPost(url, body, timeout);
		}

		public static int FetcherPostForm(string url, Dictionary<string, string> headers, string name, string file, byte[] body, string contentType, Dictionary<string, string> parameters, int timeout)
		{
			ICall.FetcherClearHeaders();
			if (headers != null) {
				foreach (string k in headers.Keys)
					ICall.FetcherAppendHeader(string.Format("{0}:{1}", k, headers[k]));
			}

			int ret = 0;
			ICall.FetcherClearFormPost();
			if (body != null)
				ret = ICall.FetcherAppendFormPostFileBuf(name, file, body, contentType);
			else if (!string.IsNullOrEmpty(file))
				ret = ICall.FetcherAppendFormPostFile(name, file, contentType);
			if (ret < 0)
				return -1;

			if (parameters != null) {
				foreach (string k in parameters.Keys)
					ICall.FetcherAppendFormPostKv(k, parameters[k]);
			}
			return ICall.FetcherPostForm(url, timeout);
		}

		public static int FetcherPut(string url, Dictionary<string, string> headers, byte[] body, int timeout)
		{
			ICall.FetcherClearHeaders();
			if (headers != null) {
				foreach (string k in headers.Keys)
					ICall.FetcherAppendHeader(string.Format("{0}:{1}", k, headers[k]));
			}
			return ICall.FetcherPut(url, body, timeout);
		}

		public static int FetcherDelete(string url, Dictionary<string, string> headers, int timeout)
		{
			ICall.FetcherClearHeaders();
			if (headers != null) {
				foreach (string k in headers.Keys)
					ICall.FetcherAppendHeader(string.Format("{0}:{1}", k, headers[k]));
			}
			return ICall.FetcherDelete(url, timeout);
		}

		public static byte[] FetcherResBody()
		{
			return ICall.FetcherResBody();
		}

		public static Dictionary<string, string> FetcherResHeaders()
		{
			string hStr = ICall.FetcherResHeaders();
			if (string.IsNullOrEmpty(hStr))
				return null;
			Dictionary<string, string> headers = new Dictionary<string, string>();
			string[] hList = hStr.Split(new string[]{"\r\n"}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
			foreach (string h in hList) {
				string[] sList = h.Split(new char[]{':'}, 2);
				if (sList.Length == 2 && !string.IsNullOrEmpty(sList[0]))
					headers[sList[0]] = sList[1].Trim();
			}
			return headers;
		}

		public static string FetcherError()
		{
			return ICall.FetcherError();
		}

		public static double FetcherTimeUsed()
		{
			return ICall.FetcherTimeUsed();
		}
	}
}
