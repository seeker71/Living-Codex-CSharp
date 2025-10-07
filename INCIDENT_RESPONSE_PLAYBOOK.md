# Living Codex Incident Response Playbook

## 🚨 Incident Classification Matrix

| Severity | Impact | Response Time | Examples |
|----------|--------|---------------|----------|
| **P1 - Critical** | System down, data loss, security breach | 0-15 min | Server crash, database corruption, security incident |
| **P2 - High** | Major functionality affected | 15-30 min | API failures, performance degradation >50% |
| **P3 - Medium** | Minor functionality affected | 30-60 min | Single module failure, minor performance issues |
| **P4 - Low** | Cosmetic issues, minor impact | 1-24 hours | UI glitches, non-critical warnings |

## 🔥 P1 - Critical Incident Response

### Immediate Response (0-15 minutes)

#### 1. Assess System Status
```bash
# Quick system check
curl -s http://localhost:5002/health || echo "SERVER DOWN"
ps aux | grep dotnet || echo "NO PROCESSES"

# Check logs for errors
tail -20 logs/server.log | grep -i error
```

#### 2. Identify Root Cause
```bash
# Check memory issues
curl -s http://localhost:5002/health/memory 2>/dev/null || echo "MEMORY CHECK FAILED"

# Check database connectivity
curl -s http://localhost:5002/health 2>/dev/null | jq '.database' || echo "DB CHECK FAILED"

# Check recent errors
grep -i "exception\|error\|critical" logs/server.log | tail -10
```

#### 3. Implement Immediate Fix
```bash
# If memory issue
if [ $(curl -s http://localhost:5002/health/memory 2>/dev/null | jq '.memoryUsage.heapSizeMB' 2>/dev/null || echo "0") -gt 2000 ]; then
    echo "CRITICAL: High memory usage detected"
    ./stop-servers.sh && sleep 5 && ./start-server.sh
fi

# If process issue
if ! pgrep -f dotnet > /dev/null; then
    echo "CRITICAL: No dotnet processes running"
    ./start-server.sh
fi

# If port conflict
if lsof -i :5002 > /dev/null 2>&1; then
    echo "CRITICAL: Port conflict detected"
    ./stop-servers.sh && sleep 2 && ./start-server.sh
fi
```

#### 4. Validate Fix
```bash
# Wait for startup
sleep 30

# Verify health
curl -s http://localhost:5002/health | jq '.status' || echo "HEALTH CHECK FAILED"

# Check memory
curl -s http://localhost:5002/health/memory | jq '.memoryUsage.heapSizeMB' || echo "MEMORY CHECK FAILED"
```

### Escalation (15-30 minutes)
- Notify on-call engineer
- Create incident ticket
- Document findings and actions taken
- Update stakeholders

### Resolution (30-60 minutes)
- Implement permanent fix
- Monitor system stability
- Update monitoring and alerting
- Document lessons learned

## ⚠️ P2 - High Priority Incident Response

### Response (0-30 minutes)

#### 1. Assess Impact
```bash
# Check API functionality
curl -s http://localhost:5002/health | jq '.moduleLoading.asyncInitializationComplete'

# Check performance metrics
curl -s http://localhost:5002/metrics/prometheus | grep -E "(latency|error_rate)"

# Check specific modules
curl -s http://localhost:5002/health | jq '.modules[] | select(.status != "loaded")'
```

#### 2. Implement Fix
```bash
# If module loading issues
if [ "$(curl -s http://localhost:5002/health | jq '.moduleLoading.asyncInitializationComplete')" != "true" ]; then
    echo "WARNING: Module loading incomplete"
    # Check specific module failures
    curl -s http://localhost:5002/health | jq '.modules[] | select(.status != "loaded")'
fi

# If performance issues
LATENCY=$(curl -s http://localhost:5002/metrics/prometheus | grep "http_request_duration_seconds" | awk '{print $2}')
if [ $(echo "$LATENCY > 1.0" | bc -l) -eq 1 ]; then
    echo "WARNING: High latency detected"
    # Check for resource constraints
    curl -s http://localhost:5002/health/memory | jq '.memoryUsage'
fi
```

#### 3. Monitor Resolution
```bash
# Watch for improvement
watch -n 10 'curl -s http://localhost:5002/health | jq "{status: .status, modules: .moduleLoading.asyncInitializationComplete, memory: .memoryUsage.heapSizeMB}"'
```

## 🔧 P3 - Medium Priority Incident Response

### Response (30-60 minutes)

#### 1. Investigate Issue
```bash
# Check specific module status
curl -s http://localhost:5002/health | jq '.modules[] | select(.status != "loaded")'

# Check performance degradation
curl -s http://localhost:5002/health/memory | jq '.memoryUsage'

# Check AI pipeline performance
curl -s http://localhost:5002/health/ai-pipeline | jq '.metrics'
```

#### 2. Apply Fix
```bash
# If single module failure
FAILED_MODULES=$(curl -s http://localhost:5002/health | jq -r '.modules[] | select(.status != "loaded") | .name')
if [ ! -z "$FAILED_MODULES" ]; then
    echo "WARNING: Failed modules: $FAILED_MODULES"
    # Restart specific module or entire system
    ./restart-server.sh
fi
```

