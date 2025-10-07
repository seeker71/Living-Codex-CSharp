#!/bin/bash

# Deployment Validation Script for Living Codex
# Comprehensive testing of all monitoring endpoints and system health
# Embodying thorough validation with compassionate attention to detail

set -e

# Colors for mindful validation
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
PURPLE='\033[0;35m'
NC='\033[0m'

NAMESPACE="${1:-living-codex}"
ENVIRONMENT="${2:-production}"

echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║          Living Codex - Deployment Validation Suite           ║${NC}"
echo -e "${BLUE}║                  Compassionate System Validation              ║${NC}"
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

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test counter
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to run a test
run_test() {
    local test_name="$1"
    local test_command="$2"
    local expected_result="${3:-0}"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    log_info "Testing: $test_name"
    
    if eval "$test_command" > /dev/null 2>&1; then
        if [ $? -eq $expected_result ]; then
            log_success "✅ $test_name: PASSED"
            PASSED_TESTS=$((PASSED_TESTS + 1))
        else
            log_error "❌ $test_name: FAILED (unexpected exit code)"
            FAILED_TESTS=$((FAILED_TESTS + 1))
        fi
    else
        log_error "❌ $test_name: FAILED"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

# Get service information
log_info "Getting service information for namespace: $NAMESPACE"

SERVICE_IP=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.clusterIP}' 2>/dev/null || echo "")
SERVICE_PORT=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.ports[0].port}' 2>/dev/null || echo "5000")

if [ -z "$SERVICE_IP" ]; then
    log_error "Could not find living-codex-service in namespace $NAMESPACE"
    log_info "Available services:"
    kubectl get services -n ${NAMESPACE}
    exit 1
fi

BASE_URL="http://${SERVICE_IP}:${SERVICE_PORT}"

echo -e "${BLUE}🌟 Validation Configuration:${NC}"
echo -e "   • Namespace: ${GREEN}${NAMESPACE}${NC}"
echo -e "   • Environment: ${GREEN}${ENVIRONMENT}${NC}"
echo -e "   • Service IP: ${GREEN}${SERVICE_IP}${NC}"
echo -e "   • Service Port: ${GREEN}${SERVICE_PORT}${NC}"
echo -e "   • Base URL: ${GREEN}${BASE_URL}${NC}"
echo ""

# 1. Kubernetes Infrastructure Tests
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Kubernetes Infrastructure Tests            ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

run_test "Namespace exists" "kubectl get namespace ${NAMESPACE}"
run_test "Deployment exists" "kubectl get deployment living-codex-api -n ${NAMESPACE}"
run_test "Service exists" "kubectl get service living-codex-service -n ${NAMESPACE}"
run_test "Pods are running" "kubectl get pods -n ${NAMESPACE} -l app=living-codex-api --field-selector=status.phase=Running"
run_test "Deployment is available" "kubectl get deployment living-codex-api -n ${NAMESPACE} -o jsonpath='{.status.conditions[?(@.type==\"Available\")].status}' | grep -q True"

# 2. Health Endpoint Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                      Health Endpoint Tests                    ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

run_test "Health endpoint responds" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f ${BASE_URL}/health"
run_test "Health endpoint returns JSON" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health | jq . > /dev/null"
run_test "Health endpoint has required fields" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health | jq -e '.status and .uptime and .requestCount'"

# 3. Memory Health Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Memory Health Endpoint Tests               ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

run_test "Memory health endpoint responds" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f ${BASE_URL}/health/memory"
run_test "Memory health returns JSON" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health/memory | jq . > /dev/null"
run_test "Memory health has required fields" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health/memory | jq -e '.memoryUsageMB and .memoryPressure and .healthScore and .moduleMetrics'"

# 4. Prometheus Metrics Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                   Prometheus Metrics Tests                    ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

run_test "Prometheus metrics endpoint responds" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f ${BASE_URL}/metrics/prometheus"
run_test "Prometheus metrics returns JSON" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/metrics/prometheus | jq . > /dev/null"
run_test "Prometheus metrics has data field" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/metrics/prometheus | jq -e '.data'"

# 5. System Metrics Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    System Metrics Tests                       ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

run_test "System metrics endpoint responds" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f ${BASE_URL}/metrics"
run_test "System metrics returns JSON" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/metrics | jq . > /dev/null"
run_test "System metrics has required fields" "kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/metrics | jq -e '.success and .system and .codex'"

# 6. Performance Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                      Performance Tests                        ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

# Test response times (should be < 2 seconds)
log_info "Testing response times..."

HEALTH_RESPONSE_TIME=$(kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -w "%{time_total}" -o /dev/null -s ${BASE_URL}/health 2>/dev/null || echo "999")
MEMORY_RESPONSE_TIME=$(kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -w "%{time_total}" -o /dev/null -s ${BASE_URL}/health/memory 2>/dev/null || echo "999")

