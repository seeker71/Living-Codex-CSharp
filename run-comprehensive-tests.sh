#!/bin/bash

# Comprehensive Test Runner for Living Codex
# This script runs all tests and provides detailed reporting

set -e

echo "🧪 Living Codex Comprehensive Test Suite"
echo "========================================"

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
SKIPPED_TESTS=0

# Function to run tests and capture results
run_test_suite() {
    local test_name="$1"
    local test_command="$2"
    
    echo -e "\n${BLUE}Running $test_name...${NC}"
    echo "Command: $test_command"
    echo "----------------------------------------"
    
    if eval "$test_command"; then
        echo -e "${GREEN}✅ $test_name PASSED${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ $test_name FAILED${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
}

# Function to check if server is running
check_server_running() {
    local port=${1:-5002}
    if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to start server in background
start_server() {
    echo -e "\n${YELLOW}Starting server for integration tests...${NC}"
    
    # Kill any existing processes
    pkill -f "dotnet.*CodexBootstrap" || true
    sleep 2
    
    # Start server in background
    cd src/CodexBootstrap
    nohup dotnet run --configuration Release > ../../test-server.log 2>&1 &
    SERVER_PID=$!
    cd ../..
    
    # Wait for server to start
    echo "Waiting for server to start..."
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if check_server_running 5002; then
            echo -e "${GREEN}✅ Server started successfully${NC}"
            return 0
        fi
        sleep 2
        ((attempt++))
    done
    
    echo -e "${RED}❌ Server failed to start within 60 seconds${NC}"
    return 1
}

# Function to stop server
stop_server() {
    if [ ! -z "$SERVER_PID" ]; then
        echo -e "\n${YELLOW}Stopping server...${NC}"
        kill $SERVER_PID 2>/dev/null || true
        pkill -f "dotnet.*CodexBootstrap" || true
        sleep 2
    fi
}

# Function to run unit tests
run_unit_tests() {
    echo -e "\n${BLUE}🔬 Running Unit Tests${NC}"
    echo "====================="
    
    cd src/CodexBootstrap.Tests
    
    # Run tests with detailed output
    run_test_suite "Unit Tests" "dotnet test --verbosity normal --logger 'console;verbosity=detailed'"
    
    cd ../..
}

# Function to run integration tests
run_integration_tests() {
    echo -e "\n${BLUE}🔗 Running Integration Tests${NC}"
    echo "=============================="
    
    # Start server for integration tests
    if ! start_server; then
        echo -e "${RED}❌ Cannot run integration tests - server failed to start${NC}"
        return 1
    fi
    
    # Run integration tests
    cd src/CodexBootstrap.Tests
    run_test_suite "Integration Tests" "dotnet test --filter 'Category=Integration' --verbosity normal"
    cd ../..
    
    # Stop server
    stop_server
}

# Function to run performance tests
run_performance_tests() {
    echo -e "\n${BLUE}⚡ Running Performance Tests${NC}"
    echo "============================="
    
    # Start server for performance tests
    if ! start_server; then
        echo -e "${RED}❌ Cannot run performance tests - server failed to start${NC}"
        return 1
    fi
    
    # Run performance tests
    cd src/CodexBootstrap.Tests
    run_test_suite "Performance Tests" "dotnet test --filter 'Category=Performance' --verbosity normal"
    cd ../..
    
    # Stop server
    stop_server
}

# Function to run load tests
run_load_tests() {
    echo -e "\n${BLUE}🚀 Running Load Tests${NC}"
    echo "====================="
    
    # Start server for load tests
    if ! start_server; then
        echo -e "${RED}❌ Cannot run load tests - server failed to start${NC}"
        return 1
    fi
    
    # Simple load test using curl
    echo "Running basic load test..."
    local success_count=0
    local total_requests=100
    
    for i in $(seq 1 $total_requests); do
        if curl -s "http://localhost:5002/health" > /dev/null 2>&1; then
            ((success_count++))
        fi
    done
    
    local success_rate=$((success_count * 100 / total_requests))
    echo "Load test completed: $success_count/$total_requests requests successful ($success_rate%)"
    
    if [ $success_rate -ge 95 ]; then
        echo -e "${GREEN}✅ Load test PASSED (${success_rate}% success rate)${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ Load test FAILED (${success_rate}% success rate)${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Stop server
    stop_server
}

# Function to run security tests
run_security_tests() {
    echo -e "\n${BLUE}🔒 Running Security Tests${NC}"
    echo "========================="
    
    # Start server for security tests
    if ! start_server; then
        echo -e "${RED}❌ Cannot run security tests - server failed to start${NC}"
        return 1
    fi
    
    # Test for common security issues
    echo "Testing for SQL injection..."
    local sql_injection_test=$(curl -s "http://localhost:5002/hello?input='; DROP TABLE users; --" | grep -i "error\|exception" | wc -l)
    if [ $sql_injection_test -eq 0 ]; then
        echo -e "${GREEN}✅ SQL injection test PASSED${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ SQL injection test FAILED${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Test for XSS
    echo "Testing for XSS..."
    local xss_test=$(curl -s "http://localhost:5002/hello?input=<script>alert('xss')</script>" | grep -i "script" | wc -l)
    if [ $xss_test -eq 0 ]; then
        echo -e "${GREEN}✅ XSS test PASSED${NC}"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}❌ XSS test FAILED${NC}"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
    
    # Stop server
    stop_server
}

# Function to generate test report
generate_test_report() {
    echo -e "\n${BLUE}📊 Test Report${NC}"
    echo "============="
    echo "Total Tests: $TOTAL_TESTS"
    echo -e "Passed: ${GREEN}$PASSED_TESTS${NC}"
    echo -e "Failed: ${RED}$FAILED_TESTS${NC}"
    echo -e "Skipped: ${YELLOW}$SKIPPED_TESTS${NC}"
    
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
    echo "Starting comprehensive test suite..."
    echo "Timestamp: $(date)"
    echo ""
    
    # Clean up any existing processes
    pkill -f "dotnet.*CodexBootstrap" || true
    sleep 2
    
    # Run all test suites
    run_unit_tests
    run_integration_tests
    run_performance_tests
    run_load_tests
    run_security_tests
    
    # Generate final report
    generate_test_report
    local exit_code=$?
    
    # Cleanup
    stop_server
    rm -f test-server.log
    
    echo -e "\nTest suite completed at $(date)"
    exit $exit_code
}

# Handle script interruption
trap 'echo -e "\n${YELLOW}Test suite interrupted. Cleaning up...${NC}"; stop_server; exit 130' INT

# Run main function
main "$@"
