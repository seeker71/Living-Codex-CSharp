#!/bin/bash

# Living Codex Comprehensive Monitoring Setup Script
# This script sets up the complete monitoring and observability stack

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_status "Setting up comprehensive monitoring and observability for Living Codex..."

# Check if Docker is running
if ! docker info &> /dev/null; then
    print_error "Docker is not running. Please start Docker first."
    exit 1
fi

# Create monitoring directories
print_status "Creating monitoring directories..."
mkdir -p monitoring/grafana/dashboards
mkdir -p monitoring/grafana/datasources
mkdir -p monitoring/prometheus/rules
mkdir -p monitoring/alertmanager
mkdir -p monitoring/jaeger
mkdir -p monitoring/elasticsearch
mkdir -p monitoring/kibana

# Create Prometheus rules
print_status "Setting up Prometheus alerting rules..."
cp monitoring/prometheus-rules.yaml monitoring/prometheus/rules/

# Create Grafana dashboards
print_status "Setting up Grafana dashboards..."
cp monitoring/grafana/dashboards/living-codex-overview.json monitoring/grafana/dashboards/

# Create Grafana datasource configuration
print_status "Creating Grafana datasource configuration..."
cat > monitoring/grafana/datasources/prometheus.yaml << EOF
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
EOF

# Create comprehensive monitoring Docker Compose
print_status "Creating comprehensive monitoring Docker Compose..."
cat > monitoring/docker-compose.monitoring.yml << EOF
version: '3.8'

services:
  # Prometheus
  prometheus:
    image: prom/prometheus:latest
    container_name: living-codex-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - ./rules:/etc/prometheus/rules
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=30d'
      - '--web.enable-lifecycle'
      - '--web.enable-admin-api'
    networks:
      - monitoring

  # Grafana
  grafana:
    image: grafana/grafana:latest
    container_name: living-codex-grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_INSTALL_PLUGINS=grafana-piechart-panel
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources:/etc/grafana/provisioning/datasources
    networks:
      - monitoring

  # Jaeger
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: living-codex-jaeger
    ports:
      - "16686:16686"
      - "14268:14268"
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    networks:
      - monitoring

  # Elasticsearch
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.8.0
    container_name: living-codex-elasticsearch
    ports:
      - "9200:9200"
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms1g -Xmx1g"
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    networks:
      - monitoring

  # Kibana
  kibana:
    image: docker.elastic.co/kibana/kibana:8.8.0
    container_name: living-codex-kibana
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch
    networks:
      - monitoring

  # AlertManager
  alertmanager:
    image: prom/alertmanager:latest
    container_name: living-codex-alertmanager
    ports:
      - "9093:9093"
    volumes:
      - ./alertmanager/alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'
      - '--storage.path=/alertmanager'
    networks:
      - monitoring

  # Node Exporter
  node-exporter:
    image: prom/node-exporter:latest
    container_name: living-codex-node-exporter
    ports:
      - "9100:9100"
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    networks:
      - monitoring

volumes:
  prometheus_data:
  grafana_data:
  elasticsearch_data:
  alertmanager_data:

networks:
  monitoring:
    driver: bridge
EOF

# Create AlertManager configuration
print_status "Creating AlertManager configuration..."
mkdir -p monitoring/alertmanager
cat > monitoring/alertmanager/alertmanager.yml << EOF
global:
  smtp_smarthost: 'localhost:587'
  smtp_from: 'alerts@livingcodex.com'

route:
  group_by: ['alertname']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'web.hook'

receivers:
- name: 'web.hook'
  webhook_configs:
  - url: 'http://localhost:5001/api/alerts'
    send_resolved: true

inhibit_rules:
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname', 'dev', 'instance']
EOF

# Start monitoring services
print_status "Starting monitoring services..."
cd monitoring
docker-compose -f docker-compose.monitoring.yml up -d

# Wait for services to be ready
print_status "Waiting for monitoring services to be ready..."
sleep 30

# Check service health
print_status "Checking monitoring service health..."

# Check Prometheus
if curl -f http://localhost:9090/-/healthy > /dev/null 2>&1; then
    print_success "Prometheus is healthy"
else
    print_warning "Prometheus health check failed"
fi

# Check Grafana
if curl -f http://localhost:3000/api/health > /dev/null 2>&1; then
    print_success "Grafana is healthy"
else
    print_warning "Grafana health check failed"
fi

# Check Jaeger
if curl -f http://localhost:16686 > /dev/null 2>&1; then
    print_success "Jaeger is healthy"
else
    print_warning "Jaeger health check failed"
fi

# Check Elasticsearch
if curl -f http://localhost:9200/_cluster/health > /dev/null 2>&1; then
    print_success "Elasticsearch is healthy"
else
    print_warning "Elasticsearch health check failed"
fi

# Check Kibana
if curl -f http://localhost:5601/api/status > /dev/null 2>&1; then
    print_success "Kibana is healthy"
else
    print_warning "Kibana health check failed"
fi

cd ..

print_success "Comprehensive monitoring and observability setup completed!"
echo ""
echo "📊 Monitoring URLs:"
echo "  Prometheus:      http://localhost:9090"
echo "  Grafana:         http://localhost:3000 (admin/admin)"
echo "  Jaeger:          http://localhost:16686"
echo "  Elasticsearch:   http://localhost:9200"
echo "  Kibana:          http://localhost:5601"
echo "  AlertManager:    http://localhost:9093"
echo "  Node Exporter:   http://localhost:9100"
echo ""
echo "🔧 Management Commands:"
echo "  View logs:       docker-compose -f monitoring/docker-compose.monitoring.yml logs -f"
echo "  Stop services:   docker-compose -f monitoring/docker-compose.monitoring.yml down"
echo "  Restart:         docker-compose -f monitoring/docker-compose.monitoring.yml restart"
echo ""
echo "📈 Key Metrics to Monitor:"
echo "  - API request rate and response times"
echo "  - Error rates and success rates"
echo "  - Database performance and connections"
echo "  - Cache hit rates and memory usage"
echo "  - Translation success rates"
echo "  - Resonance calculation performance"
echo "  - User activity and engagement"
echo "  - System resource utilization"
echo ""
print_success "Monitoring setup complete! 🎉"
