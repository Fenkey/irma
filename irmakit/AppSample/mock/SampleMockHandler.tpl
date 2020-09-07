using System;
using System.Web;
using IRMACore.Lower;

namespace ${appName}Mock
{
    public class MockHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            ${appName}Mock.Service.Handle();
        }
    }
}
