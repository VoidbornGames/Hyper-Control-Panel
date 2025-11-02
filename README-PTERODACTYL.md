# Hyper Control Panel - Pterodactyl Compatible Installation

This guide will help you install the Hyper Control Panel alongside an existing Pterodactyl installation without conflicts.

## Port Allocation Strategy

| Service | Hyper Control Panel | Pterodactyl | Notes |
|---------|-------------------|-------------|-------|
| Web Interface | `http://localhost:8080` | `http://localhost:80/443` | Different ports |
| API | `http://localhost:5050` | `http://localhost:8080` | Different ports |
| Frontend Dev | `http://localhost:3100` | N/A | Development only |
| PostgreSQL | `localhost:5433` | N/A | Pterodactyl uses MySQL |
| MySQL | `localhost:3307` | `localhost:3306` | Different ports |
| Redis | `localhost:6380` | `localhost:6379` | Different ports |
| Portainer | `http://localhost:9443` | N/A | Optional tool |

## Installation Steps

### 1. **Preparation**

```bash
# Ensure you have enough disk space and RAM
# Recommended: 16GB RAM, 50GB+ free storage

# Stop any conflicting services if needed
sudo systemctl stop apache2  # if running on port 8080
```

### 2. **Clone and Setup**

```bash
git clone <your-repository-url>
cd Hyper-Control-Panel

# Copy Pterodactyl-compatible environment file
cp .env.pterodactyl .env

# Edit the environment file
nano .env
```

### 3. **Configure Environment Variables**

Edit `.env` with these key settings:

```bash
# Database passwords (CHANGE THESE!)
POSTGRES_PASSWORD=your_secure_postgres_password
MYSQL_ROOT_PASSWORD=your_secure_mysql_root_password
REDIS_PASSWORD=your_secure_redis_password

# JWT secret (generate a long random string)
JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters_long_for_security

# File paths (separate from Pterodactyl)
SITES_ROOT_DIR=/var/www/hypercontrol-sites
BACKUP_DIR=/var/backups/hypercontrolpanel

# Network configuration
NETWORK_SUBNET=172.21.0.0/16
NETWORK_NAME=hypercontrol-br0
```

### 4. **Create Directories**

```bash
# Create separate directories for Hyper Control Panel
sudo mkdir -p /var/www/hypercontrol-sites
sudo mkdir -p /var/backups/hypercontrolpanel
sudo mkdir -p logs/{nginx,postgres,mysql,redis}

# Set proper permissions
sudo chown -R $USER:$USER /var/www/hypercontrol-sites
sudo chown -R $USER:$USER /var/backups/hypercontrolpanel
sudo chmod 755 /var/www/hypercontrol-sites
sudo chmod 755 /var/backups/hypercontrolpanel
```

### 5. **Start Services**

#### **Development Mode:**
```bash
docker-compose -f docker-compose-pterodactyl.yml up -d
```

#### **Production Mode:**
```bash
docker-compose -f docker-compose-pterodactyl.prod.yml up -d
```

### 6. **Access the Control Panel**

- **Hyper Control Panel**: `http://localhost:8080`
- **Pterodactyl Panel**: `http://localhost` (unchanged)
- **API Documentation**: `http://localhost:5050/swagger`

## Port Conflict Resolution

### If you encounter port conflicts:

1. **Check what's using the ports:**
   ```bash
   sudo netstat -tlnp | grep :8080
   sudo netstat -tlnp | grep :5050
   sudo netstat -tlnp | grep :3100
   ```

2. **Modify ports in `.env`:**
   ```bash
   # Change these values
   API_PORT=5060
   FRONTEND_URL=http://localhost:3110
   CONTROL_PANEL_URL=http://localhost:8090
   ```

3. **Update docker-compose ports:**
   ```yaml
   ports:
     - "5060:5000"  # API
     - "3110:3000"  # Frontend
     - "8090:80"    # Nginx
   ```

## Network Isolation

The systems use separate Docker networks:

- **Pterodactyl**: Uses default Docker networks
- **Hyper Control Panel**: Uses dedicated network `hypercontrol-br0` (172.21.0.0/16)

This ensures complete isolation between the two systems.

## Database Separation

- **Pterodactyl**: MySQL on port 3306
- **Hyper Control Panel**:
  - PostgreSQL on port 5433 (control panel data)
  - MySQL on port 3307 (site databases)

## File System Separation

```
/var/www/
├── pterodactyl/          # Pterodactyl servers
└── hypercontrol-sites/   # Hyper Control Panel sites

/var/backups/
├── pterodactyl/          # Pterodactyl backups
└── hypercontrolpanel/    # Hyper Control Panel backups
```

## SSL Certificate Management

### Option 1: Separate Domains
- Pterodactyl: `panel.yourdomain.com`
- Hyper Control Panel: `sites.yourdomain.com`

### Option 2: Subdirectories
- Pterodactyl: `yourdomain.com/panel`
- Hyper Control Panel: `yourdomain.com/sites`

### Option 3: Different Ports
- Pterodactyl: `yourdomain.com` (port 80/443)
- Hyper Control Panel: `yourdomain.com:8080`

## Monitoring Stack

Enable monitoring without conflicting with Pterodactyl:

```bash
# Start monitoring services on different ports
docker-compose -f docker-compose-pterodactyl.prod.yml --profile monitoring up -d

# Access:
# Prometheus: http://localhost:9091
# Grafana: http://localhost:3001
```

## Backup Strategy

Both systems have independent backup systems:

```bash
# Hyper Control Panel backups
BACKUP_DIR=/var/backups/hypercontrolpanel

# Pterodactyl backups (unchanged)
/var/lib/pterodactyl/backups
```

## Troubleshooting

### Common Issues:

1. **Port Conflicts:**
   ```bash
   # Check for conflicts
   ss -tlnp | grep -E ":(8080|5050|3100|5433|3307|6380)"
   ```

2. **Docker Network Conflicts:**
   ```bash
   # List networks
   docker network ls

   # Remove conflicting network
   docker network rm hypercontrol-br0
   ```

3. **Permission Issues:**
   ```bash
   # Fix permissions
   sudo chown -R $USER:docker /var/www/hypercontrol-sites
   sudo usermod -aG docker $USER
   ```

4. **Service Conflicts:**
   ```bash
   # Check if services are running
   docker-compose -f docker-compose-pterodactyl.yml ps

   # Restart specific service
   docker-compose -f docker-compose-pterodactyl.yml restart control-panel
   ```

## Integration Points

### 1. **Docker Integration**
Both systems can share the same Docker daemon safely.

### 2. **User Management**
Users are separate between the two systems.

### 3. **Domain Management**
- Pterodactyl: Game servers
- Hyper Control Panel: Websites/blogs

### 4. **Resource Allocation**
Each system manages its own resource limits independently.

## Security Considerations

1. **Network Isolation**: Separate Docker networks prevent cross-contamination
2. **File System Separation**: Different directories prevent file conflicts
3. **Port Separation**: Different ports prevent service conflicts
4. **Database Separation**: Separate databases prevent data conflicts
5. **User Isolation**: Different user management systems

## Performance Optimization

1. **Resource Limits**: Configure appropriate limits for each system
2. **Monitoring**: Monitor resource usage for both systems
3. **Backup Scheduling**: Stagger backup times to avoid conflicts
4. **SSL Renewal**: Schedule SSL renewals at different times

This setup ensures both Pterodactyl and Hyper Control Panel can coexist peacefully on the same server without conflicts.