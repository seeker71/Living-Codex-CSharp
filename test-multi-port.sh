#!/bin/bash

# Multi-Port Test Script for Living Codex
# Tests server startup on different ports as specified in ports.json

set -e

echo "🧪 Living Codex Multi-Port Test Suite"
echo "====================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to check if port is available
is_port_available() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        return 1  # Port is in use
    else
        return 0  # Port is available
    fi
}

# Function to start server on specific port
start_server_on_port() {
    local port=$1
    local service_name=$2
    
    echo -e "\n${BLUE}Testing server startup on port $port ($service_name)${NC}"
    echo "----------------------------------------"
    
    # Kill any existing processes on this port
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "Killing existing process on port $port..."
        lsof -ti:$port | xargs kill -9 2>/dev/null || true
        sleep 2
    fi
    
    # Start server in background
    echo "Starting server on port $port..."
    cd src/CodexBootstrap
    nohup dotnet run --urls "http://localhost:$port" > "../../test-server-$port.log" 2>&1 &
    local server_pid=$!
    cd ../..
    
    # Wait for server to start
    echo "Waiting for server to start on port $port..."
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -s "http://localhost:$port/health" >/dev/null 2>&1; then
            echo -e "${GREEN}✅ Server started successfully on port $port${NC}"
            return 0
        fi
        sleep 2
        ((attempt++))
    done
    
    echo -e "${RED}❌ Server failed to start on port $port within 60 seconds${NC}"
    kill $server_pid 2>/dev/null || true
    return 1
}

# Function to test server endpoints
test_server_endpoints() {
    local port=$1
    local service_name=$2
    
    echo "Testing endpoints on port $port..."
    
    # Test health endpoint
    if curl -s "http://localhost:$port/health" | grep -q "healthy"; then
        echo -e "${GREEN}✅ Health endpoint working on port $port${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ Health endpoint failed on port $port${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Test hello endpoint
    if curl -s "http://localhost:$port/hello" | grep -q "Hello"; then
        echo -e "${GREEN}✅ Hello endpoint working on port $port${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ Hello endpoint failed on port $port${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Test API discovery endpoint
    if curl -s "http://localhost:$port/api/discovery" | grep -q "endpoints"; then
        echo -e "${GREEN}✅ API discovery endpoint working on port $port${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ API discovery endpoint failed on port $port${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Test modules endpoint
    if curl -s "http://localhost:$port/api/modules" | grep -q "modules"; then
        echo -e "${GREEN}✅ Modules endpoint working on port $port${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ Modules endpoint failed on port $port${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
}

# Function to stop server on port
stop_server_on_port() {
    local port=$1
    
    echo "Stopping server on port $port..."
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        lsof -ti:$port | xargs kill -9 2>/dev/null || true
        sleep 2
    fi
    rm -f "test-server-$port.log"
}

# Function to run port tests
run_port_tests() {
    local port=$1
    local service_name=$2
    
    echo -e "\n${YELLOW}Testing port $port ($service_name)${NC}"
    echo "=================================="
    
    if start_server_on_port $port "$service_name"; then
        test_server_endpoints $port "$service_name"
        stop_server_on_port $port
        echo -e "${GREEN}✅ Port $port test completed successfully${NC}"
    else
        echo -e "${RED}❌ Port $port test failed${NC}"
        ((FAILED_TESTS++))
        ((TOTAL_TESTS++))
    fi
}

# Function to run unit tests
run_unit_tests() {
    echo -e "\n${BLUE}🔬 Running Unit Tests${NC}"
    echo "====================="
    
    cd src/CodexBootstrap.Tests
    
    # Build the test project
    echo "Building test project..."
    if ! dotnet build --verbosity quiet; then
        echo -e "${RED}❌ Test project build failed${NC}"
        return 1
    fi
    echo -e "${GREEN}✅ Test project built successfully${NC}"
    
    # Run tests
    echo "Running unit tests..."
    if dotnet test --verbosity normal --logger "console;verbosity=detailed"; then
        echo -e "${GREEN}✅ Unit tests passed${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ Unit tests failed${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    cd ../..
}

# Function to generate test report
generate_test_report() {
    echo -e "\n${BLUE}📊 Test Report${NC}"
    echo "============="
    echo "Total Tests: $TOTAL_TESTS"
    echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
    echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
    
    if [ $FAILED_TESTS -eq 0 ]; then
        echo -e "\n${GREEN}🎉 All tests passed!${NC}"
        return 0
    else
        echo -e "\n${RED}❌ Some tests failed${NC}"
        return 1
    fi
}

# Main execution
main() {
    echo "Starting multi-port test suite..."
    echo "Timestamp: $(date)"
    echo ""
    
    # Clean up any existing processes
    echo "Cleaning up existing processes..."
    pkill -f "dotnet.*CodexBootstrap" || true
    sleep 2
    
    # Run unit tests first
    run_unit_tests
    
    # Test ports from ports.json
    echo -e "\n${BLUE}🌐 Testing Server Startup on Different Ports${NC}"
    echo "============================================="
    
    # Test the main service port
    run_port_tests 5002 "codex-bootstrap"
    
    # Test other configured ports
    run_port_tests 5003 "codex-ai"
    run_port_tests 5004 "codex-storage"
    run_port_tests 5005 "codex-events"
    run_port_tests 5006 "codex-mobile"
    run_port_tests 5007 "codex-admin"
    
    # Test some additional ports to verify port handling
    run_port_tests 5008 "test-port-1"
    run_port_tests 5009 "test-port-2"
    
    # Generate final report
    generate_test_report
    local exit_code=$?
    
    # Cleanup
    echo -e "\n${YELLOW}Cleaning up...${NC}"
    pkill -f "dotnet.*CodexBootstrap" || true
    rm -f test-server-*.log
    
    echo -e "\nTest suite completed at $(date)"
    exit $exit_code
}

# Handle script interruption
trap 'echo -e "\n${YELLOW}Test suite interrupted. Cleaning up...${NC}"; pkill -f "dotnet.*CodexBootstrap" || true; rm -f test-server-*.log; exit 130' INT

# Run main function
main "$@"
