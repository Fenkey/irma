**`[Q]` - 采用`irmakit`开发，如何能快速上手 ？**

> * 对于待开发的service项目，有相对清晰的整体规划和考虑（业务需求、服务定位、关键接口和逻辑、缓存/存储、安全检测和拦截、API规范、数据包文规格、依赖服务、设计瓶颈和风险点等）
> * 通过`irma-genapp`命令生成默认的应用，在此基础上进行相关调整（额外目录及文件增加、修改`Makefile`等）
> * 准确且适当容错的`conf`配置（良好的用户配置信息是很重要的，更有利于未来的维护和扩展）
> * 考虑好如何开始与结束（`AppInit`、`AppFinalize`），开始时需要先准备好哪些必要的组件（例如`IDBStore`、`IKeyValueStore`、`IFetcher`等，后续请求中拿来即用）
> * 必要的基础类封装和复用（例如要求必须登录才可访问的接口，统一设计了必要的、可高效复用的`ReqCheckAttribute`拦截器）
> * 制定合理的请求路由（即HTTP API）及Handler（前端界面相关可同步设计、实现和联调）
>
> 总的来说，考虑好需求和定位，先设计再编码


**`[Q]` - 关于`conf`配置**

> * 采用`irma-genapp`生成项目时，默认采用`conf/<project-name>.conf`方式命名项目配置文件（例如`conf/Foo.conf`）。也修改为任何你所喜欢的名称，只是要留意同时修改一下启动文件`start.sh`内`config`配置，例如：

```bash
config=`pwd`/conf/myapp.conf
```

> * `conf`配置采用JSON格式，整体分为两部分：`system`（系统配置）、`user`（项目配置）。显然`user`配置是完全根据项目实际情况而配置的，没有固定或可预知的规则和内容；而`system`配置则有一定要求，主要设置包括：

```bash
"system": {
	"app_name": --项目/应用名称（当<ip>:<port>即唯一确定应用时，app_name允许为空串；否则访问路径为：<ip>:<port>/<app_name>、或<domain_name>/<app_name>、或<domain_name>）
	"version": --版本
	"release_info": --发布信息（例如日期字符串或其他你认为有用信息）
	"app_charset": --应用字符集（建议"utf-8"）
	"body_max": --支持的最大请求body大小（默认6M，以byte为单位，支持多种表达，例如：6291456、"6M"）
	"session": { --会话相关（仅当应用需要用到会话才需要）
		"server": { --服务端配置
			"engine": --IKeyValeStore引擎（例如："memcached"、"cmemcached"、"memcachedwrapper"、"credis"，默认"memcached"）
			"servers": --和engine对应的key-value服务地址
			"instance": --会话实例名称（不同应用可以有相同的instance名称，irmakit会加入app_name前缀）
			"expire": --服务端会话时长（以秒为单位，支持多种表达，例如：120、"120s"、"2h"、86400、"1d"）
		},
		"client": { --客户端配置
			"cookie_name": --客户端会话cookie名称
			"cookie_domain": --客户端会话匹配域名
			"cookie_path": --客户端会话匹配访问路径（默认""代表和app_name一致）
			"cookie_expire": --客户端会话cookie缓存时长（以秒为单位，支持多种表达。0表示永不过期）
		}
	},
	"performance": --请求处理性能评估类（将在debug日志内输出每个请求处理耗时，单位毫秒）
	"routers": [ --请求路由映射及分发
		{
			"path": --请求路径（'@'开头为绝对路径匹配，否则为正则表达式匹配）
			"handler": --请求处理器类
			"methods": --允许的请求方法（'*'代表irmakit内支持的所有方法）
			"pf": --是否支持请求性能分析及日志输出（和"performance"对应，仅当配置有效performance及true时pf才真正生效
		},
		...
	]
}
```


**`[Q]` - `conf`内`app_name`配置会产生哪些影响 ？**

