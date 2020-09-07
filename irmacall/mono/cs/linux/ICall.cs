using System;
using System.Runtime.CompilerServices;

namespace IRMACore.Lower
{
	public abstract class ICall
	{
		// for System
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LaunchInfo(string appName, string version, long bodyMax, string url);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void Launched();

		// for Log
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogDebug(string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogEvent(string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogWarn(string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogError(string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogFatal(string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void LogTc(string content);

		// for Http
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void HandleUnlock();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static object GetGlobalObject();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int RequestIsMock();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int RequestAccept();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool FuseCheck(string handler);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void OnceOver();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetRequestMethod();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetRequestUri();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetRequestQueryString();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetRequestContentType();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetAllRequestHeaders();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetRequestParam(string paramName);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int GetRequestGetParamsCount();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GetRequestGetParam(int index, ref string paramName);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int GetRequestPostParamsCount();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GetRequestPostParam(int index, ref string paramName);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int GetRequestFileParamsCount();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GetRequestFileParam(int index, ref string paramName, ref string fileName, ref string contentType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GetRequestBody();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] RequestDump();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void AddResponseHeader(string header, string headerValue);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void ClearResponseHeaders();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void SendHeader();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void Redirect(string location);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void Send(byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void SendHttp(int resCode, byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void Echo(byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int SmtpMail(string server, string user, string password, string subject, string to, string content, string a0, string a1, string a2, int hideTo, ref string error);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherAppendHeader(string header);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherClearHeaders();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherAppendFormPostKv(string k, string v);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherAppendFormPostFile(string name, string file, string contentType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherAppendFormPostFileBuf(string name, string file, byte[] body, string contentType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherClearFormPost();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherGet(string url, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherPost(string url, byte[] body, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherPostForm(string url, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherPostFile(string url, string name, string file, string contentType, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherPostFileBuf(string url, string name, string fileName, byte[] body, string contentType, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherPut(string url, byte[] body, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int FetcherDelete(string url, int timeout);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] FetcherResBody();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string FetcherResHeaders();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string FetcherError();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static double FetcherTimeUsed();

		// for Sys
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetOS();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int GetWorkerIndex();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long GetUnixTime();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long BuildUnixTime(int y, int m, int d, int h, int M, int s);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long BuildGmTime(string str);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long UnixTimeToGmTime(long unixTime, ref string gmtStr);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string GetCurrentPath();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string ShellExecute(string cmd);

		// for DES
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesEcbEncrypt(string key, byte[] content, int pType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesEcbDecrypt(string key, byte[] content, int ptype);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesCbcEncrypt(string key, string iv, byte[] content, int pType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesCbcDecrypt(string key, string iv, byte[] content, int ptype);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesNCbcEncrypt(string key, string iv, byte[] content, int pType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] DesNCbcDecrypt(string key, string iv, byte[] content, int ptype);

		// for AES
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] AesEcbEncrypt(string key, byte[] content, int pType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] AesEcbDecrypt(string key, byte[] content, int ptype);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] AesCbcEncrypt(string key, string iv, byte[] content, int pType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] AesCbcDecrypt(string key, string iv, byte[] content, int ptype);

		// for RSA
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] RsaEncrypt(string keyFile, string keyPwd, string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string RsaDecrypt(string keyFile, string keyPwd, byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] RsaSign(int type, string keyFile, string keyPwd, string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool RsaVerify(int type, string keyFile, string keyPwd, string content, byte[] sign);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] RsaMemEncrypt(string key, string keyPwd, string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string RsaMemDecrypt(string key, string keyPwd, byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] RsaMemSign(int type, string key, string keyPwd, string content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool RsaMemVerify(int type, string key, string keyPwd, string content, byte[] sign);

		// for Kvs
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long MemcachedNew(string servers, string instance, long zipMin);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long RedisNew(string server, int port, string instance);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsExpire(long L, string key, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsExists(long L, string key);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsDelete(long L, string key);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsSetNx(long L, string key, byte[] value, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsSetEx(long L, string key, byte[] value, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsSet(long L, string key, byte[] value, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsAdd(long L, string key, byte[] value, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsReplace(long L, string key, byte[] value, long expire);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsPrepend(long L, string key, byte[] value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsAppend(long L, string key, byte[] value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] KvsGet(long L, string key);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[][] KvsMGet(long L, string[] keys);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static bool KvsSetCounter(long L, string key, long value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long KvsGetCounter(long L, string key);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long KvsIncr(long L, string key, long value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long KvsDecr(long L, string key, long value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long KvsLLen(long L, string key);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long KvsRPush(long L, string key, byte[][] vals);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] KvsLPop(long L, string key);

		// for Summary
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string Md5(byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string Sha1(byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string Sha256(byte[] content);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static string Sha512(byte[] content);

		// for Unit
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long UnitParseBytes(string value);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static long UnitParseSeconds(string value);

		// for GZip
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GZip(byte[] data);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static byte[] GUnZip(byte[] data);

		// for Ver
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static int VerCmp(string v1, string v2, ref string error);
	}
}
