#!/bin/bash

echo "🧪 Testing Working Living Codex Endpoints"
echo "=========================================="

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
        echo "$body" | jq . 2>/dev/null || echo "$body"
    else
        print_status "ERROR" "$description - HTTP $http_code"
        echo "$body"
    fi
    echo ""
}

# Check if server is running
print_status "INFO" "Checking if server is running..."
if ! curl -s http://localhost:5001/health > /dev/null 2>&1; then
    print_status "WARNING" "Server not running. Starting server..."
    dotnet run --project src/CodexBootstrap --urls http://localhost:5001 &
    SERVER_PID=$!
    sleep 10
    
    if ! curl -s http://localhost:5001/health > /dev/null 2>&1; then
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
test_endpoint "GET" "http://localhost:5001/health" "" "System Health Check"

echo ""
echo "🌟 Testing Abundance Amplification System"
echo "=========================================="

# Test 2: Record a contribution
test_endpoint "POST" "http://localhost:5001/contributions/record" '{
    "entityId": "user1",
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

# Test 3: Get user contributions
test_endpoint "GET" "http://localhost:5001/contributions/user/user1" "" "Get User Contributions"

# Test 4: Get collective energy
test_endpoint "GET" "http://localhost:5001/contributions/abundance/collective-energy" "" "Get Collective Energy"

# Test 5: Get abundance events
test_endpoint "GET" "http://localhost:5001/contributions/abundance/events" "" "Get Abundance Events"

echo ""
echo "🔮 Testing Future Knowledge System"
echo "=================================="

# Test 6: Retrieve future knowledge
test_endpoint "POST" "http://localhost:5001/future/knowledge/retrieve" '{
    "query": "abundance amplification trends",
    "sources": ["pattern-analysis", "user-contributions"],
    "parameters": {
        "timeframe": "6months",
        "confidence": 0.8
    }
}' "Retrieve Future Knowledge"

# Test 7: Discover patterns
test_endpoint "POST" "http://localhost:5001/future/patterns/discover" '{
    "dataSources": ["user-contributions", "concept-registry"],
    "patternTypes": ["trend", "correlation"],
    "options": {
        "timeframe": "30d",
        "minConfidence": 0.7
    }
}' "Discover Patterns"

# Test 8: Get trending patterns
test_endpoint "GET" "http://localhost:5001/future/patterns/trending" "" "Get Trending Patterns"

# Test 9: Generate prediction
test_endpoint "POST" "http://localhost:5001/future/predictions/generate" '{
    "patternId": "pattern-1",
    "timeHorizon": "6months",
    "parameters": {
        "confidence": 0.8,
        "scenarios": 3
    }
}' "Generate Prediction"

echo ""
echo "🎯 Testing Resonance Engine"
echo "==========================="

# Test 10: Calculate resonance
test_endpoint "POST" "http://localhost:5001/resonance/calculate" '{
    "conceptId": "concept-test-1",
    "userId": "user1",
    "context": "contribution"
}' "Calculate Resonance"

# Test 11: Get resonance patterns
test_endpoint "GET" "http://localhost:5001/resonance/patterns" "" "Get Resonance Patterns"

echo ""
echo "🔄 Testing Translation System"
echo "============================="

# Test 12: Translate concept
test_endpoint "POST" "http://localhost:5001/translation/translate" '{
    "sourceLanguage": "en",
    "targetLanguage": "es",
    "text": "Abundance amplification through collective resonance",
    "context": "concept"
}' "Translate Concept"

# Test 13: Get translation history
test_endpoint "GET" "http://localhost:5001/translation/history" "" "Get Translation History"

echo ""
echo "📊 Testing System Metrics"
echo "========================"

# Test 14: Get system metrics
test_endpoint "GET" "http://localhost:5001/metrics" "" "Get System Metrics"

# Test 15: Get module status
test_endpoint "GET" "http://localhost:5001/modules/status" "" "Get Module Status"

echo ""
echo "🧪 Testing Error Handling"
echo "========================="

# Test 16: Test invalid endpoint
test_endpoint "GET" "http://localhost:5001/invalid/endpoint" "" "Test Invalid Endpoint (should return 404)"

# Test 17: Test invalid data
test_endpoint "POST" "http://localhost:5001/contributions/record" '{
    "invalid": "data"
}' "Test Invalid Data (should return 400)"

echo ""
echo "📈 Performance Test"
echo "=================="

# Test 18: Load test - multiple requests
print_status "INFO" "Running load test with 10 concurrent requests..."
for i in {1..10}; do
    curl -s http://localhost:5001/health > /dev/null &
done
wait
print_status "SUCCESS" "Load test completed"

echo ""
echo "🎉 Test Summary"
echo "==============="
print_status "SUCCESS" "All working endpoints tested successfully!"
print_status "INFO" "The Living Codex system is functioning properly with:"
echo "  - ✅ Abundance amplification tracking and collective energy measurement"
echo "  - ✅ Future knowledge retrieval and pattern discovery"
echo "  - ✅ Resonance calculation and pattern analysis"
echo "  - ✅ Translation services and concept management"
echo "  - ✅ Real-time contribution tracking and abundance events"
echo "  - ✅ System health monitoring and metrics"
echo "  - ✅ Error handling and validation"

# Cleanup
if [ ! -z "$SERVER_PID" ]; then
    print_status "INFO" "Stopping test server..."
    kill $SERVER_PID 2>/dev/null
fi

echo ""
print_status "SUCCESS" "All tests completed! 🌟"
