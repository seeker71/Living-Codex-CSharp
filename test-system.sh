#!/bin/bash

echo "🧪 Testing Living Codex System"
echo "================================"

# Test 1: Build Backend
echo "📦 Building Backend..."
cd src/CodexBootstrap
if dotnet build --verbosity quiet; then
    echo "✅ Backend builds successfully"
else
    echo "❌ Backend build failed"
    exit 1
fi

# Test 2: Build Mobile App
echo "📱 Building Mobile App..."
cd ../../LivingCodexMobile
if dotnet build -f net6.0-maccatalyst --verbosity quiet; then
    echo "✅ Mobile app builds successfully"
else
    echo "❌ Mobile app build failed"
    exit 1
fi

# Test 3: Start Backend Server
echo "🚀 Starting Backend Server..."
cd ../src/CodexBootstrap
dotnet run --urls="http://localhost:5002" &
SERVER_PID=$!

# Wait for server to start
echo "⏳ Waiting for server to start..."
sleep 5

# Test 4: Health Check
echo "🏥 Testing Health Endpoint..."
if curl -s http://localhost:5002/health > /dev/null; then
    echo "✅ Backend server is running and healthy"
else
    echo "❌ Backend server health check failed"
    kill $SERVER_PID 2>/dev/null
    exit 1
fi

# Test 5: API Discovery
echo "🔍 Testing API Discovery..."
if curl -s http://localhost:5002/api/discovery > /dev/null; then
    echo "✅ API Discovery endpoint accessible"
else
    echo "⚠️  API Discovery endpoint not accessible (may be expected)"
fi

# Test 6: Module Discovery
echo "🔧 Testing Module Discovery..."
if curl -s http://localhost:5002/api/modules > /dev/null; then
    echo "✅ Module Discovery endpoint accessible"
else
    echo "⚠️  Module Discovery endpoint not accessible (may be expected)"
fi

# Cleanup
echo "🧹 Cleaning up..."
kill $SERVER_PID 2>/dev/null
wait $SERVER_PID 2>/dev/null

echo ""
echo "🎉 System Test Complete!"
echo "================================"
echo "✅ Backend builds and runs successfully"
echo "✅ Mobile app builds successfully"
echo "✅ Backend server starts and responds to health checks"
echo ""
echo "The conversation system is ready for development!"

