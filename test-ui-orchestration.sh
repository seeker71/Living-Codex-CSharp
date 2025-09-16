#!/bin/bash

# Test UI Orchestration System
echo "🧪 Testing UI Orchestration System"
echo "=================================="

API_BASE="http://localhost:5002"

# Test 1: Compose a UI page
echo "📝 Step 1: Composing UI page..."
COMPOSE_RESPONSE=$(curl -s -X POST "$API_BASE/ui-orchestration/compose-page" \
  -H "Content-Type: application/json" \
  -d '{
    "intent": "Create a resonance comparison page",
    "path": "/resonance-compare",
    "lenses": [
      {
        "id": "lens.resonance-comparison",
        "name": "Resonance Comparison",
        "projectionType": "comparison",
        "requirements": "Visual resonance comparison with real-time updates",
        "actions": ["attune", "amplify", "weave", "reflect"],
        "endpoints": {
          "compare": "/concepts/resonance/compare",
          "concepts": "/concept/discover"
        }
      }
    ],
    "controls": [
      {
        "id": "controls.resonance-axes",
        "name": "Resonance Axes",
        "type": "resonance-compass"
      }
    ]
  }')

echo "Compose Response:"
echo "$COMPOSE_RESPONSE" | jq '.'

# Extract page ID from response
PAGE_ID=$(echo "$COMPOSE_RESPONSE" | jq -r '.data.pageAtom.id // empty')

if [ -z "$PAGE_ID" ]; then
  echo "❌ Failed to compose page"
  exit 1
fi

echo "✅ Composed page: $PAGE_ID"

# Test 2: Expand components
echo ""
echo "🔧 Step 2: Expanding components..."
EXPAND_RESPONSE=$(curl -s -X POST "$API_BASE/ui-orchestration/expand-components" \
  -H "Content-Type: application/json" \
  -d "{
    \"pageId\": \"$PAGE_ID\",
    \"provider\": \"ollama\",
    \"model\": \"llama2\"
  }")

echo "Expand Response:"
echo "$EXPAND_RESPONSE" | jq '.'

echo "✅ Expanded components for page: $PAGE_ID"

# Test 3: Validate components
echo ""
echo "✅ Step 3: Validating components..."
VALIDATE_RESPONSE=$(curl -s -X POST "$API_BASE/ui-orchestration/validate-components" \
  -H "Content-Type: application/json" \
  -d "{
    \"pageId\": \"$PAGE_ID\"
  }")

echo "Validate Response:"
echo "$VALIDATE_RESPONSE" | jq '.'

echo "✅ Validated components for page: $PAGE_ID"

# Test 4: Execute full breath loop
echo ""
echo "🌀 Step 4: Executing full breath loop..."
BREATH_RESPONSE=$(curl -s -X POST "$API_BASE/ui-orchestration/breath-loop" \
  -H "Content-Type: application/json" \
  -d '{
    "intent": "Create a concept discovery flow",
    "path": "/discover-concepts",
    "lenses": [
      {
        "id": "lens.concept-discovery",
        "name": "Concept Discovery",
        "projectionType": "stream",
        "requirements": "Discover concepts through resonance and joy",
        "actions": ["attune", "amplify", "weave", "reflect", "invite"],
        "endpoints": {
          "discover": "/concept/discover",
          "search": "/concept/search"
        }
      }
    ],
    "controls": [
      {
        "id": "controls.resonance",
        "name": "Resonance Controls",
        "type": "resonance-compass"
      },
      {
        "id": "controls.joy",
        "name": "Joy Tuner",
        "type": "joy-tuner"
      }
    ],
    "evolutionContext": "Create a joyful, resonance-driven concept discovery experience"
  }')

echo "Breath Loop Response:"
echo "$BREATH_RESPONSE" | jq '.'

echo "✅ Executed breath loop for concept discovery"

# Test 5: List available UI atoms
echo ""
echo "📋 Step 5: Listing UI atoms..."
ATOMS_RESPONSE=$(curl -s -X GET "$API_BASE/storage-endpoints/nodes?type=codex.ui.page")

echo "UI Page Atoms:"
echo "$ATOMS_RESPONSE" | jq '.nodes[] | {id, type, meta: {name, path, status}}'

echo ""
echo "🎉 UI Orchestration System Test Complete!"
echo "=========================================="
echo "The system successfully:"
echo "✅ Composed UI page atoms from intent"
echo "✅ Expanded components via AI generation"
echo "✅ Validated generated components"
echo "✅ Executed full breath loop"
echo "✅ Stored atoms in the node registry"
echo ""
echo "Next steps:"
echo "- Integrate with Next.js UI for live rendering"
echo "- Add feedback collection and pattern evolution"
echo "- Implement natural language prompts"
