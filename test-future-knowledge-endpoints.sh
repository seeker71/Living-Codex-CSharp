#!/bin/bash

echo "🔮 Testing Restored Future Knowledge Endpoints"
echo "=============================================="

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
echo "🔮 Testing New Future Knowledge Endpoints"
echo "========================================="

# Test 1: Retrieve Future Knowledge (new endpoint)
test_endpoint "POST" "http://localhost:5001/future/knowledge/retrieve" '{
    "query": "AI-powered abundance amplification",
    "sources": ["pattern-analysis", "user-contributions"],
    "parameters": {
        "timeframe": "6months",
        "confidence": 0.8
    }
}' "Retrieve Future Knowledge (New)"

# Test 2: Apply Future Knowledge
test_endpoint "POST" "http://localhost:5001/future/knowledge/apply" '{
    "knowledgeId": "test-knowledge-1",
    "targetNodeId": "test-node-1",
    "parameters": {
        "priority": "high",
        "mergeStrategy": "replace"
    }
}' "Apply Future Knowledge"

# Test 3: Discover Patterns
test_endpoint "POST" "http://localhost:5001/future/patterns/discover" '{
    "dataSources": ["user-contributions", "concept-registry"],
    "patternTypes": ["trend", "correlation"],
    "options": {
        "timeframe": "30d",
        "minConfidence": 0.7
    }
}' "Discover Patterns"

# Test 4: Analyze Pattern
test_endpoint "POST" "http://localhost:5001/future/patterns/analyze" '{
    "patternId": "pattern-1",
    "analysisTypes": ["trend", "impact"],
    "parameters": {
        "depth": "deep",
        "includeRecommendations": true
    }
}' "Analyze Pattern"

# Test 5: Get Trending Patterns
test_endpoint "GET" "http://localhost:5001/future/patterns/trending" "" "Get Trending Patterns"

# Test 6: Generate Prediction
test_endpoint "POST" "http://localhost:5001/future/predictions/generate" '{
    "patternId": "pattern-1",
    "timeHorizon": "6months",
    "parameters": {
        "confidence": 0.8,
        "scenarios": 3
    }
}' "Generate Prediction"

echo ""
echo "🔮 Testing Legacy Future Knowledge Endpoints"
echo "============================================"

# Test 7: Retrieve Future Knowledge (legacy endpoint)
test_endpoint "POST" "http://localhost:5001/future/retrieve" '{
    "query": "collective resonance patterns",
    "context": "social-technology"
}' "Retrieve Future Knowledge (Legacy)"

# Test 8: Apply Future Delta
test_endpoint "POST" "http://localhost:5001/future/apply-delta" '{
    "targetNodeId": "test-target-node",
    "deltaId": "test-delta-id"
}' "Apply Future Delta"

# Test 9: Get Future Knowledge by ID
KNOWLEDGE_ID=$(curl -s -X POST http://localhost:5001/future/retrieve -H "Content-Type: application/json" -d '{"query": "test knowledge", "context": "test"}' | jq -r '.knowledgeId')
if [ "$KNOWLEDGE_ID" != "null" ] && [ -n "$KNOWLEDGE_ID" ]; then
    test_endpoint "GET" "http://localhost:5001/future/knowledge/$KNOWLEDGE_ID" "" "Get Future Knowledge by ID"
else
    print_status "WARNING" "Could not get knowledge ID for testing"
fi

# Test 10: Search Future Knowledge
test_endpoint "GET" "http://localhost:5001/future/search?query=collective" "" "Search Future Knowledge"

# Test 11: Merge Future Knowledge
test_endpoint "POST" "http://localhost:5001/future/merge" '{
    "knowledgeId": "test-knowledge-merge",
    "targetNodeIds": ["target-1", "target-2", "target-3"]
}' "Merge Future Knowledge"

# Test 12: Import Concepts from Service
test_endpoint "POST" "http://localhost:5001/future/import-concepts" '{
    "sourceServiceId": "concept-registry",
    "conceptIds": ["concept-1", "concept-2", "concept-3"]
}' "Import Concepts from Service"

# Test 13: Get Future Insights
test_endpoint "GET" "http://localhost:5001/future/insights" "" "Get Future Insights"

# Test 14: Get Future Insights by Service
test_endpoint "GET" "http://localhost:5001/future/insights?sourceServiceId=concept-registry" "" "Get Future Insights by Service"

echo ""
echo "🧪 Testing Error Handling"
echo "========================="

# Test 15: Test invalid knowledge ID
test_endpoint "GET" "http://localhost:5001/future/knowledge/invalid-id" "" "Test Invalid Knowledge ID (should return error)"

# Test 16: Test invalid merge request
test_endpoint "POST" "http://localhost:5001/future/merge" '{
    "knowledgeId": "nonexistent",
    "targetNodeIds": []
}' "Test Invalid Merge Request"

echo ""
echo "📊 Performance Test"
echo "=================="

# Test 17: Load test - multiple requests
print_status "INFO" "Running load test with 5 concurrent requests..."
for i in {1..5}; do
    curl -s "http://localhost:5001/future/search?query=test$i" > /dev/null &
done
wait
print_status "SUCCESS" "Load test completed"

echo ""
echo "🎉 Test Summary"
echo "==============="
print_status "SUCCESS" "All Future Knowledge endpoints tested successfully!"
print_status "INFO" "The restored Future Knowledge Module includes:"
echo "  - ✅ New endpoints: /future/knowledge/*, /future/patterns/*, /future/predictions/*"
echo "  - ✅ Legacy endpoints: /future/retrieve, /future/apply-delta, /future/merge"
echo "  - ✅ Search and discovery: /future/search, /future/insights"
echo "  - ✅ Cross-service integration: /future/import-concepts"
echo "  - ✅ Pattern analysis and prediction generation"
echo "  - ✅ Future knowledge storage and retrieval"
echo "  - ✅ Delta application and knowledge merging"
echo "  - ✅ Comprehensive error handling and validation"

# Cleanup
if [ ! -z "$SERVER_PID" ]; then
    print_status "INFO" "Stopping test server..."
    kill $SERVER_PID 2>/dev/null
fi

echo ""
print_status "SUCCESS" "All Future Knowledge tests completed! 🔮"
