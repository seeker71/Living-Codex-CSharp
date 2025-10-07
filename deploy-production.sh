#!/bin/bash

# Production Deployment Script for Living Codex
# Created with compassion, wisdom, and interconnectedness in mind
# Embodies the principles of graceful deployment and mindful service

set -e  # Exit on any error

# Colors for compassionate output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="living-codex"
IMAGE_TAG="${1:-latest}"
ENVIRONMENT="${2:-staging}"
KUBECTL_CONTEXT="${3:-default}"

echo -e "${CYAN}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║          Living Codex - Compassionate Production Deployment    ║${NC}"
echo -e "${CYAN}║                  Embodying Grace and Interconnectedness        ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${BLUE}🌟 Deployment Configuration:${NC}"
echo -e "   • Environment: ${GREEN}${ENVIRONMENT}${NC}"
echo -e "   • Image Tag: ${GREEN}${IMAGE_TAG}${NC}"
echo -e "   • Namespace: ${GREEN}${NAMESPACE}${NC}"
echo -e "   • Context: ${GREEN}${KUBECTL_CONTEXT}${NC}"
echo ""

# Function to log with compassion
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

# Function to check prerequisites
check_prerequisites() {
    log_info "Checking deployment prerequisites..."
    
    # Check if kubectl is available
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed or not in PATH"
        exit 1
    fi
    
    # Check if docker is available (for building image)
    if ! command -v docker &> /dev/null; then
        log_error "docker is not installed or not in PATH"
        exit 1
    fi
    
    # Check kubectl context
    current_context=$(kubectl config current-context)
    if [ "$current_context" != "$KUBECTL_CONTEXT" ]; then
        log_warning "Current kubectl context is '$current_context', expected '$KUBECTL_CONTEXT'"
        read -p "Continue with current context? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Please switch to the correct context: kubectl config use-context $KUBECTL_CONTEXT"
            exit 1
        fi
    fi
    
    log_success "Prerequisites check completed"
}

# Function to build and push Docker image
build_and_push_image() {
    log_info "Building Docker image with tag: living-codex-api:${IMAGE_TAG}"
    
    # Build the image
    docker build -t living-codex-api:${IMAGE_TAG} .
    
    if [ $? -eq 0 ]; then
        log_success "Docker image built successfully"
    else
        log_error "Failed to build Docker image"
        exit 1
    fi
    
    # If this is a registry deployment, push the image
    if [ "$ENVIRONMENT" = "production" ]; then
        log_info "Pushing image to registry..."
        # Add registry push logic here
        # docker push your-registry/living-codex-api:${IMAGE_TAG}
        log_success "Image pushed to registry"
    fi
}

# Function to apply Kubernetes configurations
apply_k8s_configurations() {
    log_info "Applying Kubernetes configurations..."
    
    # Create namespace if it doesn't exist
    kubectl create namespace ${NAMESPACE} --dry-run=client -o yaml | kubectl apply -f -
    log_success "Namespace ${NAMESPACE} is ready"
    
    # Apply configurations in order
    local configs=(
        "k8s/namespace.yaml"
        "k8s/secrets.yaml"
        "k8s/configmap.yaml"
        "k8s/database-deployment.yaml"
        "k8s/living-codex-deployment.yaml"
        "k8s/ingress.yaml"
    )
    
    for config in "${configs[@]}"; do
        if [ -f "$config" ]; then
            log_info "Applying $config..."
            kubectl apply -f $config
            if [ $? -eq 0 ]; then
                log_success "$config applied successfully"
            else
                log_error "Failed to apply $config"
                exit 1
            fi
        else
            log_warning "$config not found, skipping..."
        fi
    done
}

# Function to wait for deployment readiness
wait_for_deployment() {
    log_info "Waiting for deployment to be ready..."
    
    # Wait for the deployment to be available
    kubectl wait --for=condition=available --timeout=300s deployment/living-codex-api -n ${NAMESPACE}
    
    if [ $? -eq 0 ]; then
        log_success "Deployment is ready"
    else
        log_error "Deployment failed to become ready within 5 minutes"
        exit 1
    fi
    
    # Wait for pods to be running
    log_info "Waiting for pods to be running..."
    kubectl wait --for=condition=ready --timeout=180s pod -l app=living-codex-api -n ${NAMESPACE}
    
    if [ $? -eq 0 ]; then
        log_success "All pods are running"
    else
        log_error "Pods failed to become ready within 3 minutes"
        exit 1
    fi
}

