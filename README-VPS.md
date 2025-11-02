# Hyper Control Panel - VPS Deployment Guide

This guide will help you deploy the Hyper Control Panel on a VPS with external access.

## 🌐 Access URLs

After deployment, you can access the control panel at:

### **Primary Access**
- **Main Panel**: `http://YOUR_VPS_IP:8080`
- **API Documentation**: `http://YOUR_VPS_IP:5050/swagger`
- **Portainer (Optional)**: `http://YOUR_VPS_IP:9443`

### **With Domain**
- **Main Panel**: `http://yourdomain.com:8080`
- **Subdomain**: `http://panel.yourdomain.com:8080`

### **With SSL**
- **HTTPS**: `https://yourdomain.com:8443`
- **HTTPS Subdomain**: `https://panel.yourdomain.com:8443`

## 🚀 VPS Deployment Steps

### 1. **Server Requirements**

**Minimum Requirements:**
- **RAM**: 4GB (8GB+ recommended)
- **Storage**: 20GB+ (50GB+ recommended)
- **CPU**: 2 cores (4+ recommended)
- **OS**: Ubuntu 20.04+ or Debian 11+
- **Docker**: 20.10+ and Docker Compose 2.0+

### 2. **Server Preparation**

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Create necessary directories
sudo mkdir -p /var/www/hypercontrol-sites
sudo mkdir -p /var/backups/hypercontrolpanel
sudo mkdir -p logs/{nginx,postgres,mysql,redis}

# Set permissions
sudo chown -R $USER:$USER /var/www/hypercontrol-sites
sudo chown -R $USER:$USER /var/backups/hypercontrolpanel
sudo chmod 755 /var/www/hypercontrol-sites
sudo chmod 755 /var/backups/hypercontrolpanel
```

### 3. **Clone and Configure**

```bash
# Clone the repository
git clone <your-repository-url>
cd Hyper-Control-Panel

# Copy VPS environment file
cp .env.vps .env

# Edit the environment file with your VPS details
nano .env
```

### 4. **Configure Environment Variables**

Edit `.env` and update these critical values:

```bash
# ⚠️ CHANGE THESE VALUES ⚠️
POSTGRES_PASSWORD=your_secure_postgres_password
MYSQL_ROOT_PASSWORD=your_secure_mysql_root_password
REDIS_PASSWORD=your_secure_redis_password
JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters

# Replace with your actual VPS IP or domain
CONTROL_PANEL_URL=http://YOUR_VPS_IP:8080
API_URL=http://YOUR_VPS_IP:5050
FRONTEND_URL=http://YOUR_VPS_IP:8080
DEFAULT_DOMAIN=YOUR_VPS_IP

# Optional: Configure email for SSL notifications
SMTP_USER=your_email@gmail.com
SMTP_PASSWORD=your_app_password
```

### 5. **Deploy the Application**

```bash
# Deploy using VPS configuration
docker-compose -f docker-compose-vps.yml up -d

# Check status
docker-compose -f docker-compose-vps.yml ps

# View logs
docker-compose -f docker-compose-vps.yml logs -f
```

### 6. **Verify Installation**

```bash
# Check if services are running
curl http://localhost:8080/health

# Check from your local machine
curl http://YOUR_VPS_IP:8080/health
```

### 7. **Firewall Configuration**

```bash
# Allow required ports
sudo ufw allow 8080/tcp    # HTTP access
sudo ufw allow 8443/tcp    # HTTPS access
sudo ufw allow 5050/tcp    # API access
sudo ufw allow 9443/tcp    # Portainer (optional)

# Enable firewall
sudo ufw enable
```

### 8. **First-Time Setup**

1. **Access the Panel**: Open `http://YOUR_VPS_IP:8080` in your browser
2. **Register Account**: Create your administrator account
3. **Login**: Access the dashboard
4. **Create Your First Site**: Use the site creation wizard

## 🔧 Domain Configuration

### **Option 1: Direct IP Access**
```
http://YOUR_VPS_IP:8080
```

### **Option 2: Domain with Port**
```
http://yourdomain.com:8080
```

### **Option 3: Subdomain with Port**
```
http://panel.yourdomain.com:8080
```

### **Option 4: Nginx Reverse Proxy (No Port)**
Create Nginx configuration on your VPS:

