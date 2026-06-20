FROM mono:6.12

# Install Nginx, Mono FastCGI server, and native SQLite library
RUN apt-get update && apt-get install -y \
    nginx \
    mono-fastcgi-server4 \
    libsqlite3-dev \
    && rm -rf /var/lib/apt/lists/*

# Set up working directory
WORKDIR /app

# Copy application files
COPY . /app

# Configure Nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Make start.sh executable
RUN chmod +x /app/start.sh

# Ensure uploads and App_Data directories exist and have proper permissions
RUN mkdir -p /app/uploads /app/App_Data && chmod -R 777 /app/uploads /app/App_Data

# Expose Nginx port
EXPOSE 80

# Start services
CMD ["/app/start.sh"]
