<!DOCTYPE html>
<html>
<head>
  <title>${appName}</title>
  <style>body {width: 50em; margin: 0 auto; font-family: Tahoma, Verdana, Arial, sans-serif;}</style>
</head>
<body>
  <h1><span style="color:GREEN">${appName}</span> that is developed base on <span style="color:GREEN">IRMAKit</span> !</h1>
  <p>If you see this page, the service of ${appName} is working successfully. Sample pages and APIs will show as below:</p>
  <p><ul>
    <li>User configuration: <a href="/${appName}/user_config">/user_config</a></li>
    <li>Information of http(s) request: <a href="/${appName}/request_params?name=Tom&age=100&nation=USA&favorite=dog&kid=4">/request_params</a></li>
    <li>Login: <a href="/${appName}/login?name=Fenkey">/login</a></li>
    <li>Security login (SSL): <a href="/${appName}/seclogin?name=Fenkey">/seclogin</a></li>
    <li>Check login: <a href="/${appName}/login_check">/login_check</a></li>
    <li>Logout: <a href="/${appName}/logout">/logout</a></li>
    <li>JSON api: <a href="/${appName}/jsonapi">/jsonapi</a></li>
    <li>Check parameters: <a href="/${appName}/params_check?name=Jack&age=99&nation=USA">/params_check</a></li>
    <li>Responses no-blockly: <a href="/${appName}/noblock?delay=10">/noblock</a></li>
    <li>RestFul api: <a href="/${appName}/restapi/Tom/99">/restapi</a></li>
    <li>Local api: <a href="/${appName}/localapi">/localapi</a></li>
    <li>Ip whitelist checking: <a href="/${appName}/ip_check">/ip_check</a></li>
    <li>Permission checking sample: <a href="/${appName}/permission_check">/permission_check</a></li>
    <li>Permission checking sample 2: <a href="/${appName}/permission_check2">/permission_check2</a></li>
    <li>Pseudo DNS: <a href="/${appName}/pseudodns">/pseudodns</a></li>
    <li>Exception throws: <a href="/${appName}/exception">/exception</a></li>
    <li>Fuse checking: <a href="/${appName}/fuse_check">/fuse_check</a></li>
    <li>Access static resource: <a href="/${appName}/s/n/1.html">/s/n/1.html</a></li>
    <li>Access protected static resource: <a href="/${appName}/s/c/2.html">/s/c/2.html</a></li>
    <li>Fetcher sample: <a href="/${appName}/fetcher">/fetcher</a></li>
    <li>QRCode sample (which refers to http://www.baidu.com): <a href="/${appName}/qr">/qr</a></li>
    <li>Ref(COVER mode) to request_params: <a href="/${appName}/refc/rq?name=Tom&age=100&nation=USA&favorite=dog&kid=4">/refc/rq</a></li>
    <li>Ref(COVER mode) missing: <a href="/${appName}/refc/">/refc/</a></li>
    <li>Ref(REPLACE mode) to restapi: <a href="/${appName}/refr/restapi/Tom/100">/refr/restapi/Jack/100</a></li>
    <li>Ref(REGULAR mode) to restapi: <a href="/${appName}/refx/restapi/Tom/100">/refx/restapi/Jack/100</a></li>
    <li>Proxy: <a href="/${appName}/proxy?name=Tom&age=100&nation=USA&favorite=dog&kid=4">/proxy</a></li>
    <li>Auto Proxy(COVER mode): <a href="/${appName}/auto_proxy_c/rq?name=Tom&age=100&nation=USA&favorite=dog&kid=4">/auto_proxy_c</a></li>
    <li>Auto Proxy(REPLACE mode) to restapi: <a href="/${appName}/auto_proxy_r/restapi/Tom/100">/auto_proxy_r/restapi/Jack/100</a></li>
    <li>Auto Proxy(REGULAR mode) to restapi: <a href="/${appName}/auto_proxy_x/restapi/Tom/100">/auto_proxy_x/restapi/Jack/100</a></li>
    <li>Des: <a href="/${appName}/des">/des</a></li>
    <li>Aes: <a href="/${appName}/aes">/aes</a></li>
    <li>Rsa: <a href="/${appName}/rsa">/rsa</a></li>
    <li>Cross-Origin access: <a href="/${appName}/cross_origin/">/cross_origin</a></li>
    <li>Download file: <a href="/${appName}/download">/download</a></li>
    <li>Memcached sample: <a href="/${appName}/memcached">/memcached</a></li>
  </ul></p>
  <p><em>Thank you for using IRMAKit.</em></p>
</body>
</html>
