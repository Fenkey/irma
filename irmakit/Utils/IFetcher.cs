using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using IRMAKit.Web;

namespace IRMAKit.Utils
{
	public interface IFetcher
	{
		/// <summary>
		/// Method
		/// </summary>
		string Method { get; }

		/// <summary>
		/// Url
		/// </summary>
		string Url { get; }

		/// <summary>
		/// Response charset
		/// </summary>
		string ResCharset { get; set; }

		/// <summary>
		/// Response body
		/// </summary>
		byte[] ResBody { get; }

		/// <summary>
		/// Response body in the form of text
		/// </summary>
		string ResText { get; }

		/// <summary>
		/// Response body in the form of json
		/// </summary>
		JObject ResJson { get; }

		/// <summary>
		/// Response headers
		/// </summary>
		Dictionary<string, string> ResHeaders { get; }

		/// <summary>
		/// Error
		/// </summary>
		string Error { get; }

		/// <summary>
		/// Time used
		/// </summary>
		double TimeUsed { get; }

		/// <summary>
		/// Get
		/// </summary>
		int Get(string url, Dictionary<string, string> headers, int timeout);

		/// <summary>
		/// Get
		/// </summary>
		int Get(string url, Dictionary<string, string> headers);

		/// <summary>
		/// Get
		/// </summary>
		int Get(string url, int timeout);

		/// <summary>
		/// Get
		/// </summary>
		int Get(string url);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, byte[] body, int timeout);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, byte[] body);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, byte[] body, int timeout);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, byte[] body);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, int time);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, int timeout, string bodyStr);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, string bodyStr);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, int timeout, string bodyStr);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, string bodyStr);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, int timeout, Dictionary<string, string> postParams);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> headers, Dictionary<string, string> postParams);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, int timeout, Dictionary<string, string> postParams);

		/// <summary>
		/// Post
		/// </summary>
		int Post(string url, Dictionary<string, string> postParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string fileName, byte[] fileBody, string fileContentType);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, Dictionary<string, string> formParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string fileName, byte[] fileBody, string fileContentType, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string fileName, byte[] fileBody, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string fileName, byte[] fileBody);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string file, string fileContentType, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string file, string fileContentType, Dictionary<string, string> formParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string file, string fileContentType, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, string name, string file, string fileContentType);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file, string fileContentType, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file, string fileContentType, Dictionary<string, string> formParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file, string fileContentType, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file, string fileContentType);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, string name, string file);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> headers, Dictionary<string, string> formParams);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> formParams, int timeout);

		/// <summary>
		/// Post form
		/// </summary>
		int PostForm(string url, Dictionary<string, string> formParams);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, byte[] body, int timeout);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, byte[] body);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, byte[] body, int timeout);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, byte[] body);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, int timeout);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, int timeout, string bodyStr);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, string bodyStr);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, int timeout, string bodyStr);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, string bodyStr);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, int timeout, Dictionary<string, string> putParams);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> headers, Dictionary<string, string> putParams);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, int timeout, Dictionary<string, string> putParams);

		/// <summary>
		/// Put
		/// </summary>
		int Put(string url, Dictionary<string, string> putParams);

		/// <summary>
		/// Delete
		/// </summary>
		int Delete(string url, Dictionary<string, string> headers, int timeout);

		/// <summary>
		/// Delete
		/// </summary>
		int Delete(string url, Dictionary<string, string> headers);

		/// <summary>
		/// Delete
		/// </summary>
		int Delete(string url, int timeout);

		/// <summary>
		/// Delete
		/// </summary>
		int Delete(string url);

		/// <summary>
		/// Proxy the received request
		/// </summary>
		int Proxy(string url, IRequest req, string xParam, int timeout);

		/// <summary>
		/// Proxy the received request
		/// </summary>
		int Proxy(string url, IRequest req, string xParam);

		/// <summary>
		/// Proxy the received request
		/// </summary>
		int Proxy(string url, IRequest req, int timeout);

		/// <summary>
		/// Proxy the received request
		/// </summary>
		int Proxy(string url, IRequest req);
	}
}
