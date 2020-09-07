/*-------------------------------------------------------------------------------------------------------------------------------------------------------
-*- nginx.conf reference

server {
	listen		8020;
	server_name	localhost;

	location ~ ^/Foo(/|$) {
		fastcgi_pass unix:/home/fenkey/tmp/test/here/Foo/Bin/Debug/irma.sock;
		include nginx_fastcgi.conf;
		#client_max_body_size 100m;
	}
}

server {
	listen		8443 ssl;
	server_name	localhost;

	ssl							on;
	ssl_verify_client			on;
	ssl_protocols				TLSv1.1 TLSv1.2;
	ssl_certificate				server-cert/server.crt;
	ssl_certificate_key			server-cert/server.key;
	ssl_client_certificate		ca.crt;
	ssl_session_cache			shared:SSL:1m;
	ssl_session_timeout			5m;
	ssl_ciphers					HIGH:!aNULL:!MD5;
	ssl_prefer_server_ciphers	on;

	location ~ ^/Foo(/|$) {
		#fastcgi_param HTTP_SSL_DN $ssl_client_s_dn;
		fastcgi_pass unix:/home/fenkey/tmp/test/here/Foo/Bin/Debug/irma.sock;
		include nginx_fastcgi.conf;
		#client_max_body_size 100m;
	}
}

-*- nginx_fastcgi.conf reference

fastcgi_param  SERVER_NAME      $server_name;
fastcgi_param  SERVER_ADDR      $server_addr;
fastcgi_param  SERVER_PORT      $server_port;
fastcgi_param  SERVER_PROTOCOL  $server_protocol;

fastcgi_param  REMOTE_ADDR      $remote_addr;
fastcgi_param  REMOTE_PORT      $remote_port;

fastcgi_param  HTTPS            $https if_not_empty;
fastcgi_param  HTTP_SSL_DN		$ssl_client_s_dn;
fastcgi_param  DOCUMENT_ROOT    $document_root;
fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
fastcgi_param  REQUEST_URI      $request_uri;
fastcgi_param  QUERY_STRING     $query_string;
fastcgi_param  REQUEST_METHOD   $request_method;
fastcgi_param  CONTENT_TYPE     $content_type;
fastcgi_param  CONTENT_LENGTH   $content_length;

client_max_body_size 1g;
-------------------------------------------------------------------------------------------------------------------------------------------------------*/

#include "icall.h"

extern void reg_sys();
extern void reg_log();
extern void reg_fcgi();
#ifdef SUPPORT_SMTP
extern void reg_smtp();
#endif
extern void reg_fetcher();
extern void reg_md5();
extern void reg_sha();
extern void reg_des();
extern void reg_aes();
extern void reg_rsa();
extern void reg_unit();
#if defined(SUPPORT_MEMCACHED) || defined(SUPPORT_REDIS)
extern void reg_kvs();
#endif
extern void reg_gzip();
extern void reg_ver();

void register_icall()
{
	reg_sys();
	reg_log();
	reg_fcgi();
	#ifdef SUPPORT_SMTP
	reg_smtp();
	#endif
	reg_fetcher();
	reg_md5();
	reg_sha();
	reg_des();
	reg_aes();
	reg_rsa();
	reg_unit();
	#if defined(SUPPORT_MEMCACHED) || defined(SUPPORT_REDIS)
	reg_kvs();
	#endif
	reg_gzip();
	reg_ver();
}
