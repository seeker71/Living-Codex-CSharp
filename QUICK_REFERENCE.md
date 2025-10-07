# Living Codex Quick Reference Guide

## 🚨 Emergency Commands

### Server Control
```bash
# Start server
./start-server.sh

# Stop server
./stop-servers.sh

# Restart server
./stop-servers.sh && sleep 2 && ./start-server.sh

# Force kill (if normal stop fails)
pkill -f dotnet
```

### Health Checks
```bash
# Quick health check
curl -s http://localhost:5002/health | jq '.status'

# Memory check
curl -s http://localhost:5002/health/memory | jq '.memoryUsage.heapSizeMB'

# AI pipeline check
curl -s http://localhost:5002/health/ai-pipeline | jq '.healthScore'
```

## 📊 Monitoring Commands

### System Status
```bash
# Overall health
curl -s http://localhost:5002/health | jq '{
  status: .status,
  modules: .moduleLoading.asyncInitializationComplete,
  memory: .memoryUsage.heapSizeMB
}'

# Memory usage
curl -s http://localhost:5002/health/memory | jq '{
  heapSize: .memoryUsage.heapSizeMB,
  workingSet: .memoryUsage.workingSetMB,
  gcPressure: .memoryUsage.gcPressure,
  sessions: .sessionMetrics.activeSessions
}'

# Performance metrics
curl -s http://localhost:5002/metrics/prometheus | grep -E "(memory|sessions|requests)"
```

### Log Monitoring
```bash
# Watch logs in real-time
tail -f logs/server.log

# Check for errors
tail -f logs/server.log | grep -i error

# Check memory issues
tail -f logs/server.log | grep -i memory

# Check module loading
tail -f logs/server.log | grep -i module
```

## 🔧 Troubleshooting

### Common Issues & Fixes

#### Server Won't Start
```bash
# Check port conflicts
lsof -i :5002

# Kill conflicting processes
./stop-servers.sh
pkill -f dotnet

# Restart
./start-server.sh
```

#### High Memory Usage
```bash
# Check memory health
curl -s http://localhost:5002/health/memory | jq '.memoryUsage'

# If critical, restart
./stop-servers.sh && sleep 5 && ./start-server.sh
```

#### Database Issues
```bash
# Check database health
curl -s http://localhost:5002/health | jq '.database'

# Check connection
echo $CONNECTION_STRING
```

#### Module Loading Issues
```bash
# Check module status
curl -s http://localhost:5002/health | jq '.moduleLoading'

# Check logs
tail -f logs/server.log | grep -i "module"
```

## 🚀 Deployment Commands

### Staging Deployment
```bash
# Deploy to staging
./deploy-staging.sh

# Validate staging
./validate-deployment.sh staging
```

### Production Deployment
```bash
# Deploy to production
./deploy-production.sh

# Validate production
./validate-deployment.sh production
```

### Rollback
```bash
# Rollback to previous version
./rollback.sh

# Manual rollback
git checkout previous-stable-version
dotnet build && ./restart-server.sh
```

## 📈 Performance Testing

### Load Testing
```bash
# Test AI endpoint
curl -X POST http://localhost:5002/ai/test/simulate \
  -H "Content-Type: application/json" \
  -d '{"requestType": "text-generation", "userId": "test-user", "model": "gpt-3.5-turbo", "processingTime": 2000}'

# Test slow endpoint
curl -X POST http://localhost:5002/ai/test/slow \
  -H "Content-Type: application/json" \
  -d '{"requestType": "slow-request", "userId": "test-user", "processingTime": 10000}'

# Get AI metrics
curl -s http://localhost:5002/ai/test/metrics | jq '.'
```

### Performance Monitoring
```bash
# Watch performance metrics
watch -n 5 'curl -s http://localhost:5002/health/memory | jq ".memoryUsage.heapSizeMB"'

# Monitor AI pipeline
watch -n 5 'curl -s http://localhost:5002/health/ai-pipeline | jq ".totalRequests"'
```

## 🔍 Debugging

### Process Information
```bash
# Find running processes
ps aux | grep dotnet

# Check process memory
ps aux | grep dotnet | awk '{print $2, $4, $6}'

# Check open files
lsof -p $(pgrep dotnet)
```

### Network Information
```bash
# Check listening ports
netstat -tlnp | grep :5002

# Check connections
ss -tlnp | grep :5002
```

### System Resources
```bash
# Memory usage
free -h

# Disk usage
df -h

# CPU usage
top -p $(pgrep dotnet)
```

## 📋 Health Check Scripts

### Complete Health Check
```bash
#!/bin/bash
echo "=== Living Codex Health Check ==="
echo "Timestamp: $(date)"
echo

echo "1. Server Status:"
curl -s http://localhost:5002/health | jq '.status' || echo "FAILED"

echo "2. Memory Health:"
curl -s http://localhost:5002/health/memory | jq '.memoryUsage.heapSizeMB' || echo "FAILED"

echo "3. AI Pipeline:"
curl -s http://localhost:5002/health/ai-pipeline | jq '.healthScore' || echo "FAILED"

echo "4. Module Loading:"
curl -s http://localhost:5002/health | jq '.moduleLoading.asyncInitializationComplete' || echo "FAILED"

echo "5. Process Check:"
ps aux | grep dotnet | grep -v grep || echo "No dotnet processes found"

echo "=== Health Check Complete ==="
```

### Memory Leak Check
```bash
#!/bin/bash
echo "=== Memory Leak Check ==="
echo "Timestamp: $(date)"
echo

echo "Memory Usage:"
curl -s http://localhost:5002/health/memory | jq '{
  heapSize: .memoryUsage.heapSizeMB,
  workingSet: .memoryUsage.workingSetMB,
  gcPressure: .memoryUsage.gcPressure,
  sessions: .sessionMetrics.activeSessions,
  tokens: .tokenMetrics.activeTokens
}'

echo "Memory Leaks:"
curl -s http://localhost:5002/health/memory | jq '.memoryLeaks'

echo "=== Memory Check Complete ==="
```

## 🚨 Alert Thresholds

### Critical Alerts
- Memory usage > 90%
- Health score < 60
- Error rate > 5%
- Response time > 1000ms

### Warning Alerts
- Memory usage > 80%
- Health score < 80
- Error rate > 1%
- Response time > 500ms

## 📞 Emergency Contacts

### Escalation Path
1. **On-Call Engineer**: [Contact]
2. **DevOps Team Lead**: [Contact]
3. **Engineering Manager**: [Contact]
4. **CTO**: [Contact]

### Communication Channels
- **Slack**: #living-codex-alerts
- **Email**: alerts@livingcodex.com
- **PagerDuty**: [Integration]

---

**Quick Reference Version**: 1.0.0  
**Last Updated**: October 3, 2025  
**For detailed procedures, see**: `PRODUCTION_RUNBOOK.md`

