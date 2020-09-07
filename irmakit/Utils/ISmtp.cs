using System.Collections.Generic;

namespace IRMAKit.Utils
{
	public interface ISmtp
	{
		string Error { get; }

		/// <summary>
		/// Mail
		/// </summary>
		/// <param name="subject">subject: mailing subject / title</param>
		/// <param name="to">to: receiver of the mail, e.g. "one@gmail.com, tow@126.com, ..."</param>
		/// <param name="content">content: mailing content which supports HTML format</param>
		/// <param name="hideTo">hideTo: if true, the mail will hide the receiver's information</param>
		/// <param name="attachment">attachment: at most 3 files which include path</param>
		bool Mail(string subject, List<string> to, string content, bool hideTo=false, string attachment0=null, string attachment1=null, string attachment2=null);

		/// <summary>
		/// Mail
		/// </summary>
		bool Mail(string subject, List<string> to, string content, string attachment0=null, string attachment1=null, string attachment2=null);

		/// <summary>
		/// Mail
		/// </summary>
		bool Mail(string subject, string to, string content, bool hideTo=false, string attachment0=null, string attachment1=null, string attachment2=null);

		/// <summary>
		/// 发送邮件
		/// </summary>
		bool Mail(string subject, string to, string content, string attachment0=null, string attachment1=null, string attachment2=null);
	}
}