```nginx
server {
    listen 80;
    server_name panel.yourdomain.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## 🔒 SSL Certificate Setup

### **Self-Signed Certificate (Quick Setup)**
```bash
# Create SSL directory
sudo mkdir -p /etc/ssl/hypercontrol

# Generate self-signed certificate
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/ssl/hypercontrol/private.key \
    -out /etc/ssl/hypercontrol/certificate.crt \
    -subj "/C=US/ST=State/L=City/O=Organization/CN=YOUR_VPS_IP"
```

### **Let's Encrypt Certificate (Recommended)**
```bash
# Install Certbot
sudo apt install certbot

# Generate certificate
sudo certbot certonly --standalone -d panel.yourdomain.com

# Update environment file
SSL_CERT_PATH=/etc/letsencrypt/live/panel.yourdomain.com/fullchain.pem
SSL_KEY_PATH=/etc/letsencrypt/live/panel.yourdomain.com/privkey.pem
```

## 📊 Monitoring and Maintenance

### **Enable Monitoring Stack**
```bash
# Start monitoring services
docker-compose -f docker-compose-vps.yml --profile monitoring up -d

# Access monitoring
# Prometheus: http://YOUR_VPS_IP:9091
# Grafana: http://YOUR_VPS_IP:3001
```

### **Automated Backups**
```bash
# Configure backup schedule in .env
BACKUP_SCHEDULE="0 2 * * *"  # Daily at 2 AM
BACKUP_RETENTION_DAYS=30

# Set up S3 backup (optional)
S3_BUCKET=your-backup-bucket
AWS_ACCESS_KEY_ID=your-key
AWS_SECRET_ACCESS_KEY=your-secret
```

### **Log Management**
```bash
# View logs
docker-compose -f docker-compose-vps.yml logs -f control-panel
docker-compose -f docker-compose-vps.yml logs -f nginx

# Log rotation (add to crontab)
0 0 * * * docker system prune -f
```

## 🔍 Troubleshooting

### **Common Issues**

1. **Cannot Access Panel**
   ```bash
   # Check if services are running
   docker-compose -f docker-compose-vps.yml ps

   # Check logs
   docker-compose -f docker-compose-vps.yml logs nginx
   docker-compose -f docker-compose-vps.yml logs control-panel

   # Check port accessibility
   sudo netstat -tlnp | grep :8080
   ```

2. **Database Connection Issues**
   ```bash
   # Check database logs
   docker-compose -f docker-compose-vps.yml logs postgresql
   docker-compose -f docker-compose-vps.yml logs mysql

   # Restart services
   docker-compose -f docker-compose-vps.yml restart postgresql mysql
   ```

3. **Firewall Issues**
   ```bash
   # Check firewall status
   sudo ufw status

   # Allow ports if needed
   sudo ufw allow 8080/tcp
   sudo ufw reload
   ```

4. **Performance Issues**
   ```bash
   # Check resource usage
   docker stats

   # Check disk space
   df -h

   # Check memory usage
   free -h
   ```

### **Health Checks**
```bash
# Check all services health
docker-compose -f docker-compose-vps.yml exec control-panel curl -f http://localhost:5000/health

# Check API accessibility
curl -X GET http://YOUR_VPS_IP:5050/api/sites/stats

# Check frontend accessibility
curl -I http://YOUR_VPS_IP:8080
```

## 🚀 Production Optimization

### **Performance Tuning**
```bash
# Increase Docker log limits
echo '{"log-driver":"json-file","log-opts":{"max-size":"10m","max-file":"3"}}' | sudo tee /etc/docker/daemon.json
sudo systemctl restart docker

# Optimize MySQL performance
# Add to mysql.cnf:
# [mysqld]
# innodb_buffer_pool_size = 1G
# innodb_log_file_size = 256M
```

### **Security Hardening**
```bash
# Fail2Ban for SSH protection
sudo apt install fail2ban

# Regular security updates
sudo apt update && sudo apt upgrade -y

# Monitor access logs
sudo tail -f /var/log/nginx/access.log
```

## 📱 Mobile Access

The control panel is fully responsive and works on mobile devices. Simply access `http://YOUR_VPS_IP:8080` from your mobile browser.

## 🔄 Updates

```bash
# Update the application
git pull
docker-compose -f docker-compose-vps.yml down
docker-compose -f docker-compose-vps.yml build --no-cache
docker-compose -f docker-compose-vps.yml up -d
```

Your Hyper Control Panel is now accessible from anywhere on the internet! 🎉