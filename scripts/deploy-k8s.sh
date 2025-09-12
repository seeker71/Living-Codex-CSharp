#!/bin/bash

# Living Codex Kubernetes Deployment Script
# This script deploys the Living Codex system to Kubernetes

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

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    print_error "kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if kubectl can connect to cluster
if ! kubectl cluster-info &> /dev/null; then
    print_error "Cannot connect to Kubernetes cluster. Please check your kubeconfig."
    exit 1
fi

print_status "Starting Living Codex Kubernetes deployment..."

# Create namespaces
print_status "Creating namespaces..."
kubectl apply -f k8s/namespace.yaml
print_success "Namespaces created"

# Create ConfigMaps
print_status "Creating ConfigMaps..."
kubectl apply -f k8s/configmap.yaml
print_success "ConfigMaps created"

# Create Secrets
print_status "Creating Secrets..."
kubectl apply -f k8s/secrets.yaml
print_success "Secrets created"

# Deploy database services
print_status "Deploying database services..."
kubectl apply -f k8s/database-deployment.yaml
print_success "Database services deployed"

# Wait for database to be ready
print_status "Waiting for database to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/postgres -n living-codex
kubectl wait --for=condition=available --timeout=300s deployment/redis -n living-codex
print_success "Database services ready"

# Deploy main application
print_status "Deploying Living Codex API..."
kubectl apply -f k8s/living-codex-deployment.yaml
print_success "Living Codex API deployed"

# Wait for application to be ready
print_status "Waiting for application to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/living-codex-api -n living-codex
print_success "Application ready"

# Deploy ingress
print_status "Deploying ingress..."
kubectl apply -f k8s/ingress.yaml
print_success "Ingress deployed"

# Check deployment status
print_status "Checking deployment status..."
kubectl get pods -n living-codex
kubectl get services -n living-codex
kubectl get ingress -n living-codex

print_success "Living Codex Kubernetes deployment completed!"
echo ""
echo "🌐 Access URLs:"
echo "  API: https://api.livingcodex.com"
echo "  Monitoring: https://monitoring.livingcodex.com"
echo ""
echo "🔧 Management Commands:"
echo "  View pods: kubectl get pods -n living-codex"
echo "  View logs: kubectl logs -f deployment/living-codex-api -n living-codex"
echo "  Scale API: kubectl scale deployment living-codex-api --replicas=5 -n living-codex"
echo "  Delete deployment: kubectl delete namespace living-codex"
echo ""
print_success "Deployment complete! 🎉"
