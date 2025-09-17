#!/bin/bash

# Comprehensive API Timeout and Performance Test
# Tests all major endpoints for response times and timeout scenarios

BACKEND_URL="http://localhost:5002"
TIMEOUT_LIMIT=5  # 5 seconds timeout limit
SLOW_THRESHOLD=2 # 2 seconds considered slow

echo "🧪 Testing API Endpoints for Timeouts and Performance"
echo "Backend: $BACKEND_URL"
echo "Timeout Limit: ${TIMEOUT_LIMIT}s"
echo "Slow Threshold: ${SLOW_THRESHOLD}s"
echo "=================================================="

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
SLOW_TESTS=0
TIMEOUT_TESTS=0

# Test function
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo -n "Testing: $description... "
    
    # Build curl command
    local curl_cmd="curl -s -w 'STATUS:%{http_code},TIME:%{time_total}' --max-time $TIMEOUT_LIMIT"
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        curl_cmd="$curl_cmd -X POST -H 'Content-Type: application/json' -d '$data'"
    elif [ "$method" = "POST" ]; then
        curl_cmd="$curl_cmd -X POST -H 'Content-Type: application/json'"
    fi
    
    curl_cmd="$curl_cmd '$BACKEND_URL$endpoint' -o /dev/null"
    
    # Execute test
    local result=$(eval $curl_cmd 2>&1)
    local exit_code=$?
    
    if [ $exit_code -eq 28 ]; then
        # Timeout
        echo -e "${RED}TIMEOUT${NC} (>${TIMEOUT_LIMIT}s)"
        TIMEOUT_TESTS=$((TIMEOUT_TESTS + 1))
        FAILED_TESTS=$((FAILED_TESTS + 1))
    elif [ $exit_code -ne 0 ]; then
        # Other error
        echo -e "${RED}ERROR${NC} (exit code: $exit_code)"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    else
        # Parse result
        local status=$(echo "$result" | grep -o 'STATUS:[0-9]*' | cut -d: -f2)
        local time=$(echo "$result" | grep -o 'TIME:[0-9.]*' | cut -d: -f2)
        
        if [ -z "$status" ] || [ -z "$time" ]; then
            echo -e "${RED}PARSE_ERROR${NC} (result: $result)"
            FAILED_TESTS=$((FAILED_TESTS + 1))
        else
            # Check if slow
            local is_slow=$(echo "$time > $SLOW_THRESHOLD" | bc -l)
            
            if [ "$is_slow" = "1" ]; then
                echo -e "${YELLOW}SLOW${NC} (${time}s, HTTP $status)"
                SLOW_TESTS=$((SLOW_TESTS + 1))
                PASSED_TESTS=$((PASSED_TESTS + 1))
            elif [ "$status" -ge 200 ] && [ "$status" -lt 300 ]; then
                echo -e "${GREEN}OK${NC} (${time}s, HTTP $status)"
                PASSED_TESTS=$((PASSED_TESTS + 1))
            elif [ "$status" -eq 404 ] || [ "$status" -eq 405 ]; then
                echo -e "${BLUE}EXPECTED${NC} (${time}s, HTTP $status)"
                PASSED_TESTS=$((PASSED_TESTS + 1))
            else
                echo -e "${RED}HTTP_ERROR${NC} (${time}s, HTTP $status)"
                FAILED_TESTS=$((FAILED_TESTS + 1))
            fi
        fi
    fi
}

echo ""
echo "🏥 Health and System Endpoints"
echo "------------------------------"
test_endpoint "GET" "/health" "Health Check"
test_endpoint "GET" "/storage-endpoints/stats" "Storage Stats"
test_endpoint "GET" "/metrics" "System Metrics"
test_endpoint "GET" "/metrics/health" "Metrics Health"
test_endpoint "GET" "/metrics/performance" "Performance Metrics"

echo ""
echo "👥 User and Identity Endpoints"
echo "------------------------------"
test_endpoint "GET" "/identity/providers" "Identity Providers"
test_endpoint "GET" "/contributions/stats/demo-user" "User Contribution Stats"

