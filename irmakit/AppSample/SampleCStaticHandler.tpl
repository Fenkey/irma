using System;
using System.IO;
using System.Text;
using IRMAKit.Utils;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[LoginCheck]
	public class CStaticHandler : StaticHandler
	{
		public CStaticHandler()
		{
			this.expires = 10;
		}
	}
}
