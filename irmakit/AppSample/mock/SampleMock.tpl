using System;
using System.Web;
using IRMACore.Lower;
using ${appName}.Web;

namespace ${appName}Mock
{
    public class ${appName}Mock
    {
        // MyService为${appName}项目内继承于IRMAKit.Service的服务类
        public static MyService Service;

        public static void AppInitialize()
        {
            ${appName}Mock.Service = new MyService();
			// 引用${appName}项目内配置（请按实际情况修改文件路径）
            ${appName}Mock.Service.Init("C:\\project\\\\${appName}\\conf\\\\${appName}.conf", ref ICall.GlobalObject);
        }
    }
}
