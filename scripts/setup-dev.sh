#!/bin/bash

# Living Codex Development Environment Setup Script
# This script sets up the complete development environment

set -e

echo "🚀 Setting up Living Codex Development Environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
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

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Create necessary directories
print_status "Creating necessary directories..."
mkdir -p https
mkdir -p monitoring/grafana/dashboards
mkdir -p monitoring/grafana/datasources
mkdir -p monitoring/rules
mkdir -p logs
mkdir -p data

# Generate SSL certificates for HTTPS
print_status "Generating SSL certificates..."
if [ ! -f "https/cert.pem" ] || [ ! -f "https/key.pem" ]; then
    openssl req -x509 -newkey rsa:4096 -keyout https/key.pem -out https/cert.pem -days 365 -nodes -subj "/C=US/ST=State/L=City/O=Organization/CN=localhost"
    print_success "SSL certificates generated"
else
    print_warning "SSL certificates already exist, skipping generation"
fi

# Generate .env file if it doesn't exist
if [ ! -f ".env" ]; then
    print_status "Creating .env file..."
    cat > .env << EOF
# Living Codex Environment Variables
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=your-super-secret-jwt-key-that-should-be-32-characters-long
ENCRYPTION_KEY=your-32-character-encryption-key-here
POSTGRES_PASSWORD=postgres
REDIS_PASSWORD=
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
GRAFANA_ADMIN_PASSWORD=admin
EOF
    print_success ".env file created"
else
    print_warning ".env file already exists, skipping creation"
fi

# Build the application
print_status "Building Living Codex application..."
dotnet build src/CodexBootstrap/CodexBootstrap.csproj -c Release
if [ $? -eq 0 ]; then
    print_success "Application built successfully"
else
    print_error "Failed to build application"
    exit 1
fi

# Start the services
print_status "Starting Docker services..."
docker-compose up -d

# Wait for services to be ready
print_status "Waiting for services to be ready..."
sleep 30

# Check service health
print_status "Checking service health..."

# Check Living Codex API
if curl -f http://localhost:5000/health > /dev/null 2>&1; then
    print_success "Living Codex API is healthy"
else
    print_warning "Living Codex API health check failed"
fi

# Check PostgreSQL
if docker exec living-codex-postgres pg_isready -U postgres > /dev/null 2>&1; then
    print_success "PostgreSQL is ready"
else
    print_warning "PostgreSQL health check failed"
fi

# Check Redis
if docker exec living-codex-redis redis-cli ping > /dev/null 2>&1; then
    print_success "Redis is ready"
else
    print_warning "Redis health check failed"
fi

# Check RabbitMQ
if curl -f http://localhost:15672 > /dev/null 2>&1; then
    print_success "RabbitMQ is ready"
else
    print_warning "RabbitMQ health check failed"
fi

# Pull Ollama models
print_status "Pulling Ollama models..."
docker exec living-codex-ollama ollama pull llama3 || print_warning "Failed to pull llama3 model"
docker exec living-codex-ollama ollama pull gpt-oss:20b || print_warning "Failed to pull gpt-oss:20b model"

# Display service URLs
print_success "Development environment setup complete!"
echo ""
echo "🌐 Service URLs:"
echo "  Living Codex API:     http://localhost:5000"
echo "  Living Codex API (HTTPS): https://localhost:5001"
echo "  Nginx Load Balancer:  http://localhost:80"
echo "  Nginx Load Balancer (HTTPS): https://localhost:443"
echo "  PostgreSQL:           localhost:5432"
echo "  Redis:                localhost:6379"
echo "  RabbitMQ Management:  http://localhost:15672"
echo "  Prometheus:           http://localhost:9090"
echo "  Grafana:              http://localhost:3000 (admin/admin)"
echo "  Jaeger:               http://localhost:16686"
echo "  Elasticsearch:        http://localhost:9200"
echo "  Kibana:               http://localhost:5601"
echo "  Adminer (DB Admin):   http://localhost:8080"
echo "  Redis Commander:      http://localhost:8081"
echo ""
echo "🔧 Management Commands:"
echo "  View logs:            docker-compose logs -f [service-name]"
echo "  Stop services:        docker-compose down"
echo "  Restart services:     docker-compose restart [service-name]"
echo "  Scale API instances:  docker-compose up -d --scale living-codex-api=3"
echo ""
echo "📚 Next Steps:"
echo "  1. Access the API at http://localhost:5000"
echo "  2. Check the health endpoint: http://localhost:5000/health"
echo "  3. View API documentation: http://localhost:5000/swagger"
echo "  4. Monitor services in Grafana: http://localhost:3000"
echo "  5. Check logs: docker-compose logs -f living-codex-api"
echo ""
print_success "Happy coding! 🎉"