# Convert to milliseconds for comparison
HEALTH_MS=$(echo "$HEALTH_RESPONSE_TIME * 1000" | bc -l 2>/dev/null || echo "999")
MEMORY_MS=$(echo "$MEMORY_RESPONSE_TIME * 1000" | bc -l 2>/dev/null || echo "999")

log_info "Health endpoint response time: ${HEALTH_MS}ms"
log_info "Memory health response time: ${MEMORY_MS}ms"

if (( $(echo "$HEALTH_MS < 2000" | bc -l) )); then
    log_success "✅ Health endpoint response time: PASSED (< 2s)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    log_error "❌ Health endpoint response time: FAILED (> 2s)"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

if (( $(echo "$MEMORY_MS < 2000" | bc -l) )); then
    log_success "✅ Memory health response time: PASSED (< 2s)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    log_error "❌ Memory health response time: FAILED (> 2s)"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# 7. Resource Usage Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Resource Usage Tests                       ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

# Check memory usage from health endpoint
MEMORY_USAGE=$(kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health/memory | jq -r '.memoryUsageMB' 2>/dev/null || echo "999")
HEALTH_SCORE=$(kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health/memory | jq -r '.healthScore' 2>/dev/null || echo "0")

log_info "Current memory usage: ${MEMORY_USAGE} MB"
log_info "Current health score: ${HEALTH_SCORE}/100"

if [ "$MEMORY_USAGE" != "null" ] && [ "$MEMORY_USAGE" -lt 1000 ]; then
    log_success "✅ Memory usage: PASSED (< 1000 MB)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    log_warning "⚠️ Memory usage: HIGH (${MEMORY_USAGE} MB)"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

if [ "$HEALTH_SCORE" != "null" ] && [ "$HEALTH_SCORE" -gt 70 ]; then
    log_success "✅ Health score: PASSED (> 70)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    log_warning "⚠️ Health score: LOW (${HEALTH_SCORE}/100)"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# 8. Module Cleanup Tests
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Module Cleanup Tests                       ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

# Check if module cleanup data is present
MODULE_METRICS=$(kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -s ${BASE_URL}/health/memory | jq -r '.moduleMetrics | length' 2>/dev/null || echo "0")

if [ "$MODULE_METRICS" -gt 0 ]; then
    log_success "✅ Module metrics present: PASSED (${MODULE_METRICS} modules)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    log_error "❌ Module metrics: FAILED (no module data)"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

# Test Results Summary
echo ""
echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${PURPLE}║                    Validation Results Summary                 ║${NC}"
echo -e "${PURPLE}╚════════════════════════════════════════════════════════════════╝${NC}"

echo -e "${BLUE}📊 Test Results:${NC}"
echo -e "   • Total Tests: ${TOTAL_TESTS}"
echo -e "   • Passed: ${GREEN}${PASSED_TESTS}${NC}"
echo -e "   • Failed: ${RED}${FAILED_TESTS}${NC}"
echo -e "   • Success Rate: $((PASSED_TESTS * 100 / TOTAL_TESTS))%"

if [ $FAILED_TESTS -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 All validation tests passed! 🎉${NC}"
    echo -e "${GREEN}✨ The deployment is healthy and ready for production ✨${NC}"
    echo ""
    echo -e "${BLUE}🔍 Detailed Status:${NC}"
    echo -e "   • Kubernetes infrastructure: ✅ Healthy"
    echo -e "   • Health endpoints: ✅ Responding"
    echo -e "   • Memory monitoring: ✅ Active"
    echo -e "   • Prometheus metrics: ✅ Available"
    echo -e "   • Performance: ✅ Within limits"
    echo -e "   • Resource usage: ✅ Acceptable"
    echo -e "   • Module cleanup: ✅ Functioning"
    echo ""
    echo -e "${GREEN}🙏 May this deployment serve all beings with wisdom and compassion 🙏${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}⚠️ Some validation tests failed ⚠️${NC}"
    echo -e "${YELLOW}Please review the failed tests and address issues before proceeding${NC}"
    echo ""
    echo -e "${BLUE}🔍 Troubleshooting:${NC}"
    echo -e "   • Check pod logs: kubectl logs -f deployment/living-codex-api -n ${NAMESPACE}"
    echo -e "   • Check pod status: kubectl get pods -n ${NAMESPACE}"
    echo -e "   • Check service: kubectl get service living-codex-service -n ${NAMESPACE}"
    echo -e "   • Manual health check: curl ${BASE_URL}/health"
    echo ""
    echo -e "${YELLOW}💡 Consider running the deployment validation again after addressing issues${NC}"
    exit 1
fi

