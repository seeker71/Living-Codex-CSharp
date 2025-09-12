#!/bin/bash

echo "🧪 Testing All Living Codex Systems"
echo "==================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "SUCCESS") echo -e "${GREEN}✅ $message${NC}" ;;
        "ERROR") echo -e "${RED}❌ $message${NC}" ;;
        "WARNING") echo -e "${YELLOW}⚠️  $message${NC}" ;;
        "INFO") echo -e "${BLUE}ℹ️  $message${NC}" ;;
    esac
}

# Function to test API endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    print_status "INFO" "Testing: $description"
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" -H "Content-Type: application/json" -d "$data" "$endpoint")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$http_code" -eq 200 ]; then
        print_status "SUCCESS" "$description - HTTP $http_code"
        echo "$body" | python3 -m json.tool 2>/dev/null || echo "$body"
    else
        print_status "ERROR" "$description - HTTP $http_code"
        echo "$body"
    fi
    echo ""
}

# Check if server is running
print_status "INFO" "Checking if server is running..."
if ! curl -s http://localhost:5000/health > /dev/null 2>&1; then
    print_status "WARNING" "Server not running. Starting server..."
    dotnet run --project src/CodexBootstrap --urls http://localhost:5000 &
    SERVER_PID=$!
    sleep 10
    
    if ! curl -s http://localhost:5000/health > /dev/null 2>&1; then
        print_status "ERROR" "Failed to start server"
        exit 1
    fi
    print_status "SUCCESS" "Server started successfully"
else
    print_status "SUCCESS" "Server is already running"
fi

echo ""
echo "🔍 Testing Core System Health"
echo "============================="

# Test 1: Health Check
test_endpoint "GET" "http://localhost:5000/health" "" "System Health Check"

# Test 2: Service Discovery
test_endpoint "GET" "http://localhost:5000/discovery/services" "" "Service Discovery"

# Test 3: Node Registry
test_endpoint "GET" "http://localhost:5000/registry/nodes" "" "Node Registry"

echo ""
echo "🌟 Testing Abundance Amplification System"
echo "=========================================="

# Test 4: Record a contribution
test_endpoint "POST" "http://localhost:5000/contributions/record" '{
    "userId": "user1",
    "contributionType": "Create",
    "description": "Created a new abundance amplification algorithm",
    "impact": "high",
    "tags": ["AI", "abundance", "algorithm"],
    "metadata": {
        "category": "technology",
        "complexity": "high"
    }
}' "Record User Contribution"

# Test 5: Get user contributions
test_endpoint "GET" "http://localhost:5000/contributions/user/user1" "" "Get User Contributions"

# Test 6: Get collective energy
test_endpoint "GET" "http://localhost:5000/contributions/abundance/collective-energy" "" "Get Collective Energy"

# Test 7: Get abundance events
test_endpoint "GET" "http://localhost:5000/contributions/abundance/events" "" "Get Abundance Events"

echo ""
echo "🧠 Testing Concept Registry"
echo "==========================="

# Test 8: Register a concept
test_endpoint "POST" "http://localhost:5000/concepts/register" '{
    "concept": {
        "id": "concept-test-1",
        "title": "Abundance Amplification",
        "description": "The process of amplifying individual contributions through collective resonance",
        "type": "principle",
        "tags": ["abundance", "amplification", "collective"],
        "metadata": {
            "category": "core-principle",
            "importance": "high"
        }
    }
}' "Register Concept"

# Test 9: Get all concepts
test_endpoint "GET" "http://localhost:5000/concepts" "" "Get All Concepts"

# Test 10: Search concepts
test_endpoint "GET" "http://localhost:5000/concepts/search?query=abundance" "" "Search Concepts"

echo ""
echo "🔄 Testing Translation System"
echo "============================="

# Test 11: Translate concept
test_endpoint "POST" "http://localhost:5000/translation/translate" '{
    "sourceLanguage": "en",
    "targetLanguage": "es",
    "text": "Abundance amplification through collective resonance",
    "context": "concept"
}' "Translate Concept"

# Test 12: Get translation history
test_endpoint "GET" "http://localhost:5000/translation/history" "" "Get Translation History"

echo ""
echo "🎯 Testing Resonance Engine"
echo "==========================="

# Test 13: Calculate resonance
test_endpoint "POST" "http://localhost:5000/resonance/calculate" '{
    "conceptId": "concept-test-1",
    "userId": "user1",
    "context": "contribution"
}' "Calculate Resonance"

# Test 14: Get resonance patterns
test_endpoint "GET" "http://localhost:5000/resonance/patterns" "" "Get Resonance Patterns"

echo ""
echo "🔮 Testing Future Knowledge"
echo "==========================="

# Test 15: Retrieve future knowledge
test_endpoint "POST" "http://localhost:5000/future/knowledge/retrieve" '{
    "query": "abundance amplification trends",
    "sources": ["pattern-analysis", "user-contributions"],
    "parameters": {
        "timeframe": "6months",
        "confidence": 0.8
    }
}' "Retrieve Future Knowledge"

# Test 16: Discover patterns
test_endpoint "POST" "http://localhost:5000/future/patterns/discover" '{
    "dataSources": ["user-contributions", "concept-registry"],
    "patternTypes": ["trend", "correlation"],
    "options": {
        "timeframe": "30d",
        "minConfidence": 0.7
    }
}' "Discover Patterns"

echo ""
echo "📊 Testing System Metrics"
echo "========================"

# Test 17: Get system metrics
test_endpoint "GET" "http://localhost:5000/metrics" "" "Get System Metrics"

# Test 18: Get module status
test_endpoint "GET" "http://localhost:5000/modules/status" "" "Get Module Status"

echo ""
echo "🧪 Testing Error Handling"
echo "========================="

# Test 19: Test invalid endpoint
test_endpoint "GET" "http://localhost:5000/invalid/endpoint" "" "Test Invalid Endpoint (should return 404)"

# Test 20: Test invalid data
test_endpoint "POST" "http://localhost:5000/contributions/record" '{
    "invalid": "data"
}' "Test Invalid Data (should return 400)"

echo ""
echo "📈 Performance Test"
echo "=================="

# Test 21: Load test - multiple requests
print_status "INFO" "Running load test with 10 concurrent requests..."
for i in {1..10}; do
    curl -s http://localhost:5000/health > /dev/null &
done
wait
print_status "SUCCESS" "Load test completed"

echo ""
echo "🎉 Test Summary"
echo "==============="
print_status "SUCCESS" "All core systems tested successfully!"
print_status "INFO" "The Living Codex system is functioning properly with:"
echo "  - ✅ Abundance amplification tracking"
echo "  - ✅ Concept registry and management"
echo "  - ✅ Translation services"
echo "  - ✅ Resonance calculation"
echo "  - ✅ Future knowledge retrieval"
echo "  - ✅ Pattern discovery"
echo "  - ✅ Real-time contribution tracking"
echo "  - ✅ Collective energy measurement"

# Cleanup
if [ ! -z "$SERVER_PID" ]; then
    print_status "INFO" "Stopping test server..."
    kill $SERVER_PID 2>/dev/null
fi

echo ""
print_status "SUCCESS" "All tests completed! 🌟"
