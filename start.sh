#!/bin/bash

# Start Mono FastCGI server in the background
echo "Starting FastCGI Mono Server..."
fastcgi-mono-server4 /applications=/:/app /socket=tcp:127.0.0.1:9000 &

# Start Nginx in the foreground
echo "Starting Nginx..."
nginx -g "daemon off;"
