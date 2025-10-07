# Living Codex Production Runbook

## Table of Contents
1. [System Overview](#system-overview)
2. [Deployment Procedures](#deployment-procedures)
3. [Monitoring & Alerting](#monitoring--alerting)
4. [Health Checks](#health-checks)
5. [Memory Management](#memory-management)
6. [Troubleshooting Guide](#troubleshooting-guide)
7. [Incident Response](#incident-response)
8. [Rollback Procedures](#rollback-procedures)
9. [Performance Optimization](#performance-optimization)
10. [Security Considerations](#security-considerations)

## System Overview

The Living Codex is a production-ready, horizontally scalable system built on .NET 6 with the following key components:

### Core Infrastructure
- **Backend**: ASP.NET Core 6.0 API with modular architecture
- **Frontend**: Next.js 14 with TypeScript and React
- **Database**: SQLite (development) / PostgreSQL (production)
- **Session Storage**: Redis-based distributed sessions with SQLite fallback
- **Monitoring**: Prometheus + Grafana + Jaeger
- **Containerization**: Docker + Kubernetes
- **Load Balancing**: Nginx

### Key Features
- ✅ Memory leak prevention with automatic cleanup
- ✅ Graceful shutdown handling
- ✅ Distributed session storage
- ✅ Comprehensive health monitoring
- ✅ Performance optimization with caching
- ✅ AI pipeline monitoring
- ✅ Prometheus metrics integration
- ✅ Production deployment automation

## Deployment Procedures

### Prerequisites
- Docker & Docker Compose
- Kubernetes cluster (for K8s deployment)
- Redis instance (for production)
- PostgreSQL instance (for production)

### Environment Variables
```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5002

# Database Configuration
CONNECTION_STRING=Host=postgres-host;Database=livingcodex;Username=user;Password=pass
SQLITE_CONNECTION_STRING=Data Source=./ice_prod.db

# Redis Configuration (Production)
REDIS_CONNECTION_STRING=redis://redis-host:6379
ENABLE_DISTRIBUTED_SESSIONS=true
SESSION_STORAGE_ENABLE_FALLBACK=true

# Performance Optimization
ENABLE_PERFORMANCE_OPTIMIZATION=true
ENABLE_RESPONSE_CACHING=true
ENABLE_DATABASE_OPTIMIZATION=true

# AI Pipeline Monitoring
ENABLE_AI_PIPELINE_MONITORING=true

# Security
JWT_SECRET=your-secure-jwt-secret-key
JWT_EXPIRATION_HOURS=24

# Monitoring
PROMETHEUS_ENABLED=true
GRAFANA_ENABLED=true
```

### Deployment Methods

#### 1. Docker Compose (Recommended for Staging)
```bash
# Deploy to staging
./deploy-staging.sh

# Validate deployment
./validate-deployment.sh staging
```

#### 2. Kubernetes (Production)
```bash
# Deploy to production
./deploy-production.sh

# Validate deployment
./validate-deployment.sh production
```

#### 3. Manual Deployment
```bash
# Build and run locally
cd /path/to/Living-Codex-CSharp
dotnet build
./start-server.sh

# Check health
curl http://localhost:5002/health
```

### Deployment Validation Checklist
- [ ] All health endpoints responding (200 OK)
- [ ] Database connections established
- [ ] Redis connectivity verified
- [ ] Memory usage within normal limits
- [ ] No critical errors in logs
- [ ] All modules loaded successfully
- [ ] Prometheus metrics exposed
- [ ] AI pipeline monitoring active

## Monitoring & Alerting

### Health Endpoints
| Endpoint | Purpose | Expected Response |
|----------|---------|-------------------|
| `/health` | Overall system health | 200 OK with module status |
| `/health/memory` | Memory usage and leaks | 200 OK with memory metrics |
| `/health/ai-pipeline` | AI pipeline status | 200 OK with AI metrics |
| `/metrics/prometheus` | Prometheus metrics | 200 OK with metrics data |

### Key Metrics to Monitor
- **Memory Usage**: Heap size, GC pressure, memory leaks
- **Request Metrics**: P95/P99 latency, throughput, error rates
- **Session Management**: Active sessions, cleanup events
- **Database**: Connection pool, query performance, locks
- **AI Pipeline**: Request count, processing time, queue depth

### Alerting Thresholds
```yaml
# Memory Alerts
memory_usage_percent > 80%: WARNING
memory_usage_percent > 90%: CRITICAL
memory_leak_detected: CRITICAL

# Performance Alerts
p95_latency > 500ms: WARNING
p95_latency > 1000ms: CRITICAL
error_rate > 1%: WARNING
error_rate > 5%: CRITICAL

# Health Alerts
health_score < 80: WARNING
health_score < 60: CRITICAL
module_failures > 0: WARNING
```

## Health Checks

### Automated Health Checks
The system includes comprehensive health monitoring:

```bash
# Check overall health
curl http://localhost:5002/health | jq '.'

# Check memory health
curl http://localhost:5002/health/memory | jq '.'

# Check AI pipeline health
curl http://localhost:5002/health/ai-pipeline | jq '.'
```

### Health Status Interpretation
- **Healthy**: All modules loaded, memory usage normal, no critical errors
- **Degraded**: Some non-critical issues, but system functional
- **Unhealthy**: Critical failures, system may be unstable

### Manual Health Verification
```bash
# Check module loading status
curl http://localhost:5002/health | jq '.moduleLoading'

# Check memory usage
curl http://localhost:5002/health/memory | jq '.memoryUsage'

# Check system metrics
curl http://localhost:5002/metrics/prometheus | grep -E "(memory|sessions|requests)"
```

## Memory Management

### Memory Leak Prevention
The system includes automatic memory leak prevention:

- **Session Cleanup**: Automatic cleanup of expired sessions
- **Token Management**: Automatic revocation of expired tokens
- **Module Disposal**: Proper disposal of module resources
- **Timer Management**: Automatic cleanup of background timers

### Memory Monitoring
```bash
# Check memory usage
curl http://localhost:5002/health/memory

# Monitor memory trends
watch -n 5 'curl -s http://localhost:5002/health/memory | jq ".memoryUsage"'
```

### Memory Optimization Settings
```bash
# Enable memory optimization
export ENABLE_MEMORY_OPTIMIZATION=true
export GC_MODE=Server
export HEAP_SIZE_LIMIT=2GB
```

## Troubleshooting Guide

### Common Issues

#### 1. Server Won't Start
**Symptoms**: Server fails to start, port conflicts
**Diagnosis**:
```bash
# Check for port conflicts
lsof -i :5002

# Check for running processes
ps aux | grep dotnet

# Kill conflicting processes
./stop-servers.sh
```
**Solution**: Stop conflicting processes, restart server

#### 2. Memory Issues
**Symptoms**: High memory usage, OutOfMemoryException
**Diagnosis**:
```bash
# Check memory health
curl http://localhost:5002/health/memory

# Check for memory leaks
curl http://localhost:5002/health/memory | jq '.memoryLeaks'
```
**Solution**: Restart application, check for memory leaks

#### 3. Database Connection Issues
**Symptoms**: Database errors, connection timeouts
**Diagnosis**:
```bash
# Check database health
curl http://localhost:5002/health | jq '.database'

# Check connection string
echo $CONNECTION_STRING
```
**Solution**: Verify connection string, check database availability

#### 4. Module Loading Failures
**Symptoms**: Modules not loading, initialization errors
**Diagnosis**:
```bash
# Check module status
curl http://localhost:5002/health | jq '.moduleLoading'

# Check logs for errors
tail -f logs/server.log | grep -i error
```
**Solution**: Check module dependencies, verify configuration

#### 5. Performance Issues
**Symptoms**: Slow response times, high latency
**Diagnosis**:
```bash
# Check performance metrics
curl http://localhost:5002/metrics/prometheus | grep latency

# Check AI pipeline performance
curl http://localhost:5002/health/ai-pipeline
```
**Solution**: Enable performance optimization, check resource usage

### Log Analysis
```bash
# View recent errors
tail -f logs/server.log | grep -i error

# View memory-related logs
tail -f logs/server.log | grep -i memory

# View module loading logs
tail -f logs/server.log | grep -i module
```

## Incident Response

### Incident Classification
- **P1 - Critical**: System down, data loss, security breach
- **P2 - High**: Major functionality affected, performance severely degraded
- **P3 - Medium**: Minor functionality affected, performance impacted
- **P4 - Low**: Cosmetic issues, minor performance impact

### Response Procedures

#### P1 - Critical Incidents
1. **Immediate Response** (0-15 minutes)
   - Assess system status
   - Check health endpoints
   - Identify root cause
   - Implement immediate fix or workaround

2. **Escalation** (15-30 minutes)
   - Notify stakeholders
   - Create incident ticket
   - Document findings

3. **Resolution** (30-60 minutes)
   - Implement permanent fix
   - Validate system stability
   - Update monitoring

4. **Post-Incident** (24-48 hours)
   - Conduct post-mortem
   - Update runbook
   - Implement preventive measures

#### P2 - High Priority Incidents
1. **Response** (0-30 minutes)
   - Assess impact
   - Implement fix
   - Monitor resolution

2. **Follow-up** (24 hours)
   - Document incident
   - Update procedures

### Emergency Contacts
- **On-Call Engineer**: [Contact Information]
- **DevOps Team**: [Contact Information]
- **Management**: [Contact Information]

## Rollback Procedures

### Automated Rollback
```bash
# Rollback to previous version
./rollback.sh

# Rollback to specific version
./rollback.sh --version=v1.2.3

# Validate rollback
./validate-deployment.sh
```

### Manual Rollback
1. **Stop Current Deployment**
   ```bash
   ./stop-servers.sh
   ```

2. **Restore Previous Version**
   ```bash
   git checkout previous-stable-version
   dotnet build
   ```

3. **Restart Services**
   ```bash
   ./start-server.sh
   ```

4. **Validate Rollback**
   ```bash
   curl http://localhost:5002/health
   ```

### Database Rollback
```bash
# Backup current database
pg_dump livingcodex > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore previous database
psql livingcodex < backup_previous.sql
```

## Performance Optimization

### Production Optimizations
- **Response Caching**: Enabled for static content
- **Database Optimization**: Connection pooling, query optimization
- **Memory Management**: Automatic cleanup, leak prevention
- **Load Balancing**: Nginx configuration optimized

### Performance Monitoring
```bash
# Check performance metrics
curl http://localhost:5002/metrics/prometheus | grep -E "(latency|throughput|memory)"

# Monitor AI pipeline performance
curl http://localhost:5002/health/ai-pipeline | jq '.metrics'
```

### Optimization Settings
```bash
# Enable all optimizations
export ENABLE_PERFORMANCE_OPTIMIZATION=true
export ENABLE_RESPONSE_CACHING=true
export ENABLE_DATABASE_OPTIMIZATION=true
```

## Security Considerations

### Authentication & Authorization
- JWT-based authentication
- Token expiration and revocation
- Role-based access control
- Secure session management

### Data Protection
- Encrypted connections (HTTPS)
- Secure database connections
- Environment variable protection
- Input validation and sanitization

### Monitoring & Auditing
- Security event logging
- Access pattern monitoring
- Failed authentication tracking
- Suspicious activity detection

### Security Checklist
- [ ] HTTPS enabled in production
- [ ] JWT secrets properly configured
- [ ] Database credentials secured
- [ ] Redis connections encrypted
- [ ] Input validation enabled
- [ ] Security headers configured
- [ ] Regular security updates applied

## Maintenance Procedures

### Daily Maintenance
- [ ] Check health endpoints
- [ ] Review error logs
- [ ] Monitor memory usage
- [ ] Verify backup completion

### Weekly Maintenance
- [ ] Review performance metrics
- [ ] Update dependencies
- [ ] Check security updates
- [ ] Validate monitoring alerts

### Monthly Maintenance
- [ ] Full system health check
- [ ] Performance optimization review
- [ ] Security audit
- [ ] Disaster recovery testing

## Appendix

### Useful Commands
```bash
# Server Management
./start-server.sh          # Start server
./stop-servers.sh          # Stop all servers
./restart-server.sh        # Restart server

# Health Checks
curl http://localhost:5002/health
curl http://localhost:5002/health/memory
curl http://localhost:5002/health/ai-pipeline

# Monitoring
curl http://localhost:5002/metrics/prometheus
tail -f logs/server.log

# Deployment
./deploy-staging.sh
./deploy-production.sh
./validate-deployment.sh
```

### Configuration Files
- `docker-compose.yml` - Docker Compose configuration
- `k8s/` - Kubernetes deployment manifests
- `config/` - Application configuration
- `nginx/nginx.conf` - Nginx configuration

### Log Locations
- `logs/server.log` - Main application log
- `logs/request-tracker.log` - Request tracking log
- Docker logs: `docker logs <container-name>`

---

**Last Updated**: October 3, 2025  
**Version**: 1.0.0  
**Maintainer**: Living Codex DevOps Team