echo ""
echo "🧠 Concept and Knowledge Endpoints"
echo "-----------------------------------"
test_endpoint "GET" "/concepts" "Get All Concepts"
test_endpoint "GET" "/storage-endpoints/nodes?typeId=codex.concept&limit=10" "Concept Nodes"

echo ""
echo "⚡ Energy and Resonance Endpoints"
echo "---------------------------------"
test_endpoint "GET" "/contributions/abundance/collective-energy" "Collective Energy"
test_endpoint "GET" "/contributions/abundance/contributor-energy/demo-user" "Contributor Energy"

echo ""
echo "📰 News and Content Endpoints"
echo "-----------------------------"
test_endpoint "GET" "/news/trending?limit=5" "Trending Topics"
test_endpoint "GET" "/news/feed/demo-user?limit=10" "News Feed"
test_endpoint "POST" "/news/search" "News Search" '{"interests": ["technology"], "limit": 5}'

echo ""
echo "🗃️ Storage and Node Endpoints"
echo "-----------------------------"
test_endpoint "GET" "/storage-endpoints/nodes?limit=10" "List Nodes"
test_endpoint "GET" "/storage-endpoints/edges?limit=10" "List Edges"
test_endpoint "POST" "/storage-endpoints/nodes/search" "Search Nodes" '{"query": "test", "limit": 5}'

echo ""
echo "🔍 Spec and Module Endpoints"
echo "----------------------------"
test_endpoint "GET" "/spec/modules/all" "All Modules"
test_endpoint "GET" "/spec/routes/all" "All Routes"
test_endpoint "GET" "/spec/status/overview" "Spec Status"

echo ""
echo "📊 Test Results Summary"
echo "======================="
echo -e "Total Tests:    ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Passed:         ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed:         ${RED}$FAILED_TESTS${NC}"
echo -e "Timeouts:       ${RED}$TIMEOUT_TESTS${NC}"
echo -e "Slow Responses: ${YELLOW}$SLOW_TESTS${NC}"

# Calculate success rate
if [ $TOTAL_TESTS -gt 0 ]; then
    SUCCESS_RATE=$(echo "scale=1; $PASSED_TESTS * 100 / $TOTAL_TESTS" | bc -l)
    echo -e "Success Rate:   ${GREEN}${SUCCESS_RATE}%${NC}"
fi

echo ""
if [ $FAILED_TESTS -eq 0 ] && [ $TIMEOUT_TESTS -eq 0 ]; then
    echo -e "${GREEN}✅ All API endpoints are responsive and working correctly!${NC}"
elif [ $TIMEOUT_TESTS -gt 0 ]; then
    echo -e "${RED}⚠️ $TIMEOUT_TESTS endpoints have timeout issues that need investigation${NC}"
elif [ $SLOW_TESTS -gt 5 ]; then
    echo -e "${YELLOW}⚠️ $SLOW_TESTS endpoints are slow and might need optimization${NC}"
else
    echo -e "${YELLOW}⚠️ Some endpoints have issues but no critical timeouts detected${NC}"
fi

echo ""
echo "💡 Recommendations:"
if [ $TIMEOUT_TESTS -gt 0 ]; then
    echo "   • Investigate timeout endpoints for database/external service issues"
    echo "   • Consider adding caching for slow operations"
    echo "   • Review database query performance"
fi
if [ $SLOW_TESTS -gt 3 ]; then
    echo "   • Optimize slow endpoints with caching or async processing"
    echo "   • Consider pagination for large data sets"
    echo "   • Review database indexing"
fi
if [ $FAILED_TESTS -gt 0 ]; then
    echo "   • Check server logs for failed endpoint errors"
    echo "   • Verify all required services are running"
fi

echo ""
echo "🔧 To fix timeout issues:"
echo "   1. Check server logs: tail -f logs/server-*.log"
echo "   2. Monitor database performance"
echo "   3. Add appropriate caching strategies"
echo "   4. Consider async processing for heavy operations"
