#!/bin/bash

# Living Codex Test Runner
# Runs comprehensive test suite for all API endpoints

set -e

echo "🧪 Living Codex Test Suite Runner"
echo "=================================="

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/CodexBootstrap"
TEST_DIR="$SCRIPT_DIR/src/CodexBootstrap.Tests"

# Configuration
SERVER_URL="http://localhost:5002"
TIMEOUT=30

echo "📁 Project directory: $PROJECT_DIR"
echo "🧪 Test directory: $TEST_DIR"
echo "🌐 Server URL: $SERVER_URL"
echo ""

# Function to check if server is running
check_server() {
    echo "🔍 Checking if server is running..."
    if curl -s "$SERVER_URL/health" >/dev/null 2>&1; then
        echo "✅ Server is running"
        return 0
    else
        echo "❌ Server is not running on $SERVER_URL"
        echo "   Please start the server first with: ./start-server.sh"
        return 1
    fi
}

# Function to run tests
run_tests() {
    echo "🚀 Running test suite..."
    echo ""
    
    cd "$TEST_DIR"
    
    # Build the test project
    echo "🔨 Building test project..."
    if ! dotnet build --verbosity quiet; then
        echo "❌ Test project build failed"
        return 1
    fi
    echo "✅ Test project built successfully"
    echo ""
    
    # Run tests with detailed output
    echo "🧪 Executing tests..."
    dotnet test --verbosity normal --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"
    
    echo ""
    echo "✅ Test execution completed"
}

# Function to generate test report
generate_report() {
    echo "📊 Generating test report..."
    
    # Get test results
    local test_count=$(curl -s "$SERVER_URL/health" | jq -r '.registrationMetrics.totalRoutesRegistered // 0')
    local module_count=$(curl -s "$SERVER_URL/health" | jq -r '.moduleCount // 0')
    
    echo ""
    echo "📈 Test Summary Report"
    echo "====================="
    echo "🌐 Server URL: $SERVER_URL"
    echo "📊 Total Routes: $test_count"
    echo "🔧 Total Modules: $module_count"
    echo "⏰ Test Time: $(date)"
    echo ""
}

# Main execution
main() {
    echo "Starting test execution..."
    echo ""
    
    # Check if server is running
    if ! check_server; then
        exit 1
    fi
    
    echo ""
    
    # Run tests
    if run_tests; then
        echo ""
        generate_report
        echo "🎉 All tests completed successfully!"
    else
        echo ""
        echo "❌ Some tests failed. Check the output above for details."
        exit 1
    fi
}

# Run main function
main "$@"
