#!/bin/sh

process_count=1
thread_count=4
log_type="debug"
dll=`pwd`/Bin/Debug/${appName}.dll
klass=${appName}.Web:MyService
config=`pwd`/conf/${appName}.conf

irma-launch -d $dll -k $klass -c $config -p $process_count -x $thread_count -t $log_type