> `app_name`对于`conf`配置来说是必须的，但可以为空字符串（`""`）。`app_name`代表的是当前应用的名称：
>
> * `app_name`为空字符串（`""`）: 端口唯一确定应用，即访问`http(s)://<ip>:<port>`则对应当前应用。典型情况如`VS`内直接通过`chrome`启动
> * `app_name`为`非空`（例如"Foo"）: 通过`app_name`唯一确定应用，即访问`http(s)://<ip>:<port>/Foo`则对应`Foo`应用
>
> `app_name`甚至可以包括路径，例如: `fenkey/Foo`，但不能包括`空格`、开头和结尾不能带“`/`”（即`"/Foo"`或`"Foo/"`是错的）。另外，`app_name`会影响到如下地方:
>
> * `context.Request.AppLocation`
> * `conf`文件内`system.routers.path`配置
> * `RestfulApi`内`Pattern`配置
> * 服务端`session`保存
> * 客户端`cookie路径`
>
> **特别留意该情况**: 当两个或多个应用，其`app_name`设置一样、而且`system.session.server.instance`设置也一样时，意味着这些应用必然共享了相同的服务端`session`存储，或者说这些应用之间即可单点登录`SSO（Single Sign On）`。从该意义上看，更多应用可设计为主应用的分布式子service而形成服务群


**`[Q]` - 关于日志输出级别和规格 ？**

> * `irmakit`最终的日志输出采用了引擎层`irmacall`内的`log`实现，和大部分日志规划一样分了如下输出层级：`debug、event、warn、error、fatal、tc`。结合`start.sh`内`log_type`启动级别，确定最终代码内哪些日志能被实际输出到文件（例如`log_type`为`event`时，代码内只有`debug`、`event`日志才能输出）
> * `event`日志按`内存cache`方式缓存若干行才输出到文件（例如100行）
> * 其中`tc`为`Test Case`日志，代码内如果出现有`tc`日志，则无论`log_type`级别如何，均会输出。`tc`日志往往用于代码级别的回归测试埋点（建议加入必要的宏定义，确保生产环境编译运行时不会产生无谓的日志输出干扰）
> * 日志文件以`irma_<yyyy><mm><dd>.log`文件名格式输出在`<app>.dll`相同目录内（`irmacall`内会自动按日期切割生成日志文件），例如：

```bash
$ tree Bin/Debug/log
Bin/Debug/log
├── debug
│   └── irma_20200905.log
├── error
├── event
│   └── irma_20200905.log
├── fatal
├── tc
└── warn
```

> `log`文件内按行输出，每行规格为`[hh:mm:ss,ms|pid|threadid] [Core|Kit|] - ...`


**`[Q]` - 关于`context`、及`Handler`之外如何引用`context` ？**

> `context`即一次会话的上下文，是交互式系统设计中的常规做法。`irmakit`将请求（`IRequest`）、响应（`IResponse`）、及配置（`IConfig`）对象的引用均包含在了`context`，获取`context`对象即几乎获取了单次响应处理中所需要的所有信息；同时不同的工作线程有自己独立的`context`对象、且设计为静态对象，即`context`是在独立沙箱环境内全局有效的，在`Handler`之外可通过`Service.CurrentContext`引用。最后，建议在启动时（`AppInit`）即将后续需要用到的组件、解析后必要的配置key放入到`context`内（例如：context["fetcher"] = new Fetcher()），这既和底层引擎`irmacall`对组件的单例化扩展实现机制有关、也和初始有效性检测、及提高响应性能有关


**`[Q]` - `Handler`内对单次请求进行多次`response`响应（例如：`Echo`、`SendHttp`）情况如何 ？响应后剩余代码是否还能执行 ？**

> 可以多次`response`，但只有第一次的`response`是有效的，后续的`response`均将被忽略和失效。响应后剩余的代码会继续执行，即意味着`response`并不代表本次请求处理的结束。典型情况如当前HTTP API处理是比较耗时的，完全可在接收到请求后即刻`response`告知前端已收到请求，然后才开展实际的耗时处理，从而避免了前端的等待（类似异步处理）