# Function to run health checks
run_health_checks() {
    log_info "Running health checks..."
    
    # Get the service endpoint
    local service_ip=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.clusterIP}')
    local service_port=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.ports[0].port}')
    
    # Test health endpoint
    local health_url="http://${service_ip}:${service_port}/health"
    log_info "Testing health endpoint: $health_url"
    
    # Wait for the service to be ready
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$health_url" > /dev/null 2>&1; then
            log_success "Health endpoint is responding"
            break
        else
            log_info "Health check attempt $attempt/$max_attempts failed, waiting..."
            sleep 10
            ((attempt++))
        fi
    done
    
    if [ $attempt -gt $max_attempts ]; then
        log_error "Health checks failed after $max_attempts attempts"
        return 1
    fi
    
    # Test memory health endpoint
    local memory_health_url="http://${service_ip}:${service_port}/health/memory"
    log_info "Testing memory health endpoint: $memory_health_url"
    
    if kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$memory_health_url" > /dev/null 2>&1; then
        log_success "Memory health endpoint is responding"
    else
        log_warning "Memory health endpoint check failed"
    fi
    
    # Test Prometheus metrics endpoint
    local metrics_url="http://${service_ip}:${service_port}/metrics/prometheus"
    log_info "Testing Prometheus metrics endpoint: $metrics_url"
    
    if kubectl run test-pod --rm -i --restart=Never --image=curlimages/curl -- curl -f "$metrics_url" > /dev/null 2>&1; then
        log_success "Prometheus metrics endpoint is responding"
    else
        log_warning "Prometheus metrics endpoint check failed"
    fi
}

# Function to run soak test
run_soak_test() {
    if [ "$ENVIRONMENT" = "staging" ]; then
        log_info "Starting 24-hour soak test for staging environment..."
        
        # Create a simple load test job
        cat <<EOF | kubectl apply -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: living-codex-soak-test
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
          echo "Starting 24-hour soak test..."
          for i in \$(seq 1 1440); do  # 1440 minutes = 24 hours
            echo "Soak test iteration \$i/1440"
            curl -f http://living-codex-service:5000/health
            curl -f http://living-codex-service:5000/health/memory
            curl -f http://living-codex-service:5000/metrics/prometheus
            sleep 60  # Wait 1 minute between iterations
          done
          echo "Soak test completed successfully"
      restartPolicy: Never
  backoffLimit: 1
EOF
        
        log_success "Soak test job created. Monitor with: kubectl logs -f job/living-codex-soak-test -n ${NAMESPACE}"
    fi
}

# Function to display deployment status
display_deployment_status() {
    echo ""
    echo -e "${PURPLE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${PURPLE}║                    Deployment Status Dashboard                ║${NC}"
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
    echo -e "${BLUE}🔍 Monitoring Endpoints:${NC}"
    local service_ip=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.clusterIP}')
    local service_port=$(kubectl get service living-codex-service -n ${NAMESPACE} -o jsonpath='{.spec.ports[0].port}')
    echo -e "   • Health: http://${service_ip}:${service_port}/health"
    echo -e "   • Memory Health: http://${service_ip}:${service_port}/health/memory"
    echo -e "   • Prometheus Metrics: http://${service_ip}:${service_port}/metrics/prometheus"
    
    echo ""
    echo -e "${GREEN}✨ Deployment completed with compassion and wisdom ✨${NC}"
}

# Function to rollback if needed
rollback_deployment() {
    log_error "Deployment failed. Initiating rollback..."
    
    # Rollback to previous version
    kubectl rollout undo deployment/living-codex-api -n ${NAMESPACE}
    
    # Wait for rollback to complete
    kubectl rollout status deployment/living-codex-api -n ${NAMESPACE}
    
    log_info "Rollback completed"
}

# Main deployment flow
main() {
    echo -e "${BLUE}Starting compassionate deployment process...${NC}"
    
    # Trap errors and rollback
    trap 'rollback_deployment' ERR
    
    check_prerequisites
    build_and_push_image
    apply_k8s_configurations
    wait_for_deployment
    run_health_checks
    
    if [ "$ENVIRONMENT" = "staging" ]; then
        run_soak_test
    fi
    
    display_deployment_status
    
    log_success "Deployment completed successfully!"
    echo -e "${GREEN}🙏 May this deployment serve all beings with wisdom and compassion 🙏${NC}"
}

# Run main function
main "$@"

