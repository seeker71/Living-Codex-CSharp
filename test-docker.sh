#!/bin/bash

echo "🐳 Testing Living Codex Docker Setup"
echo "====================================="

# Stop any existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Build the image
echo "🔨 Building Docker image..."
docker build -t living-codex-api .

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

echo "✅ Docker image built successfully"

# Start only the core services
echo "🚀 Starting core services..."
docker-compose up -d postgres redis

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 10

# Test the API directly with a simple container
echo "🧪 Testing API with simple container..."
docker run --rm -p 5002:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  living-codex-api &

# Wait for API to start
sleep 15

# Test health endpoint
echo "🔍 Testing health endpoint..."
curl -s http://localhost:5002/health || echo "❌ Health check failed"

# Cleanup
echo "🧹 Cleaning up..."
docker-compose down
docker rmi living-codex-api

echo "✅ Docker test completed"