**`[Q]` - 应用API及路由配置多该怎么办 ？**

> `irmakit`除了`conf`文件的`system.routers`可配置外，还支持在代码内重载`Service.LoadRouter()`方法从任何地方导入路由配置，例如Database、云端存储服务、另外的Web Service等，由应用决定如何获取路由信息。所需要的是参考`conf`内`routers`格式的`JSON`字符串返回，例如：`return "[{\"path\":\"^/$\",\"handler\":\"Foo.Web.IndexHandler\",\"methods\": \"*\",\"pf\":true}]";`


**`[Q]` - `[ref]`标注（`Attribute`）的作用是什么 ？什么场景可用 ？**

> `[ref]`即`RefAttribute`为服务端对当前请求的`内部交付`（inner transfer），将请求通过内部路由重分发到另外一个`Handler`去处理，当前的`Handler`对应的API相当于是一个伪API（pseudo API）。相比较HTTP的`302`重定向，`[ref]`机制避免了客户端浏览器的重新请求、降低了网络通信次数；同时也避免了一次的前端请求意图，被切分成2个、甚至多个HTTP处理的连贯性和可控性问题。`Handler`内可通过`context.Request.IsRef`判断当前正在处理的请求是否来自于服务端自身的`[ref]`，并依此作出和常规请求差异的处理。典型应用场景如：API-a和API-b在处理逻辑上是完全一样的，因为业务权限控制及API设计需要而分开成独立的两个API，用户有权限访问API-a、而没有权限直接访问API-b（有些用户则有权限直接访问API-b），可考虑的一种做法就是具体的实现代码都在API-b，让API-a的请求`[ref]`到API-b去，API-b内检测`IsRef`为true时，视为足够权限的请求而直接处理、而无需再次的权限检测（API-a内已经完成必要的访问控制检测，例如是否登录、是否有权限访问API-a等）

**`[Q]` - `irmakit`的`IDBStore`事务处理中，为何额外增加了一个`rc`参数 ？**

> 参考`irmakit`的`Store/MySqlStore.cs`，可了解到类似`BeginTransaction()`、`CommitTransaction()`及`RollbackTransaction()`方法内均额外增加的`int rc`参数含义为`refCount`（引用计数）。在我们通常的事务相关开发中，可能会出现`事务嵌套应用`的情况，在A事务开启并未最终commit/rollback之前，又开启了B事务，而且A/B事务之间存在操作干涉，将导致事务死锁。而且这类写法并不容易被发现、一旦死锁则又不容易解锁（甚至需要重启应用，否则整体性能急速下滑）。在`irmakit`所封装的`MySqlStore`组件中，内部维护了`connection`及`transaction`对象，通过`rc`参数作尽可能的审查和避免嵌套，换句话说，同一连接下只能次序事务处理（例如A commit/rollback后，才能进行B事务，否则将导致异常抛出而避免进一步的死锁）。另外注意，该方式并不影响我们常规的用法：

`using (DbTransaction tran = db.BeginTransaction(...)) { ... }`


**`[Q]` - 关于MemcachedStore / CMemcachedStore / MemcachedStoreWrapper的差异 ？**

> 组件 | 实现 | 值压缩 | OS可运行
> ---- | ---- | ----- | -------
> MemcachedStore | C#第三方包 | 超过128kb时Deflate方式压缩 | linux / windows
> CMemcachedStore | irmacall C实现 | 自定义阀值进行quicklz方式压缩 | linux
> MemcachedStoreWrapper | irmakit C#实现 | - | linux / windows
>
> `MemcachedStoreWrapper`在`linux`平台下自动套用`CMemcachedStore`、在`windwos`下套用`MemcachedStore`；包括`CRedisStore`在内，均可作为`system.session.server.servers.engine`，考虑到尽可能在不同平台下的开发和自测，`engine`默认采用`MemcachedStore`，而且`engine`选择`CMemcachedStore`时，`irmakit`将其阀值设置为5kb（即超过5kb的值则自动启用压缩）


