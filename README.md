# Hyper Control Panel

A self-hosted WordPress Multisite alternative that enables users to easily create and manage multiple websites with one-click deployment, automatic domain setup, and centralized management.

## Features

- **One-Click Site Creation**: Create new websites with custom domains or subdomains in minutes
- **Template System**: Deploy WordPress, static sites, and web applications from pre-configured templates
- **Domain Management**: Automatic DNS configuration and SSL certificate provisioning
- **File Management**: Web-based file browser with upload/download capabilities
- **Database Provisioning**: Automatic MySQL database creation for each site
- **Resource Monitoring**: Track usage and enforce limits per site
- **Multi-Platform Support**: WordPress, Hugo, Jekyll, Laravel, Node.js, and custom HTML sites

## Architecture

- **Backend**: ASP.NET Core 8.0 Web API with PostgreSQL
- **Frontend**: React 18 with TypeScript and Material-UI
- **Database**: PostgreSQL (control panel) + MySQL (site databases)
- **Container**: Docker with Docker Compose orchestration
- **Web Server**: Nginx reverse proxy with SSL termination

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for development)
- Node.js 18+ (for frontend development)

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd Hyper-Control-Panel
```

2. Copy environment configuration:
```bash
cp .env.example .env
# Edit .env with your configuration
```

3. Start the services:
```bash
docker-compose up -d
```

4. Access the control panel at `http://localhost:3000`

### Development

**Backend Development:**
```bash
cd backend/HyperControlPanel.API
dotnet run
```

**Frontend Development:**
```bash
cd frontend/hyper-control-panel
npm install
npm start
```

## Project Structure

```
├── backend/                    # ASP.NET Core Web API
│   └── HyperControlPanel.API/
├── frontend/                   # React TypeScript application
│   └── hyper-control-panel/
├── nginx/                      # Nginx configuration
├── templates/                  # Site templates
│   ├── wordpress/
│   ├── hugo/
│   └── custom/
├── scripts/                    # Deployment and utility scripts
├── docs/                       # Documentation
├── docker-compose.yml          # Development environment
├── docker-compose.prod.yml     # Production environment
└── .env.example               # Environment variables template
```

## License

MIT License - see LICENSE file for details.