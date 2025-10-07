# Living Codex Monitoring & Alerting Configuration

## 📊 Monitoring Stack Overview

The Living Codex system includes comprehensive monitoring with the following components:

- **Prometheus**: Metrics collection and storage
- **Grafana**: Visualization and dashboards
- **Jaeger**: Distributed tracing
- **Custom Health Endpoints**: Application-specific monitoring
- **Log Aggregation**: Centralized logging with structured data

## 🔧 Prometheus Configuration

### Prometheus Rules (`monitoring/prometheus-rules.yaml`)

```yaml
groups:
  - name: living-codex.rules
    rules:
      # Memory Alerts
      - alert: HighMemoryUsage
        expr: living_codex_memory_heap_size_bytes / living_codex_memory_working_set_bytes > 0.8
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High memory usage detected"
          description: "Memory usage is above 80% for more than 5 minutes"

      - alert: CriticalMemoryUsage
        expr: living_codex_memory_heap_size_bytes / living_codex_memory_working_set_bytes > 0.9
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Critical memory usage detected"
          description: "Memory usage is above 90% for more than 2 minutes"

      # Performance Alerts
      - alert: HighLatency
        expr: histogram_quantile(0.95, living_codex_http_request_duration_seconds_bucket) > 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency detected"
          description: "95th percentile latency is above 500ms"

      - alert: CriticalLatency
        expr: histogram_quantile(0.95, living_codex_http_request_duration_seconds_bucket) > 1.0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Critical latency detected"
          description: "95th percentile latency is above 1000ms"

      # Error Rate Alerts
      - alert: HighErrorRate
        expr: rate(living_codex_http_requests_total{status=~"5.."}[5m]) / rate(living_codex_http_requests_total[5m]) > 0.01
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          description: "Error rate is above 1% for more than 5 minutes"

      - alert: CriticalErrorRate
        expr: rate(living_codex_http_requests_total{status=~"5.."}[5m]) / rate(living_codex_http_requests_total[5m]) > 0.05
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Critical error rate detected"
          description: "Error rate is above 5% for more than 2 minutes"

      # Health Score Alerts
      - alert: LowHealthScore
        expr: living_codex_health_score < 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Low health score detected"
          description: "System health score is below 80"

      - alert: CriticalHealthScore
        expr: living_codex_health_score < 60
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Critical health score detected"
          description: "System health score is below 60"

      # Session Management Alerts
      - alert: HighSessionCount
        expr: living_codex_active_sessions > 10000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High session count detected"
          description: "Active session count is above 10,000"

      # AI Pipeline Alerts
      - alert: AI PipelineHighLatency
        expr: living_codex_ai_pipeline_processing_time_seconds > 30
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "AI pipeline high latency detected"
          description: "AI pipeline processing time is above 30 seconds"

      - alert: AI PipelineQueueBacklog
        expr: living_codex_ai_pipeline_queue_size > 100
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "AI pipeline queue backlog detected"
          description: "AI pipeline queue size is above 100"
```

## 📈 Grafana Dashboard Configuration

### System Overview Dashboard

```json
{
  "dashboard": {
    "title": "Living Codex System Overview",
    "panels": [
      {
        "title": "System Health Score",
        "type": "stat",
        "targets": [
          {
            "expr": "living_codex_health_score",
            "legendFormat": "Health Score"
          }
        ],
        "thresholds": [
          {"color": "red", "value": 60},
          {"color": "yellow", "value": 80},
          {"color": "green", "value": 90}
        ]
      },
      {
        "title": "Memory Usage",
        "type": "graph",
        "targets": [
          {
            "expr": "living_codex_memory_heap_size_bytes / 1024 / 1024",
            "legendFormat": "Heap Size (MB)"
          },
          {
            "expr": "living_codex_memory_working_set_bytes / 1024 / 1024",
            "legendFormat": "Working Set (MB)"
          }
        ]
      },
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(living_codex_http_requests_total[5m])",
            "legendFormat": "Requests/sec"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, living_codex_http_request_duration_seconds_bucket)",
            "legendFormat": "95th percentile"
          },
          {
            "expr": "histogram_quantile(0.99, living_codex_http_request_duration_seconds_bucket)",
            "legendFormat": "99th percentile"
          }
        ]
      },
      {
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(living_codex_http_requests_total{status=~\"5..\"}[5m]) / rate(living_codex_http_requests_total[5m]) * 100",
            "legendFormat": "Error Rate %"
          }
        ]
      },
      {
        "title": "Active Sessions",
        "type": "graph",
        "targets": [
          {
            "expr": "living_codex_active_sessions",
            "legendFormat": "Active Sessions"
          }
        ]
      },
      {
        "title": "AI Pipeline Metrics",
        "type": "graph",
        "targets": [
          {
            "expr": "living_codex_ai_pipeline_active_requests",
            "legendFormat": "Active Requests"
          },
          {
            "expr": "living_codex_ai_pipeline_queue_size",
            "legendFormat": "Queue Size"
          }
        ]
      }
    ]
  }
}
```

