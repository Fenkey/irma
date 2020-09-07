using System;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class RequestParamsHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			res.BufferAppend(string.Format("Request is mock: {0}<br/>", req.IsMock));
			res.BufferAppend("Request Params Handle<br/><hr/>");
			res.BufferAppend(string.Format("ServerName: {0}<br/>", req.ServerName));
			res.BufferAppend(string.Format("ServerAddr: {0}<br/>", req.ServerAddr));
			res.BufferAppend(string.Format("ServerProtocol: {0}<br/>", req.ServerProtocol));
			res.BufferAppend(string.Format("ServerPort: {0}<br/>", req.ServerPort));
			res.BufferAppend(string.Format("RemoteAddr: {0}<br/>", req.RemoteAddr));
			res.BufferAppend(string.Format("RemotePort: {0}<br/>", req.RemotePort));
			res.BufferAppend(string.Format("Host: {0}<br/>", req.Host));
			res.BufferAppend(string.Format("DocumentRoot: {0}<br/>", req.DocumentRoot));
			res.BufferAppend(string.Format("ScriptFileName: {0}<br/>", req.ScriptFileName));
			res.BufferAppend(string.Format("ForwardedFor: {0}<br/>", req.ForwardedFor));
			res.BufferAppend(string.Format("ClientIp: {0}<br/>", req.ClientIp));
			res.BufferAppend(string.Format("Connection: {0}<br/>", req.Connection));
			res.BufferAppend(string.Format("Https: {0}<br/>", req.Https));
			res.BufferAppend(string.Format("HttpMethod: {0}<br/>", req.HttpMethod));
			res.BufferAppend(string.Format("QueryString: {0}<br/>", req.QueryString));
			res.BufferAppend(string.Format("ContentType: {0}<br/>", req.ContentType));
			res.BufferAppend(string.Format("Accept: {0}<br/>", req.Accept));
			res.BufferAppend(string.Format("AcceptLanguage: {0}<br/>", req.AcceptLanguage));
			res.BufferAppend(string.Format("AcceptEncoding: {0}<br/>", req.AcceptEncoding));
			res.BufferAppend(string.Format("UserAgent: {0}<br/>", req.UserAgent));
			res.BufferAppend(string.Format("Uri: {0}<br/>", req.Uri.AbsoluteUri));
			res.BufferAppend(string.Format("Uri.AbsolutePath: {0}<br/>", req.Uri.AbsolutePath));
			res.BufferAppend(string.Format("IsRef: {0}<br/>", req.IsRef));
			res.BufferAppend(string.Format("RefParam: {0}<br/>", req.RefParam));
			res.BufferAppend(string.Format("RefToParam: {0}<br/>", req.RefToParam));
			res.BufferAppend(string.Format("IsProxy: {0}<br/>", req.IsProxy));
			res.BufferAppend(string.Format("ProxyParam: {0}<br/>", req.ProxyParam));
			res.BufferAppend(string.Format("AppLocation: {0}<br/>", req.AppLocation));
			res.BufferAppend(string.Format("OriAppLocation: {0}<br/>", req.OriAppLocation));
			res.BufferAppend(string.Format("Referer: {0}<br/>", req.Referer));
			res.BufferAppend(string.Format("Origin: {0}<br/>", req.Origin));
			res.BufferAppend(string.Format("IfModifiedSince: {0}<br/>", req.IfModifiedSince));
			res.BufferAppend(string.Format("IfMatch: {0}<br/>", req.IfMatch));
			res.BufferAppend(string.Format("SSLDN: {0}<br/>", req.SSLDN));

			res.BufferAppend("<br/>Headers:<br/><hr/>");
			foreach (KeyValuePair<string, string> h in req.Headers)
				res.BufferAppend(h.Key + ": " + h.Value + "<br/>");

			res.BufferAppend("<br/>GetParams:<br/><hr/>");
			foreach (string k in req.GetParams.AllKeys)
				res.BufferAppend(k + ": " + req.GetParams.Get(k) + "<br/>");

			res.BufferAppend("<br/>PostParams:<br/><hr/>");
			foreach (string k in req.PostParams.AllKeys)
				res.BufferAppend(k + ": " + req.PostParams.Get(k) + "<br/>");

			res.BufferAppend("<br/>Files:<br/><hr/>");
			foreach (KeyValuePair<string, IPostFile> f in req.Files)
				res.BufferAppend(string.Format("Name('{0}') FileName('{1}') ContentType('{2}') ContentLength({3})<br/>", f.Value.Name, f.Value.FileName, f.Value.ContentType, f.Value.Content.Length));

			res.BufferAppend(string.Format("<br/>TotalBytes: {0}", req.TotalBytes));

			res.AppendHeader("X-Test", "Just for testing ...");

			res.Echo();

			Logger.DEBUG("Request params handle success.");
		}
	}
}