**`[Q]` - `session`过期及自动续期的机制 ？**

> `session`通过`conf`文件内配置`system.session.server.servers.expire`设定其过期时间，在每次`session`对象新创建时，对象内均记录了其开始时间（首次即创建时间）、和过期时长（秒），在每次获取`session`对象时，都会与当前时刻对比而确定其是否还有效（即过期）；有效情况下，进一步检测cache时长距离开始时间是否已超过过期时长的2/3，是则更新其开始时间为当前时间、并设置过期时长为`system.session.server.servers.expire`原始配置时长的1/2，以实现`session`自动续期（含义为：最近该用户存在过活跃访问，预测其接下来还可能会有访问的需要）。总的来说，该方式是在一定程度有效、及算法相对简单的一种折中做法


**`[Q]` - `ISession.AttachSid()`方法的作用 ？**

> 可选的调用方法，用于更新最新的登录`session id`。通常来说，用户采用相同用户名在不同浏览器是可以同时有效登录应用的，但假如将该行为视为非法情况，则应该有个踢出旧登录的机制，以确保同一时刻只有唯一一个登录会话是合法可用的，这主要是基于安全及账号体系可控的考虑，这就是是`AttachSid`方法的作用。例如应用采用`UserName`作为登录`account`、且以`UserInfo`作为key获取`session`内用户信息（Session["UserInfo"]），则`UserName`为“Jack”的用户在浏览器A登录后（没有logout），继续在浏览器B按“Jack”登录，假如登录代码内加入了登录成功后：

```bash
// 标明用户已经成功登录
context.Session["UserInfo"] = ui;
context.Session["..."] = ...;
// 将踢出A浏览器登录所产生的旧`session id`
context.Session.AttachSid("Jack", "UserInfo");
```

> 此时A浏览器内如果继续访问后端应用，在获取对应`session`信息时，将会收到异常而了解到其自身已经被踢出（后端可依此反馈给前端相关信息）：

```bash
try {
	ui = context.Session["UserInfo"];
	...
} catch (SessionKickedOutException e) {
	...
}
```

> 故该方法只是一个可选方法，但如果有此需要、并调用了该方法，则意味着尽可能通过`session`获取用户登录信息的代码中，加入必要的异常捕捉处理


**`[Q]` - 建议的Mono版本 ？**

> 没有准确的建议，至少之前所采用的`mono-5.18.1.0`还是相对稳定的（也是滞后的）。对IRMA项目有兴趣者，不妨作更多尝试，也希望在此反馈和更新你所了解的情况


**`[Q]` - 运行应用抛出`System.DllNotFoundException: libc`异常 ？**

> 例如`DNS`解析时即可能出现（`irmakit`的`Fetcher`及`MemcachedStore`等组件都可能会涉及到对域名的解析），简单验证代码如：

```bash
using System.Net;
...
IPHostEntry entry = Dns.GetHostEntry("localhost");
```

> 这和`libc.so`文件有关，假设我们安装的`mono`位置为`$HOME/local/mono`，检测其所依赖信息：

```bash
$ ldd `which mono` | grep "\<libc\>"
libc.so.6 => /lib/x86_64-linux-gnu/libc.so.6 (0x00007f77a04a1000)
```

> 简单方式可直接加入对应的软链接：

```bash
cd $HOME/local/mono/lib
ln -s /lib/x86_64-linux-gnu/libc.so.6 libc.so
```


**`[Q]` - Linux及Mono环境下采用`System.Drawing`绘图方法，在运行时异常 ？**

