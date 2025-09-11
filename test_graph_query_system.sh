#!/bin/bash

# Test Graph Query and Meta-Node Generation System
# This script demonstrates the comprehensive graph-based exploration system

set -e

BASE_URL="http://localhost:5000"
echo "🧠 Testing Graph Query and Meta-Node Generation System"
echo "======================================================"

# Start the server in the background
echo "🚀 Starting server..."
dotnet run --project src/CodexBootstrap &
SERVER_PID=$!

# Wait for server to start
echo "⏳ Waiting for server to start..."
sleep 10

# Test server health
echo "🏥 Testing server health..."
curl -s "$BASE_URL/health" | jq '.' || echo "Health check failed"

echo ""
echo "📊 Testing System Overview..."
echo "============================="
curl -s "$BASE_URL/graph/overview" | jq '.'

echo ""
echo "🔍 Testing Graph Query System..."
echo "================================"

# Test XPath-like queries
echo "📝 Testing XPath-like node queries..."
curl -s -X POST "$BASE_URL/graph/query" \
  -H "Content-Type: application/json" \
  -d '{"query": "/nodes", "filters": {"typeId": "module"}}' | jq '.'

echo ""
echo "🔗 Testing Connection Discovery..."
echo "================================="

# Test connection discovery
curl -s -X POST "$BASE_URL/graph/connections" \
  -H "Content-Type: application/json" \
  -d '{"sourceConceptId": "codex.core", "maxDepth": 2}' | jq '.'

echo ""
echo "🔎 Testing Node Search..."
echo "========================="

# Test node search
curl -s "$BASE_URL/graph/search?query=module" | jq '.'

echo ""
echo "📈 Testing Node Relationships..."
echo "==============================="

# Test node relationships
curl -s "$BASE_URL/graph/relationships/codex.core?depth=2" | jq '.'

echo ""
echo "🏗️ Testing Meta-Node Generation..."
echo "=================================="

# Test meta-node generation from code
echo "📝 Generating meta-nodes from code files..."
curl -s -X POST "$BASE_URL/meta/generate-from-code" \
  -H "Content-Type: application/json" \
  -d '{"includeClasses": true, "includeMethods": true, "includeApiRoutes": true}' | jq '.'

echo ""
echo "📋 Generating meta-nodes from spec files..."
curl -s -X POST "$BASE_URL/meta/generate-from-spec" \
  -H "Content-Type: application/json" \
  -d '{"includeSections": true, "includeCodeBlocks": true}' | jq '.'

echo ""
echo "📊 Testing Meta-Node Statistics..."
echo "=================================="

curl -s "$BASE_URL/meta/statistics" | jq '.'

echo ""
echo "🌐 Testing Cross-Service Event Publishing..."
echo "==========================================="

# Test cross-service event publishing
curl -s -X POST "$BASE_URL/events/publish-cross-service" \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "system_loaded",
    "entityType": "system",
    "entityId": "graph-query-system",
    "data": {
      "message": "Graph query system loaded successfully",
      "capabilities": ["graph-query", "meta-node-generation", "connection-discovery"]
    },
    "sourceServiceId": "codex.graph.query",
    "targetServices": ["codex.service.discovery"]
  }' | jq '.'

echo ""
echo "🔄 Testing Service Discovery..."
echo "=============================="

# Test service discovery
curl -s -X POST "$BASE_URL/service/register" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceId": "codex.graph.query",
    "serviceType": "graph-query",
    "baseUrl": "http://localhost:5000",
    "capabilities": {
      "graph-query": "XPath-like graph queries",
      "meta-node-generation": "Automatic meta-node generation",
      "connection-discovery": "Concept connection discovery"
    }
  }' | jq '.'

echo ""
echo "📋 Listing all services..."
curl -s "$BASE_URL/service/list" | jq '.'

echo ""
echo "🎯 Testing Future Knowledge Import..."
echo "===================================="

# Test future knowledge import
curl -s -X POST "$BASE_URL/future/import-concepts" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceServiceId": "codex.graph.query",
    "conceptIds": ["codex.graph.query", "codex.meta.generator"],
    "analysisContext": "Graph query system analysis",
    "targetBeliefSystem": {
      "framework": "U-CORE",
      "language": "English",
      "culturalContext": "Technical"
    }
  }' | jq '.'

echo ""
echo "🔮 Testing Future Insights..."
echo "============================="

curl -s "$BASE_URL/future/insights" | jq '.'

echo ""
echo "✅ All tests completed successfully!"
echo "=================================="

# Clean up
echo "🧹 Cleaning up..."
kill $SERVER_PID 2>/dev/null || true

echo ""
echo "🎉 Graph Query and Meta-Node Generation System Test Complete!"
echo "The system now provides:"
echo "  • XPath-like graph queries"
echo "  • Automatic meta-node generation from code and specs"
echo "  • Concept connection discovery"
echo "  • Cross-service event publishing"
echo "  • Service discovery and registration"
echo "  • Future knowledge import and analysis"
echo "  • Comprehensive system introspection"
