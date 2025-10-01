#!/bin/bash

# Script to run UI tests with real backend API data
# This allows testing against actual backend data instead of mocks

set -e

echo "🧪 Running UI Tests with Real Backend API"
echo "=========================================="

# Check if backend is running
echo "🔍 Checking if backend is available..."
if curl -s http://localhost:5002/health >/dev/null 2>&1; then
    echo "✅ Backend is running on port 5002"
else
    echo "❌ Backend not found on port 5002"
    echo "💡 Start the backend first with: ./start-server.sh"
    exit 1
fi

# Set environment variable to use real API
export USE_REAL_API=true
export TEST_BACKEND_URL=http://localhost:5002

echo "🚀 Running tests with real API data..."
echo "   Backend URL: $TEST_BACKEND_URL"
echo "   Use Real API: $USE_REAL_API"
echo ""

# Run the tests
npm test -- --testNamePattern="Gallery|Profile" --verbose

echo ""
echo "✅ Tests completed with real API data"

