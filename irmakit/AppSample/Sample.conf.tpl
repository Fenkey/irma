{
	"system": {
		"app_name": "${appName}",
		"version": "v1.0.0",
		"release_info": "2020-08-20",
		"app_charset": "utf-8",
		"session": {
			"server": {
				"servers": "localhost:11211",
				"instance": "${appName}-session",
				"expire": "2h"
			},
			"client": {
				"cookie_name": "${appName}-sessionid",
				"cookie_path": "",
				"cookie_domain": "",
				"cookie_expire": 0
			}
		},
		"performance": "${appName}.Web.HandlePerformance",
		"routers": [
			{ "path": "^/s/n/",              "handler": "${appName}.Web.StaticHandler",          "methods": "GET"                  },
			{ "path": "^/s/c/",              "handler": "${appName}.Web.CStaticHandler",         "methods": "GET"                  },
			{ "path": "^/$",                 "handler": "${appName}.Web.IndexHandler",           "methods": "GET",      "pf": true },
			{ "path": "@/user_config",       "handler": "${appName}.Web.UserConfigHandler",      "methods": "GET",      "pf": true },
			{ "path": "@/login",             "handler": "${appName}.Web.LoginHandler",           "methods": "GET|POST", "pf": true },
			{ "path": "@/seclogin",          "handler": "${appName}.Web.SecLoginHandler",        "methods": "GET|POST", "pf": true },
			{ "path": "@/login_check",       "handler": "${appName}.Web.LoginCheckHandler",      "methods": "GET|POST", "pf": true },
			{ "path": "@/logout",            "handler": "${appName}.Web.LogoutHandler",          "methods": "GET|POST", "pf": true },
			{ "path": "@/request_params",    "handler": "${appName}.Web.RequestParamsHandler",   "methods": "*",        "pf": true },
			{ "path": "@/jsonapi",           "handler": "${appName}.Web.JsonApiHandler",         "methods": "GET",      "pf": true },
			{ "path": "@/localapi",          "handler": "${appName}.Web.LocalApiHandler",        "methods": "GET|POST", "pf": true },
			{ "path": "@/ip_check",          "handler": "${appName}.Web.IpCheckHandler",         "methods": "GET|POST", "pf": true },
			{ "path": "@/params_check",      "handler": "${appName}.Web.ParamsCheckHandler",     "methods": "GET|POST", "pf": true },
			{ "path": "@/noblock",           "handler": "${appName}.Web.NoBlockHandler",         "methods": "GET",      "pf": true },
			{ "path": "^/restapi/",          "handler": "${appName}.Web.RestApiHandler",         "methods": "GET",      "pf": true },
			{ "path": "@/permission_check",  "handler": "${appName}.Web.PermissionCheckHandler", "methods": "*",        "pf": true },
			{ "path": "@/permission_check2", "handler": "${appName}.Web.PermissionCheckHandler2","methods": "*",        "pf": true },
			{ "path": "@/des",               "handler": "${appName}.Web.DesHandler",             "methods": "GET",      "pf": true },
			{ "path": "@/aes",               "handler": "${appName}.Web.AesHandler",             "methods": "GET",      "pf": true },
			{ "path": "@/rsa",               "handler": "${appName}.Web.RsaHandler",             "methods": "GET",      "pf": true },
			{ "path": "@/pseudodns",         "handler": "${appName}.Web.PseudoDNSHandler",       "methods": "GET|POST", "pf": true },
			{ "path": "@/fetcher",           "handler": "${appName}.Web.FetcherHandler",         "methods": "GET",      "pf": true },
			{ "path": "@/qr",                "handler": "${appName}.Web.QRHandler",              "methods": "GET",      "pf": true },
			{ "path": "^/refc/",             "handler": "${appName}.Web.RefCHandler",            "methods": "GET",      "pf": true },
			{ "path": "^/refr/",             "handler": "${appName}.Web.RefRHandler",            "methods": "GET",      "pf": true },
			{ "path": "^/refx/",             "handler": "${appName}.Web.RefXHandler",            "methods": "GET",      "pf": true },
			{ "path": "@/proxy",             "handler": "${appName}.Web.ProxyHandler",           "methods": "GET",      "pf": true },
			{ "path": "^/auto_proxy_c",      "handler": "${appName}.Web.AutoProxyCHandler",      "methods": "GET|POST", "pf": true },
			{ "path": "^/auto_proxy_r",      "handler": "${appName}.Web.AutoProxyRHandler",      "methods": "GET|POST", "pf": true },
			{ "path": "^/auto_proxy_x",      "handler": "${appName}.Web.AutoProxyXHandler",      "methods": "GET|POST", "pf": true },
			{ "path": "^/cross_origin",      "handler": "${appName}.Web.CrossOriginHandler",     "methods": "GET",      "pf": true },
			{ "path": "^/download",          "handler": "${appName}.Web.DownloadHandler",        "methods": "GET",      "pf": true },
			{ "path": "@/memcached",         "handler": "${appName}.Web.MemcachedHandler",       "methods": "GET|POST", "pf": true },
			{ "path": "@/exception",         "handler": "${appName}.Web.ExceptionHandler",       "methods": "*"                    },
			{ "path": "@/fuse_check",        "handler": "${appName}.Web.FuseCheckHandler",       "methods": "*"                    }
		]
	},
	"user": {
		"ip_whitelist": "192.168.146.0/24, 192.169.0.0/16, 127.0.0.1",
		"unit": {
			"number": 100,
			"size": [ 123, "1b", "1k", "1m", "1g" ],
			"time": [ 456, "1s", "1m", "1h", "1d", "1w", "1M", "1y" ]
		},
		"dbs": { "host": "localhost", "port": 3306, "user": "mytest", "password": "123", "db": "mydb" },
		"mc": { "servers": "localhost:11211", "instance": "mc", "expire": 3600 },
		"smtp": { "server": "smtp://smtp.exmail.qq.com:25", "user": "myaccount", "password": "123" }
	}
}