> 由于绘图和OS密切相关，为了在`linux`上可以通过`System.Drawing`进行绘图，可按`Mono`官方建议下载和编译`libgdiplus`、并让`Mono`能找到其最终的编译结果`libgdiplus.so`、`libgdiplus.so.0`即可。具体参考：<a src="https://www.mono-project.com/docs/gui/drawing/">`https://www.mono-project.com/docs/gui/drawing/`</a>

```bash
git clone https://github.com/mono/libgdiplus.git #Or: wget https://github.com/mono/libgdiplus/archive/5.6.1.tar.gz
...
./autogen.sh --prefix=$HOME/local/libgdiplus
make & make install
export LD_LIBRARY_PATH=$HOME/local/libgdiplus/lib:$LD_LIBRARY_PATH
假设Mono安装位置：$HOME/local/mono，加入软链接（也可以是其他方式）：
cd $HOME/local/mono/lib; ln -s $HOME/local/libgdiplus/lib/libgdiplus.so.0 libgdiplus.so.0
```

> 除了上述软链接方式外，也可以通过修改`/etc/ld.so.conf`并执行`ldconfig`导入更新动态库缓存等方法。另外`libgdiplus`的编译依赖的不少基础包（例如`libgif`、`libglib2.0`、`libcairo2`、`libtiff`等，请参考其`README.md`文件安装即可）


**`[Q]` - Linux环境下运行应用，抛出`FontFamilyNotFound`异常 ？**

> 这是由于Linux环境内没有安装对应的字体库导致（例如`/etc/fonts/fonts.conf`文件内查看字体库安装目录），简单方式，可直接从`Windows`系统内打包复制（例如：`C:\Windows\Fonts`）到上述字体库目录


**`[Q]` - Windows环境下如何进行`irmakit`应用开发 ？**

> `irmakit`结合调度引擎`irmacall`的目标运行环境是Linux，考虑到Windows环境下一系列IDE工具的成熟、方便和本身.Net系列技术团队/人员的开发习惯等因素，`irmakit`同样支持Windows环境下采用IDE工具的开发，但前提定位是Windows环境始终只是以`可开发、可mock调试运行`目标去准备的，包括只能以单线程方式、部分组件方法不支持等。首先，先理解`irmakit`在Windows下mock的原理：
>
> *"通过构建的mock网站将请求转交给实际开发项目（例如Foo），由后者所配置的路由（`routers`）和处理器（`Handler`）去处理请求（即进入`irmakit`框架内处理逻辑），并将响应信息通过mock网站反馈给远程客户端"*
>
> * 采用`irma-genapp`工具自动生成含windows mock的项目（其中Foo为真正开发的项目，也是最终运行在Linux环境的项目；FooMock仅作为Windows环境下mock网站）。留意按实际情况修改`FooMock/App_Code/FooMock.cs`文件内`FooMock.Service.Init()`的conf文件地址

```bash
$ irma-genapp Foo -m
Generated projects: 'Foo' and 'FooMock' (which is for Windows OS). Check pls !
$ tree FooMock
FooMock/
├── App_Code
│   ├── FooMock.cs
│   └── MockHandler.cs
└── Web.config
```

> * 以`Visual Stdio IDE`为例，创建网站项目：FooMock（文件->新建->网站->选择Virtual C#模板内ASP.Net空网站）
> * 进一步导入`Foo`项目到`FooMock`所在当前解决方案、并将实际项目`Foo`内`Foo/Bin/Debug/{Foo.dll,IRMACore-windows.dll,IRMAKit-windows.dll}`三个dll作为引用导入`FooMock`内
> * 由于VS工具可直接通过端口启动应用（即不需要指定和配置应用名称，例如通过Chrome启动：`http://localhost:<port>`），采用该方式进行DEBUG时，请将`Foo/conf/Foo.conf`文件`system.app_name`设置为空即可（`"app_name": ""`）；若已明确通过`IIS`配置了应用名称，则要求`app_name`与IIS实际配置名称一致
