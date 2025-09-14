#!/bin/bash

echo "🧪 Testing Living Codex Demo APIs..."
echo "===================================="

BASE_URL="http://localhost:5001"

# Test health endpoint
echo "Testing health endpoint..."
if curl -s "$BASE_URL/health" > /dev/null; then
    echo "✅ Health check passed"
else
    echo "❌ Health check failed"
    exit 1
fi

# Test module status
echo "Testing module status..."
if curl -s "$BASE_URL/modules/status" > /dev/null; then
    echo "✅ Module status check passed"
else
    echo "❌ Module status check failed"
fi

# Test temporal consciousness
echo "Testing temporal consciousness..."
if curl -s -X POST "$BASE_URL/temporal/portal/connect" \
  -H "Content-Type: application/json" \
  -d '{"temporalType": "present", "targetMoment": "2025-01-01T00:00:00Z", "consciousnessLevel": 0.8}' > /dev/null; then
    echo "✅ Temporal consciousness test passed"
else
    echo "❌ Temporal consciousness test failed"
fi

# Test concept creation
echo "Testing concept creation..."
if curl -s -X POST "$BASE_URL/concept/create" \
  -H "Content-Type: application/json" \
  -d '{"name": "Demo Concept", "description": "A concept for demonstration", "tags": ["demo", "test"]}' > /dev/null; then
    echo "✅ Concept creation test passed"
else
    echo "❌ Concept creation test failed"
fi

echo ""
echo "🎉 Demo tests completed!"