#### 3. Validate Fix
```bash
# Check module status
curl -s http://localhost:5002/health | jq '.modules[] | select(.status != "loaded")'

# Monitor for 10 minutes
for i in {1..10}; do
    echo "Check $i/10: $(curl -s http://localhost:5002/health | jq '.status')"
    sleep 60
done
```

## 📋 Common Incident Scenarios

### Scenario 1: Server Crash
**Symptoms**: Server process terminated, health endpoint unreachable
**Response**:
```bash
# 1. Check if process is running
ps aux | grep dotnet

# 2. Check logs for crash reason
tail -50 logs/server.log | grep -i "exception\|error\|crash"

# 3. Restart server
./start-server.sh

# 4. Monitor startup
watch -n 5 'curl -s http://localhost:5002/health | jq ".status"'
```

### Scenario 2: Memory Leak
**Symptoms**: Continuously increasing memory usage, eventual OutOfMemoryException
**Response**:
```bash
# 1. Check current memory usage
curl -s http://localhost:5002/health/memory | jq '.memoryUsage'

# 2. Check for memory leaks
curl -s http://localhost:5002/health/memory | jq '.memoryLeaks'

# 3. If critical, restart
if [ $(curl -s http://localhost:5002/health/memory | jq '.memoryUsage.heapSizeMB') -gt 2000 ]; then
    ./stop-servers.sh && sleep 5 && ./start-server.sh
fi

# 4. Monitor memory trends
watch -n 30 'curl -s http://localhost:5002/health/memory | jq ".memoryUsage.heapSizeMB"'
```

### Scenario 3: Database Connection Issues
**Symptoms**: Database errors, connection timeouts, module failures
**Response**:
```bash
# 1. Check database health
curl -s http://localhost:5002/health | jq '.database'

# 2. Check connection string
echo $CONNECTION_STRING

# 3. Test database connectivity
psql "$CONNECTION_STRING" -c "SELECT 1;" || echo "DB CONNECTION FAILED"

# 4. Restart if needed
./restart-server.sh
```

### Scenario 4: High Latency
**Symptoms**: Slow response times, timeout errors
**Response**:
```bash
# 1. Check performance metrics
curl -s http://localhost:5002/metrics/prometheus | grep -E "(latency|duration)"

# 2. Check memory usage
curl -s http://localhost:5002/health/memory | jq '.memoryUsage'

# 3. Check AI pipeline performance
curl -s http://localhost:5002/health/ai-pipeline | jq '.metrics'

# 4. If memory pressure, restart
if [ $(curl -s http://localhost:5002/health/memory | jq '.memoryUsage.heapSizeMB') -gt 1500 ]; then
    ./restart-server.sh
fi
```

### Scenario 5: Module Loading Failures
**Symptoms**: Modules not initializing, partial functionality
**Response**:
```bash
# 1. Check module status
curl -s http://localhost:5002/health | jq '.modules[] | select(.status != "loaded")'

# 2. Check logs for module errors
tail -100 logs/server.log | grep -i "module\|initialization"

# 3. Check dependencies
curl -s http://localhost:5002/health | jq '.moduleLoading'

# 4. Restart if needed
./restart-server.sh
```

## 📊 Post-Incident Procedures

### Immediate Post-Incident (0-24 hours)
1. **Document Incident**
   - Incident ticket with timeline
   - Root cause analysis
   - Actions taken
   - Resolution steps

2. **Monitor System**
   - Watch for recurrence
   - Monitor performance metrics
   - Check error logs

3. **Communicate Status**
   - Update stakeholders
   - Close incident ticket
   - Send resolution notification

### Post-Incident Review (24-48 hours)
1. **Conduct Post-Mortem**
   - Timeline reconstruction
   - Root cause analysis
   - Impact assessment
   - Lessons learned

2. **Update Documentation**
   - Update runbook
   - Add new troubleshooting steps
   - Update monitoring thresholds

3. **Implement Improvements**
   - Fix root cause
   - Add monitoring
   - Update procedures
   - Train team

## 🚨 Emergency Contacts

### On-Call Rotation
- **Primary**: [Contact Information]
- **Secondary**: [Contact Information]
- **Escalation**: [Contact Information]

### Communication Channels
- **Slack**: #living-codex-incidents
- **PagerDuty**: [Integration]
- **Email**: incidents@livingcodex.com

### Escalation Matrix
| Severity | Primary Response | Escalation Time | Escalation Contact |
|----------|------------------|-----------------|-------------------|
| P1 | On-Call Engineer | 15 minutes | DevOps Lead |
| P2 | On-Call Engineer | 30 minutes | Engineering Manager |
| P3 | On-Call Engineer | 60 minutes | Team Lead |
| P4 | Regular Support | 24 hours | Product Manager |

## 📋 Incident Checklist

### During Incident
- [ ] Assess severity and impact
- [ ] Implement immediate fix/workaround
- [ ] Notify stakeholders
- [ ] Document actions taken
- [ ] Monitor resolution
- [ ] Validate fix effectiveness

### Post-Incident
- [ ] Document incident details
- [ ] Conduct root cause analysis
- [ ] Update runbook/procedures
- [ ] Implement preventive measures
- [ ] Schedule post-mortem meeting
- [ ] Follow up on action items

---

**Incident Response Playbook Version**: 1.0.0  
**Last Updated**: October 3, 2025  
**Review Schedule**: Monthly  
**Next Review**: November 3, 2025

