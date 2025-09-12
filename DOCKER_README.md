# Living Codex Docker Development Environment

This document describes how to set up and run the Living Codex system using Docker and Docker Compose for local development.

## 🚀 Quick Start

1. **Prerequisites**
   - Docker Desktop (latest version)
   - Docker Compose (latest version)
   - Git

2. **Setup**
   ```bash
   # Clone the repository
   git clone <repository-url>
   cd Living-Codex-CSharp

   # Run the setup script
   ./scripts/setup-dev.sh
   ```

3. **Access the Services**
   - Living Codex API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger
   - Grafana Dashboard: http://localhost:3000 (admin/admin)
   - Prometheus: http://localhost:9090

## 🏗️ Architecture

The Docker Compose setup includes the following services:

### Core Services
- **living-codex-api**: Main API service (.NET 6)
- **postgres**: PostgreSQL database
- **redis**: Redis cache
- **rabbitmq**: Message queue
- **ollama**: LLM service for AI features

### Monitoring & Observability
- **prometheus**: Metrics collection
- **grafana**: Dashboards and visualization
- **jaeger**: Distributed tracing
- **elasticsearch**: Log storage
- **kibana**: Log analysis

### Load Balancing & Security
- **nginx**: Load balancer and reverse proxy
- **adminer**: Database administration
- **redis-commander**: Redis administration

## 📁 Project Structure

```
Living-Codex-CSharp/
├── docker-compose.yml          # Main Docker Compose configuration
├── Dockerfile                  # API service Dockerfile
├── .dockerignore              # Docker build exclusions
├── scripts/
│   ├── setup-dev.sh           # Development setup script
│   └── init-db.sql            # Database initialization
├── nginx/
│   └── nginx.conf             # Nginx configuration
├── monitoring/
│   ├── prometheus.yml         # Prometheus configuration
│   └── grafana/               # Grafana dashboards and datasources
└── https/                     # SSL certificates (generated)
```

## 🔧 Configuration

### Environment Variables

The system uses the following environment variables (configured in `.env`):

```bash
# Application
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=your-super-secret-jwt-key-that-should-be-32-characters-long
ENCRYPTION_KEY=your-32-character-encryption-key-here

# Database
POSTGRES_PASSWORD=postgres

# Message Queue
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest

# Monitoring
GRAFANA_ADMIN_PASSWORD=admin
```

### Service Ports

| Service | Port | Description |
|---------|------|-------------|
| Living Codex API | 5000 | HTTP API |
| Living Codex API | 5001 | HTTPS API |
| Nginx | 80 | HTTP Load Balancer |
| Nginx | 443 | HTTPS Load Balancer |
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Cache |
| RabbitMQ | 5672 | Message Queue |
| RabbitMQ Management | 15672 | Web UI |
| Ollama | 11434 | LLM Service |
| Prometheus | 9090 | Metrics |
| Grafana | 3000 | Dashboards |
| Jaeger | 16686 | Tracing |
| Elasticsearch | 9200 | Log Storage |
| Kibana | 5601 | Log Analysis |
| Adminer | 8080 | DB Admin |
| Redis Commander | 8081 | Redis Admin |

## 🛠️ Development Commands

### Basic Operations

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f [service-name]

# Restart a service
docker-compose restart [service-name]

# Scale API instances
docker-compose up -d --scale living-codex-api=3
```

### Database Operations

```bash
# Connect to PostgreSQL
docker exec -it living-codex-postgres psql -U postgres -d livingcodex

# Run database migrations
docker exec living-codex-api dotnet ef database update

# Backup database
docker exec living-codex-postgres pg_dump -U postgres livingcodex > backup.sql

# Restore database
docker exec -i living-codex-postgres psql -U postgres livingcodex < backup.sql
```

### Application Operations

```bash
# Build the application
docker-compose build living-codex-api

# Run tests
docker exec living-codex-api dotnet test

# View application logs
docker-compose logs -f living-codex-api

# Access application shell
docker exec -it living-codex-api /bin/bash
```

### Monitoring Operations

```bash
# View Prometheus targets
curl http://localhost:9090/api/v1/targets

# Check Grafana health
curl http://localhost:3000/api/health

# View Jaeger traces
open http://localhost:16686

# Check Elasticsearch health
curl http://localhost:9200/_cluster/health
```

## 🔍 Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Check what's using a port
   lsof -i :5000
   
   # Kill process using port
   kill -9 <PID>
   ```

2. **Service Not Starting**
   ```bash
   # Check service logs
   docker-compose logs [service-name]
   
   # Check service status
   docker-compose ps
   ```

3. **Database Connection Issues**
   ```bash
   # Check PostgreSQL logs
   docker-compose logs postgres
   
   # Test database connection
   docker exec living-codex-postgres pg_isready -U postgres
   ```

4. **SSL Certificate Issues**
   ```bash
   # Regenerate SSL certificates
   rm -rf https/*
   ./scripts/setup-dev.sh
   ```

### Health Checks

```bash
# API Health
curl http://localhost:5000/health

# Database Health
docker exec living-codex-postgres pg_isready -U postgres

# Redis Health
docker exec living-codex-redis redis-cli ping

# RabbitMQ Health
curl http://localhost:15672/api/overview
```

## 📊 Monitoring & Observability

### Grafana Dashboards

Access Grafana at http://localhost:3000 (admin/admin) to view:

- **System Overview**: CPU, memory, disk usage
- **API Metrics**: Request rates, response times, error rates
- **Database Metrics**: Connection pools, query performance
- **Cache Metrics**: Hit rates, memory usage
- **Message Queue Metrics**: Queue depths, processing rates

### Prometheus Metrics

Access Prometheus at http://localhost:9090 to query metrics:

- `living_codex_api_requests_total`: Total API requests
- `living_codex_api_request_duration_seconds`: Request duration
- `living_codex_api_errors_total`: Total errors
- `postgres_up`: PostgreSQL availability
- `redis_up`: Redis availability

### Jaeger Tracing

Access Jaeger at http://localhost:16686 to trace requests:

- View distributed traces across services
- Analyze request flow and performance
- Debug issues with detailed trace information

## 🔒 Security

### SSL/TLS

The setup includes SSL certificates for HTTPS:

- Certificates are generated automatically
- Self-signed certificates for development
- Production should use proper certificates

### Authentication

- JWT-based authentication
- Role-based access control
- API rate limiting via Nginx

### Network Security

- Services communicate via internal Docker network
- External access only through Nginx
- Firewall rules can be applied to Docker network

## 🚀 Production Considerations

### Scaling

```bash
# Scale API instances
docker-compose up -d --scale living-codex-api=5

# Scale with resource limits
docker-compose up -d --scale living-codex-api=3
```

### Resource Limits

Add resource limits to `docker-compose.yml`:

```yaml
services:
  living-codex-api:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '1.0'
          memory: 2G
```

### Data Persistence

- Database data is persisted in Docker volumes
- Backup strategies should be implemented
- Consider using external storage for production

### Monitoring

- Set up alerting rules in Prometheus
- Configure Grafana alerts
- Implement log aggregation
- Set up health check endpoints

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [Redis Docker Image](https://hub.docker.com/_/redis)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
