using System;
using System.Collections.Generic;
using IRMACore.Net;

namespace IRMAKit.Utils
{
	public class Smtp : ISmtp
	{
		private bool valid = true;
		private string server;
		private string user;
		private string password;
		private string error;
		public string Error { get { return this.error; } }

		/*
		 * This is the URL for your mailserver. Note the use of smtps:// rather
		 * than smtp:// to request a SSL based connection. Such as:
		 * "smtp://smtp.exmail.qq.com:25"
		 * "smtps://smtp.exmail.qq.com:465"
		 */
		public Smtp(string server, string user, string password)
		{
			if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
				this.valid = false;
			else {
				this.server = server.Trim();
				this.user = user.Trim();
				this.password = password.Trim();
			}
		}

		private bool __Mail(string subject, List<string> to, string content, bool hideTo, string attachment0, string attachment1, string attachment2)
		{
			if (string.IsNullOrEmpty(subject) || to == null || to.Count <= 0 || string.IsNullOrEmpty(content)) {
				error = "Invalid params";
				return false;
			}
			return Http.SmtpMail(server, user, password, subject, to, content, attachment0, attachment1, attachment2, hideTo, ref error);
		}

		public bool Mail(string subject, List<string> to, string content, bool hideTo=false, string attachment0=null, string attachment1=null, string attachment2=null)
		{
			error = null;
			if (!valid)
				return false;
			return __Mail(subject, to, content, hideTo, attachment0, attachment1, attachment2);
		}

		public bool Mail(string subject, List<string> to, string content, string attachment0=null, string attachment1=null, string attachment2=null)
		{
			return Mail(subject, to, content, false, attachment0, attachment1, attachment2);
		}

		public bool Mail(string subject, string to, string content, bool hideTo=false, string attachment0=null, string attachment1=null, string attachment2=null)
		{
			error = null;
			if (!valid)
				return false;
			if (!valid || string.IsNullOrEmpty(to)) {
				error = "Invalid params";
				return false;
			}
			List<string> toList = new List<string>(to.Split(new char[] {',', ';'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries));
			return __Mail(subject, toList, content, hideTo, attachment0, attachment1, attachment2);
		}

		public bool Mail(string subject, string to, string content, string attachment0=null, string attachment1=null, string attachment2=null)
		{
			return Mail(subject, to, content, false, attachment0, attachment1, attachment2);
		}
	}
}