## 🔔 Alert Manager Configuration

### Alert Manager Rules (`monitoring/alertmanager.yml`)

```yaml
global:
  smtp_smarthost: 'localhost:587'
  smtp_from: 'alerts@livingcodex.com'

route:
  group_by: ['alertname']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'default'
  routes:
    - match:
        severity: critical
      receiver: 'critical-alerts'
    - match:
        severity: warning
      receiver: 'warning-alerts'

receivers:
  - name: 'default'
    email_configs:
      - to: 'alerts@livingcodex.com'
        subject: 'Living Codex Alert: {{ .GroupLabels.alertname }}'
        body: |
          {{ range .Alerts }}
          Alert: {{ .Annotations.summary }}
          Description: {{ .Annotations.description }}
          {{ end }}

  - name: 'critical-alerts'
    email_configs:
      - to: 'critical@livingcodex.com'
        subject: '🚨 CRITICAL: Living Codex Alert: {{ .GroupLabels.alertname }}'
        body: |
          {{ range .Alerts }}
          Alert: {{ .Annotations.summary }}
          Description: {{ .Annotations.description }}
          {{ end }}
    slack_configs:
      - api_url: 'YOUR_SLACK_WEBHOOK_URL'
        channel: '#living-codex-critical'
        title: '🚨 Critical Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'

  - name: 'warning-alerts'
    email_configs:
      - to: 'warnings@livingcodex.com'
        subject: '⚠️ WARNING: Living Codex Alert: {{ .GroupLabels.alertname }}'
        body: |
          {{ range .Alerts }}
          Alert: {{ .Annotations.summary }}
          Description: {{ .Annotations.description }}
          {{ end }}
    slack_configs:
      - api_url: 'YOUR_SLACK_WEBHOOK_URL'
        channel: '#living-codex-alerts'
        title: '⚠️ Warning Alert'
        text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'
```

## 📊 Custom Health Endpoints

### Health Endpoint Configuration

The system exposes several health endpoints for monitoring:

```bash
# Overall system health
curl http://localhost:5002/health

# Memory-specific health
curl http://localhost:5002/health/memory

# AI pipeline health
curl http://localhost:5002/health/ai-pipeline

# Prometheus metrics
curl http://localhost:5002/metrics/prometheus
```

### Health Check Script

```bash
#!/bin/bash
# health-check.sh

ENDPOINT="http://localhost:5002"
TIMEOUT=10

check_endpoint() {
    local endpoint=$1
    local name=$2
    
    echo "Checking $name..."
    if curl -s --max-time $TIMEOUT "$ENDPOINT$endpoint" > /dev/null; then
        echo "✅ $name: OK"
        return 0
    else
        echo "❌ $name: FAILED"
        return 1
    fi
}

echo "=== Living Codex Health Check ==="
echo "Timestamp: $(date)"
echo

# Check all endpoints
check_endpoint "/health" "Overall Health"
check_endpoint "/health/memory" "Memory Health"
check_endpoint "/health/ai-pipeline" "AI Pipeline Health"
check_endpoint "/metrics/prometheus" "Prometheus Metrics"

echo
echo "=== Health Check Complete ==="
```

## 🔍 Log Monitoring Configuration

### Log Aggregation Setup

```yaml
# Filebeat configuration for log shipping
filebeat.inputs:
  - type: log
    enabled: true
    paths:
      - /path/to/living-codex/logs/*.log
    fields:
      service: living-codex
      environment: production
    multiline.pattern: '^\d{4}-\d{2}-\d{2}'
    multiline.negate: true
    multiline.match: after

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
  index: "living-codex-logs-%{+yyyy.MM.dd}"

processors:
  - add_host_metadata:
      when.not.contains.tags: forwarded
```

### Log Parsing Rules

