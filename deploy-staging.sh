#!/bin/bash

# Staging Deployment Script for Living Codex
# Focused on 24-hour soak testing and validation
# Embodying the principle of thorough, compassionate testing

set -e

# Colors for mindful output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
PURPLE='\033[0;35m'
NC='\033[0m'

NAMESPACE="living-codex-staging"
IMAGE_TAG="staging"

echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║          Living Codex - Staging Deployment & Soak Test        ║${NC}"
echo -e "${BLUE}║                  Compassionate Testing & Validation           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Build and deploy to staging
log_info "Building and deploying to staging environment..."

# Build the Docker image
docker build -t living-codex-api:${IMAGE_TAG} .

# Apply staging configuration
kubectl apply -f k8s/staging-deployment.yaml

# Wait for deployment to be ready
log_info "Waiting for staging deployment to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/living-codex-api-staging -n ${NAMESPACE}

# Wait for pods to be running
kubectl wait --for=condition=ready --timeout=180s pod -l app=living-codex-api -n ${NAMESPACE}

log_success "Staging deployment is ready!"

# Run initial health checks
log_info "Running initial health checks..."

# Get service endpoint
SERVICE_IP=$(kubectl get service living-codex-service-staging -n ${NAMESPACE} -o jsonpath='{.spec.clusterIP}')
SERVICE_PORT=$(kubectl get service living-codex-service-staging -n ${NAMESPACE} -o jsonpath='{.spec.ports[0].port}')

# Test endpoints
HEALTH_URL="http://${SERVICE_IP}:${SERVICE_PORT}/health"
MEMORY_HEALTH_URL="http://${SERVICE_IP}:${SERVICE_PORT}/health/memory"
METRICS_URL="http://${SERVICE_IP}:${SERVICE_PORT}/metrics/prometheus"

log_info "Testing health endpoint..."
kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$HEALTH_URL" > /dev/null 2>&1
log_success "Health endpoint responding"

log_info "Testing memory health endpoint..."
kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$MEMORY_HEALTH_URL" > /dev/null 2>&1
log_success "Memory health endpoint responding"

log_info "Testing Prometheus metrics endpoint..."
kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$METRICS_URL" > /dev/null 2>&1
log_success "Prometheus metrics endpoint responding"

# Start 24-hour soak test
log_info "Starting 24-hour soak test..."

cat <<EOF | kubectl apply -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: living-codex-soak-test-$(date +%Y%m%d-%H%M%S)
  namespace: ${NAMESPACE}
spec:
  template:
    spec:
      containers:
      - name: soak-test
        image: curlimages/curl
        command:
        - /bin/sh
        - -c
        - |
          echo "🧪 Starting 24-hour soak test for Living Codex..."
          echo "📊 Monitoring endpoints:"
          echo "   • Health: ${HEALTH_URL}"
          echo "   • Memory Health: ${MEMORY_HEALTH_URL}"
          echo "   • Prometheus Metrics: ${METRICS_URL}"
          echo ""
          
          # Test iteration counter
          iteration=0
          total_iterations=1440  # 24 hours * 60 minutes
          
          # Track success/failure counts
          health_success=0
          memory_success=0
          metrics_success=0
          total_tests=0
          
          while [ \$iteration -lt \$total_iterations ]; do
            iteration=\$((iteration + 1))
            echo "⏰ Soak test iteration \$iteration/\$total_iterations (\$(date))"
            
            # Test health endpoint
            if curl -f -s "$HEALTH_URL" > /dev/null; then
              health_success=\$((health_success + 1))
              echo "   ✅ Health endpoint: OK"
            else
              echo "   ❌ Health endpoint: FAILED"
            fi
            
            # Test memory health endpoint
            if curl -f -s "$MEMORY_HEALTH_URL" > /dev/null; then
              memory_success=\$((memory_success + 1))
              echo "   ✅ Memory health endpoint: OK"
            else
              echo "   ❌ Memory health endpoint: FAILED"
            fi
            
            # Test Prometheus metrics endpoint
            if curl -f -s "$METRICS_URL" > /dev/null; then
              metrics_success=\$((metrics_success + 1))
              echo "   ✅ Prometheus metrics endpoint: OK"
            else
              echo "   ❌ Prometheus metrics endpoint: FAILED"
            fi
            
            total_tests=\$((total_tests + 3))
            
            # Calculate success rates
            health_rate=\$((health_success * 100 / iteration))
            memory_rate=\$((memory_success * 100 / iteration))
            metrics_rate=\$((metrics_success * 100 / iteration))
            
            echo "   📈 Success rates: Health=\${health_rate}%, Memory=\${memory_rate}%, Metrics=\${metrics_rate}%"
            
            # Check for memory usage trends (every 10 iterations)
            if [ \$((iteration % 10)) -eq 0 ]; then
              echo "   🔍 Memory health check:"
              curl -s "$MEMORY_HEALTH_URL" | grep -o '"memoryUsageMB":[0-9]*' | head -1
              curl -s "$MEMORY_HEALTH_URL" | grep -o '"memoryPressure":"[^"]*"' | head -1
              curl -s "$MEMORY_HEALTH_URL" | grep -o '"healthScore":[0-9]*' | head -1
            fi
            
            echo ""
            
            # Sleep for 1 minute
            sleep 60
          done
          
          echo "🎉 24-hour soak test completed!"
          echo "📊 Final Statistics:"
          echo "   • Total tests: \$total_tests"
          echo "   • Health endpoint success: \$health_success/\$total_iterations (\$((health_success * 100 / total_iterations))%)"
          echo "   • Memory health success: \$memory_success/\$total_iterations (\$((memory_success * 100 / total_iterations))%)"
          echo "   • Metrics endpoint success: \$metrics_success/\$total_iterations (\$((metrics_success * 100 / total_iterations))%)"
          echo ""
          echo "🙏 Thank you for your patience during this comprehensive test"
      restartPolicy: Never
  backoffLimit: 0
EOF

log_success "24-hour soak test job created!"

# Display monitoring information
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Staging Deployment Status                   ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

echo -e "${BLUE}📊 Pod Status:${NC}"
kubectl get pods -n ${NAMESPACE} -l app=living-codex-api

echo ""
echo -e "${BLUE}🌐 Service Status:${NC}"
kubectl get services -n ${NAMESPACE}

echo ""
echo -e "${BLUE}📈 Deployment Status:${NC}"
kubectl get deployments -n ${NAMESPACE}

echo ""
echo -e "${BLUE}🧪 Soak Test Monitoring:${NC}"
echo "   • Monitor progress: kubectl logs -f job/living-codex-soak-test-* -n ${NAMESPACE}"
echo "   • Check job status: kubectl get jobs -n ${NAMESPACE}"

echo ""
echo -e "${BLUE}🔍 Manual Testing:${NC}"
echo "   • Health: curl http://${SERVICE_IP}:${SERVICE_PORT}/health"
echo "   • Memory Health: curl http://${SERVICE_IP}:${SERVICE_PORT}/health/memory"
echo "   • Prometheus Metrics: curl http://${SERVICE_IP}:${SERVICE_PORT}/metrics/prometheus"

echo ""
echo -e "${GREEN}✨ Staging deployment and soak test initiated with compassion ✨${NC}"
echo -e "${YELLOW}⏰ The 24-hour soak test will complete in approximately 24 hours${NC}"
echo -e "${BLUE}📋 Monitor the progress and validate memory leak fixes work under sustained load${NC}"

log_success "Staging deployment completed successfully!"
echo -e "${GREEN}🙏 May this testing serve to ensure reliable service for all beings 🙏${NC}"

