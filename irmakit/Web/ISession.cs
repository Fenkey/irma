using System;
using System.Web;

namespace IRMAKit.Web
{
	public class SessionKickedOutException : Exception
	{
		private DateTime kt;
		public DateTime KickedTime { get { return this.kt; } }

		public SessionKickedOutException(DateTime kt) { this.kt = kt; }
	}

	public interface ISession : IDisposable
	{
		/// <summary>
		/// Session ID
		/// </summary>
		string SID { get; }

		/// <summary>
		/// Session内容[]支持
		/// </summary>
		object this[string key] { get; set; }

		/// <summary>
		/// 删除指定key对象
		/// </summary>
		bool Remove(string key);

		/// <summary>
		/// 绑定key与sid，使之关系唯一化，即同一时刻只有一个sid有效，可避免相同用户名在多处同时登录使用服务
		/// attachKey：将与sid绑定的key，通常为登录用户名
		/// sessionKey: session关键字
		/// kickOutOnly: 仅踢出旧SID（不绑定当前SID），例如迫使某用户强行退出
		/// AttachSid() 通常在login成功后调用
		/// </summary>
		bool AttachSid(string attachKey, string sessionKey, bool kickOutOnly=false);

		/// <summary>
		/// 指定key是否已被内部绑定？如果已绑定，则当前绑定者是否本人sid？
		/// attachKey：将与sid绑定的key，通常为登录用户名
		/// sessionKey: session关键字
		/// 用途：
		/// 1. login过程检测当前用户名是否已绑定（已登录），可进一步咨询用户是否需要踢出旧登录者（通过调用AttachSid()）
		/// 2. 为避免并发登录绕过kick out的情况（几乎同时登录、均未能检测并kick out，最终均成功login），可考虑在特定
		/// 地方调用IsAttached()（例如index页面）进行检测，如果返回false、或currentIsYourself为false，均自动logout出去
		///（将导致最后一个并发登录进入者保持继续可用，其余退出）
		/// 3. 检测任何一个指定的用户名当前是否处于已登录状态（前提是登录过程启用了AttachSid()进行绑定）。和常规检测用
		/// 户是否已登录的方式不同（例如Session[sessionKey]是否为空），前者只能对当前会话进行检测、而后者可以对任何一个
		/// 非当前会话进行检测，适用于高级管理权限行为
		/// </summary>
		bool IsAttached(string attachKey, string sessionKey, ref bool currentIsYourself);

		/// <summary>
		/// IsAttached简化方法
		/// </summary>
		bool IsAttached(string attachKey, string sessionKey);

		/// <summary>
		/// Session对象关闭
		/// </summary>
		bool Close();
	}
}
