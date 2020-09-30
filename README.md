# IRMA

IRMA, an efficient web service framework, which originated from a business project and was created because of the comprehensive reasons such as the requirement of enterprise to adopt .Net, SaaS & cloud automated deployment and Linux running environment etc. IRMA is built on the Mono and the FastCGI. It uses Mono to implement the parsing and execution of C# code on Linux, and utilizes the FastCGI and Nginx to deal with the HTTP interaction. There are two parts of IRMA: `irmacall`, a multithreading scheduling engine written in C, and `irmakit`, a development framework written in C#. `irmacall` is responsible for launching & scheduling all kinds of web applications developed on `irmakit` framework. The design concept of IRMA is: ***Simple and efficient scheduling, on-demand integration and expansion***, which really makes the IRMA look more like a toolkit than a framework (In fact, it is a toolkit). Anyway, I hope it's useful to you :-)

## Compilation & Installation

Configure it at first:

```bash
$ make config
```

Here I assume that you have installed the relevant libraries on `$HOME/local/` (You might type the correct path according to your actual situation), so the configuration process will be roughly as follows:

```bash
########################################
# mono config:
########################################
mono installed location ? [/home/fenkey/local/mono]

########################################
# ssl config:
########################################
ssl installed location ? [/home/fenkey/local/openssl]

########################################
# fcgi config:
########################################
fcgi installed location ? [/home/fenkey/local/fcgi]

########################################
# curl config:
########################################
curl installed location ? [/home/fenkey/local/curl]
c_ares support ? [y]
c_ares installed location ? [/home/fenkey/local/c-ares]

########################################
# memcached config:
########################################
memcached support ? [y]
memcached installed location ? [/home/fenkey/local/libmemcached]

########################################
# redis config:
########################################
redis support ? [y]
hiredis installed location ? [/home/fenkey/local/hiredis]

########################################
# smtp config:
########################################
smtp support ? [y]

########################################
# log config:
########################################
log prefix (string with valid characters or numbers, of which the length is limited to 20): [irma]
log file generated hourly support ? [n]

The irma is now hopefully configured for your setup.
Check the config.in & config.h files and do 'make' to build it.
```

The source links of above dependencies might refer as below:

* `mono`: <a src="https://github.com/mono/mono.git">https://github.com/mono/mono.git</a>
* `openssl`: <a src="https://github.com/openssl/openssl.git">https://github.com/openssl/openssl.git</a>
* `fcgi`: <a src="http://fastcgi.com/">http://fastcgi.com/</a> or <a src="https://github.com/jorben/fcgi">https://github.com/jorben/fcgi</a>
* `c-ares`: <a src="https://github.com/c-ares/c-ares">https://github.com/c-ares/c-ares</a>
* `curl`: <a src="https://github.com/curl/curl.git">https://github.com/curl/curl.git</a>
* `memcached`: <a src="https://libmemcached.org/">https://libmemcached.org/</a>
* `hiredis`: <a src="https://github.com/redis/hiredis.git">https://github.com/redis/hiredis.git</a>

> Note to add SSL support (`--with-ssl`) when you compile libcurl. Because libcurl does not support the DNS asynchronous resolusion, you have to utilize the way of `signal`, or the third party package of `c-ares` that is the official recommendation of libcurl. We follow the advice to use `c-ares`:

```bash
$ ./configure --prefix=$HOME/local/c-ares --enable-static
$ make
$ make install
```

> Compile & install the libcurl:

```bash
$ ./buildconf
$ export PKG_CONFIG_PATH=$HOME/local/openssl/lib/pkgconfig
$ ./configure --prefix=$HOME/local/curl --disable-ldap --disable-ldaps --with-ssl --enable-ares=$HOME/local/c-ares --enable-static
$ make
$ make install
```

Two files are generated after the configuration about IRMA is done: `config.in` and `config.h`. Now let's go on

```bash
$ make
$ make install
```

By default we compile IRMA statically and it means that you can apply & run it in the least dependant way. You might change it by modifying the Makefile directly. We assume the installation path is `$HOME/local/irma` and modify the file `irma/Makefile`:

```bash
PREFIX = $(HOME)/local/irma
```

Set the environment PATH and reload it:

```bash
$ echo "export PATH=$HOME/local/irma/bin:$PATH" >> ~/.bash_profile
$ . ~/.bash_profile
```

Execute it:

```bash
$ irmacall
 ___ ____  __  __    _    ____      _ _
|_ _|  _ \|  \/  |  / \  / ___|__ _| | |
 | || |_) | |\/| | / _ \| |   / _` | | |
 | ||  _ <| |  | |/ ___ \ |__| (_| | | |
|___|_| \_\_|  |_/_/   \_\____\__,_|_|_|
+------------------------------------------------------------------------------------------------------------------+
| Usage: irmacall [-t <log-type>] [-x <thread-count>] [-m <module-invoke>] [-c <config-of-module>] [-k] [-v] [-h]  |
| Options:                                                                                                         |
|    -t: Log lever of 'debug', 'event', 'warn', 'error' or 'fatal'                                                 |
|    -x: Threads count of every process                                                                            |
|    -m: Module invoking. Normally, it's a .Net DLL                                                                |
|    -c: Configuration file of module                                                                              |
|    -k: Mock request support                                                                                      |
|    -v: Version of irmacall                                                                                       |
|    -h: Help information                                                                                          |
+------------------------------------------------------------------------------------------------------------------+

$ irmacall -v
irmacall 0.8
Features: fetcher fuse c_ares memcached redis smtp logprefx('irma')
```

## Quick Start

Create and launch the first project named `Foo` easily:

```bash
$ cd ~/tmp
$ irma-genapp Foo
Generated project: 'Foo'. Check pls !

$ cd Foo
$ make
...
$ ./start.sh
spawn-fcgi: child spawned successfully: PID: 17701
```

> Excuse me for reminding that you have to install the command `spawn-fcgi` before starting `Foo`:

```bash
$ git clone https://github.com/lighttpd/spawn-fcgi
$ cd spawn-fcgi
$ ./autogen.sh
$ ./configure --prefix=$HOME/local/spawn-fcgi
$ make
$ make install
$ echo "export PATH=$HOME/local/spawn-fcgi/bin:$PATH" >> ~/.bash_profile
$ . ~/.bash_profile
```

`Foo` is running in the way of multiprocess / multithreading and writes `debug` logs in the `Bin/Debug/log` directory. You might update these in the `start.sh` file:

```bash
process_count=1
thread_count=4
log_type="debug"
...
```

If `Foo` fails to start, you should confirm whether the memcached server is running (refer to the configuration of `system.session.server.servers` and `user.mc` in file `conf/Foo.conf`).

```bash
$ ./start.sh
!!!!!!!!!!!!!
!!! FATAL !!! - Fail to launch project 'Foo.dll' ! check log files pls
!!!!!!!!!!!!!

$ cat Bin/Debug/log/fatal/irma_20200903.log
[14:46:15,554039|032126|7f4d19fa7700] Kit - Service init failed: Memcached client instance is null: Availability testing is failed
[14:46:15,554132|032126|7f4d19fa7700] Core - Raise exception while invoking application ! Check it pls
```

If there are any other reasons, refer to [FAQ](./docs/FAQ.zh-CN.md) please. If success, the `log` files will be generated:

```bash
$ cat Bin/Debug/log/event/irma_20200903.log
[15:01:38,147495|032299|7f017783e780] Core - irmacall version(0.8)
[15:01:38,601612|032299|7f0173da6700] Kit - Service start
[15:01:38,688583|032299|7f0171e83700] Kit - Service start
[15:01:38,787017|032299|7f01710ff700] Kit - Service start
[15:01:38,875054|032299|7f017783e780] Core - Total (4) workers have been booted up successfully
[15:01:38,875149|032299|7f0153fff700] Kit - Service start
```

Note if you choose to support `loghourly` in `make config`, the log file name of above will be like `irma_2020090315.log`. Supporting `loghourly` will cause log files to be generated every hour.

Configure the nginx (Note to reload or restart it):

```bash
server {
	listen 8020;
	server_name localhost;
	
	...
	
	location ~ ^/Foo(/|$) {
		fastcgi_pass unix:/home/fenkey/tmp/Foo/Bin/Debug/irma.sock;
		include nginx_fastcgi.conf;
    }
}
```

> The `nginx_fastcgi.conf` file describes the mapping relation of the request parameters between Nginx and FastCGI. For example:

```bash
fastcgi_param  SERVER_NAME      $server_name;
fastcgi_param  SERVER_ADDR      $server_addr;
fastcgi_param  SERVER_PORT      $server_port;
fastcgi_param  SERVER_PROTOCOL  $server_protocol;
fastcgi_param  REMOTE_ADDR      $remote_addr;
fastcgi_param  REMOTE_PORT      $remote_port;
fastcgi_param  DOCUMENT_ROOT    $document_root;
fastcgi_param  SCRIPT_FILENAME  $document_root$fastcgi_script_name;
fastcgi_param  REQUEST_URI      $request_uri;
fastcgi_param  QUERY_STRING     $query_string;
fastcgi_param  REQUEST_METHOD   $request_method;
fastcgi_param  CONTENT_TYPE     $content_type;
fastcgi_param  CONTENT_LENGTH   $content_length;
```

Type address in browser to visit:
![Foo](./images/Foo.jpg)


## FAQ

For more details about IRMA, please refer to: [FAQ](./docs/FAQ.zh-CN.md)


## Contributing

You are welcome to join us in various ways e.g. submit your PR, documents or issues.