```yaml
# Logstash configuration for log parsing
filter {
  if [service] == "living-codex" {
    grok {
      match => { "message" => "%{TIMESTAMP_ISO8601:timestamp} \[%{WORD:level}\] %{GREEDYDATA:log_message}" }
    }
    
    date {
      match => [ "timestamp", "yyyy-MM-dd HH:mm:ss.SSS" ]
    }
    
    if [log_message] =~ /memory/i {
      mutate { add_tag => [ "memory" ] }
    }
    
    if [log_message] =~ /error|exception/i {
      mutate { add_tag => [ "error" ] }
    }
    
    if [log_message] =~ /module/i {
      mutate { add_tag => [ "module" ] }
    }
  }
}
```

## 📱 Monitoring Scripts

### Memory Monitoring Script

```bash
#!/bin/bash
# memory-monitor.sh

ENDPOINT="http://localhost:5002"
ALERT_THRESHOLD=80

while true; do
    MEMORY_USAGE=$(curl -s "$ENDPOINT/health/memory" | jq '.memoryUsage.heapSizeMB')
    MEMORY_LIMIT=$(curl -s "$ENDPOINT/health/memory" | jq '.memoryUsage.workingSetMB')
    
    if [ "$MEMORY_USAGE" != "null" ] && [ "$MEMORY_LIMIT" != "null" ]; then
        USAGE_PERCENT=$(echo "scale=2; $MEMORY_USAGE * 100 / $MEMORY_LIMIT" | bc)
        
        echo "$(date): Memory Usage: ${USAGE_PERCENT}% (${MEMORY_USAGE}MB / ${MEMORY_LIMIT}MB)"
        
        if [ $(echo "$USAGE_PERCENT > $ALERT_THRESHOLD" | bc -l) -eq 1 ]; then
            echo "⚠️ ALERT: Memory usage above ${ALERT_THRESHOLD}%"
        fi
    else
        echo "$(date): Failed to get memory metrics"
    fi
    
    sleep 60
done
```

### Performance Monitoring Script

```bash
#!/bin/bash
# performance-monitor.sh

ENDPOINT="http://localhost:5002"

while true; do
    echo "=== Performance Check: $(date) ==="
    
    # Check response time
    RESPONSE_TIME=$(curl -o /dev/null -s -w '%{time_total}' "$ENDPOINT/health")
    echo "Response Time: ${RESPONSE_TIME}s"
    
    # Check health score
    HEALTH_SCORE=$(curl -s "$ENDPOINT/health" | jq '.healthScore // 0')
    echo "Health Score: $HEALTH_SCORE"
    
    # Check AI pipeline metrics
    AI_METRICS=$(curl -s "$ENDPOINT/health/ai-pipeline" | jq '.totalRequests // 0')
    echo "AI Pipeline Requests: $AI_METRICS"
    
    echo "================================"
    sleep 30
done
```

## 🚨 Alert Testing

### Test Alert Configuration

```bash
#!/bin/bash
# test-alerts.sh

echo "Testing alert configurations..."

# Test memory alert
echo "Testing memory alert..."
curl -X POST http://localhost:5002/ai/test/simulate \
  -H "Content-Type: application/json" \
  -d '{"requestType": "memory-test", "userId": "alert-test", "model": "test", "processingTime": 1000}'

# Test performance alert
echo "Testing performance alert..."
curl -X POST http://localhost:5002/ai/test/slow \
  -H "Content-Type: application/json" \
  -d '{"requestType": "performance-test", "userId": "alert-test", "processingTime": 30000}'

# Test error alert
echo "Testing error alert..."
curl -X POST http://localhost:5002/ai/test/fail \
  -H "Content-Type: application/json" \
  -d '{"requestType": "error-test", "userId": "alert-test"}'

echo "Alert tests completed. Check monitoring dashboard for results."
```

## 📋 Monitoring Checklist

### Pre-Production Setup
- [ ] Prometheus configured and running
- [ ] Grafana dashboards imported
- [ ] Alert Manager rules configured
- [ ] Email/Slack notifications tested
- [ ] Health endpoints responding
- [ ] Metrics collection working
- [ ] Log aggregation configured
- [ ] Monitoring scripts deployed

### Production Monitoring
- [ ] Health checks running every 5 minutes
- [ ] Memory monitoring active
- [ ] Performance metrics collected
- [ ] Error rate tracking enabled
- [ ] AI pipeline monitoring active
- [ ] Alert thresholds configured
- [ ] Incident response procedures tested

### Maintenance
- [ ] Dashboard updates monthly
- [ ] Alert rules reviewed quarterly
- [ ] Monitoring scripts updated as needed
- [ ] Performance baselines established
- [ ] Capacity planning data collected

---

**Monitoring Configuration Version**: 1.0.0  
**Last Updated**: October 3, 2025  
**Review Schedule**: Monthly  
**Next Review**: November 3, 2025

