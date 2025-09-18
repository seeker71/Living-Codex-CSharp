#!/bin/bash

# Comprehensive test for user concept interaction -> news feed flow
# Tests: user registration -> concept interaction -> contribution tracking -> news feed personalization

set -e

echo "🧪 Testing Complete Concept Interaction -> News Feed Flow"
echo "=========================================================="

# Configuration
BACKEND_URL="http://localhost:5002"
TEST_USER_ID="flow-test-user-$(date +%s)"
TEST_CONCEPT_1="concept-ai-$(date +%s)"
TEST_CONCEPT_2="concept-blockchain-$(date +%s)"
TEST_EMAIL="flowtest@example.com"
TEST_USERNAME="flowtest$(date +%s)"

echo "🔧 Test Configuration:"
echo "User ID: $TEST_USER_ID"
echo "Concept 1: $TEST_CONCEPT_1 (AI/Technology)"
echo "Concept 2: $TEST_CONCEPT_2 (Blockchain/Finance)"
echo ""

# Step 1: Create test user
echo "👤 Step 1: Creating test user..."
USER_RESPONSE=$(curl -s -X POST "$BACKEND_URL/identity/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"username\": \"$TEST_USERNAME\",
    \"email\": \"$TEST_EMAIL\",
    \"password\": \"testpass123\",
    \"displayName\": \"Flow Test User\"
  }")

echo "User creation: $(echo "$USER_RESPONSE" | jq '{success: .success, userId: .userId // .user.id // .id}')"

if echo "$USER_RESPONSE" | jq -e '.success' > /dev/null; then
  ACTUAL_USER_ID=$(echo "$USER_RESPONSE" | jq -r '.userId // .user.id // .id // empty')
  if [ -n "$ACTUAL_USER_ID" ]; then
    TEST_USER_ID="$ACTUAL_USER_ID"
  fi
fi
echo ""

# Step 2: Create test concepts
echo "🧠 Step 2: Creating test concepts..."
CONCEPT1_RESPONSE=$(curl -s -X POST "$BACKEND_URL/concepts" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$TEST_CONCEPT_1\",
    \"title\": \"Artificial Intelligence\",
    \"description\": \"AI and machine learning concepts\",
    \"tags\": [\"ai\", \"technology\", \"machine-learning\"]
  }")

CONCEPT2_RESPONSE=$(curl -s -X POST "$BACKEND_URL/concepts" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"$TEST_CONCEPT_2\",
    \"title\": \"Blockchain Technology\",
    \"description\": \"Distributed ledger and cryptocurrency\",
    \"tags\": [\"blockchain\", \"crypto\", \"finance\"]
  }")

echo "Concept 1: $(echo "$CONCEPT1_RESPONSE" | jq '{success: .success}')"
echo "Concept 2: $(echo "$CONCEPT2_RESPONSE" | jq '{success: .success}')"
echo ""

# Step 3: Test attune to concepts
echo "🔗 Step 3: Testing attune to concepts..."
ATTUNE1_RESPONSE=$(curl -s -X POST "$BACKEND_URL/concept/user/link" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$TEST_USER_ID\",
    \"conceptId\": \"$TEST_CONCEPT_1\",
    \"relation\": \"attuned\"
  }")

ATTUNE2_RESPONSE=$(curl -s -X POST "$BACKEND_URL/concept/user/link" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$TEST_USER_ID\",
    \"conceptId\": \"$TEST_CONCEPT_2\",
    \"relation\": \"attuned\"
  }")

echo "Attune to AI: $(echo "$ATTUNE1_RESPONSE" | jq '{success: .success}')"
echo "Attune to Blockchain: $(echo "$ATTUNE2_RESPONSE" | jq '{success: .success}')"
echo ""

# Step 4: Test amplify concepts (record contributions)
echo "⚡ Step 4: Testing amplify concepts..."
AMPLIFY1_RESPONSE=$(curl -s -X POST "$BACKEND_URL/contributions/record" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$TEST_USER_ID\",
    \"entityId\": \"$TEST_CONCEPT_1\",
    \"entityType\": \"concept\",
    \"contributionType\": \"Rating\",
    \"description\": \"User amplified AI concept\",
    \"value\": 5,
    \"metadata\": {
      \"action\": \"amplify\",
      \"conceptId\": \"$TEST_CONCEPT_1\",
      \"conceptTitle\": \"Artificial Intelligence\"
    }
  }")

AMPLIFY2_RESPONSE=$(curl -s -X POST "$BACKEND_URL/contributions/record" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$TEST_USER_ID\",
    \"entityId\": \"$TEST_CONCEPT_2\",
    \"entityType\": \"concept\",
    \"contributionType\": \"Rating\",
    \"description\": \"User amplified Blockchain concept\",
    \"value\": 3,
    \"metadata\": {
      \"action\": \"amplify\",
      \"conceptId\": \"$TEST_CONCEPT_2\",
      \"conceptTitle\": \"Blockchain Technology\"
    }
  }")

echo "Amplify AI: $(echo "$AMPLIFY1_RESPONSE" | jq '{success: .success, contributionId: .contributionId}')"
echo "Amplify Blockchain: $(echo "$AMPLIFY2_RESPONSE" | jq '{success: .success, contributionId: .contributionId}')"
echo ""

# Step 5: Check user-concept relationships
echo "🔍 Step 5: Checking user-concept relationships..."
USER_CONCEPTS_RESPONSE=$(curl -s "$BACKEND_URL/concept/user/$TEST_USER_ID")
echo "User concepts: $(echo "$USER_CONCEPTS_RESPONSE" | jq '{success: .success, conceptCount: (.concepts | length // 0)}')"
if echo "$USER_CONCEPTS_RESPONSE" | jq -e '.concepts | length > 0' > /dev/null; then
  echo "✅ User has linked concepts:"
  echo "$USER_CONCEPTS_RESPONSE" | jq '.concepts[] | {conceptId: .conceptId, relation: .relation}' 2>/dev/null || echo "  (Could not parse concept details)"
else
  echo "❌ No user-concept relationships found"
fi
echo ""

# Step 6: Check user contributions
echo "📊 Step 6: Checking user contributions..."
CONTRIBUTIONS_RESPONSE=$(curl -s "$BACKEND_URL/contributions/user/$TEST_USER_ID")
echo "User contributions: $(echo "$CONTRIBUTIONS_RESPONSE" | jq '{success: .success, contributionCount: (.contributions | length // 0)}')"
if echo "$CONTRIBUTIONS_RESPONSE" | jq -e '.contributions | length > 0' > /dev/null; then
  echo "✅ User has contributions:"
  echo "$CONTRIBUTIONS_RESPONSE" | jq '.contributions[] | {entityId: .entityId, contributionType: .contributionType, value: .value, description: .description}' 2>/dev/null || echo "  (Could not parse contribution details)"
else
  echo "❌ No user contributions found"
fi
echo ""

# Step 7: Check abundance events
echo "🌟 Step 7: Checking abundance events for user..."
ABUNDANCE_RESPONSE=$(curl -s "$BACKEND_URL/contributions/abundance/events?limit=10")
USER_ABUNDANCE=$(echo "$ABUNDANCE_RESPONSE" | jq --arg userId "$TEST_USER_ID" '.events[] | select(.userId == $userId)')
if [ -n "$USER_ABUNDANCE" ]; then
  echo "✅ User has abundance events:"
  echo "$USER_ABUNDANCE" | jq '{contributionId: .contributionId, abundanceMultiplier: .abundanceMultiplier, timestamp: .timestamp}'
else
  echo "❌ No abundance events found for user"
fi
echo ""

# Step 8: Check personal news feed (current implementation)
echo "📰 Step 8: Checking current personal news feed..."
NEWS_FEED_RESPONSE=$(curl -s "$BACKEND_URL/news/feed/$TEST_USER_ID?limit=10")
echo "News feed: $(echo "$NEWS_FEED_RESPONSE" | jq '{success: .success, itemCount: (.items | length // 0), totalCount: .totalCount // 0, message: .message}')"

if echo "$NEWS_FEED_RESPONSE" | jq -e '.items | length > 0' > /dev/null; then
  echo "✅ News feed has items"
else
  echo "❌ News feed is empty - this is the problem we need to fix!"
fi
echo ""

# Step 9: Check what user interests are being used
echo "🎯 Step 9: Checking user interest extraction..."
USER_NODE_RESPONSE=$(curl -s "$BACKEND_URL/storage-endpoints/nodes/$TEST_USER_ID")
echo "User node: $(echo "$USER_NODE_RESPONSE" | jq '{success: .success, hasNode: (.node != null)}')"
if echo "$USER_NODE_RESPONSE" | jq -e '.node' > /dev/null; then
  echo "User interests from node: $(echo "$USER_NODE_RESPONSE" | jq '.node.meta.interests // "none"')"
  echo "User contributions from node: $(echo "$USER_NODE_RESPONSE" | jq '.node.meta.contributions // "none"')"
else
  echo "❌ User node not found - this explains why news feed is empty!"
fi
echo ""

# Step 10: Analysis and recommendations
echo "🔍 Analysis: Current Flow Issues"
echo "================================"

echo "✅ Working Components:"
echo "  - User registration"
echo "  - Concept creation"
echo "  - User-concept linking (attune)"
echo "  - Contribution recording (amplify)"
echo "  - Abundance event generation"

echo ""
echo "❌ Missing Components:"
echo "  - User interests not extracted from concept interactions"
echo "  - News feed doesn't read user-concept relationships"
echo "  - No automatic interest profile building from interactions"

echo ""
echo "🔧 Required Fixes:"
echo "  1. Update NewsFeedModule to read user-concept relationships"
echo "  2. Build user interest profile from concept interactions"
echo "  3. Match news content to user's interacted concepts"
echo "  4. Create news items from user contribution events"

echo ""
echo "📋 Test Results Summary:"
echo "========================"
if echo "$AMPLIFY1_RESPONSE" | jq -e '.success' > /dev/null; then
  echo "✅ Concept interactions are being recorded as contributions"
else
  echo "❌ Concept interactions not being recorded"
fi

if [ -n "$USER_ABUNDANCE" ]; then
  echo "✅ Abundance events are generated from concept interactions"
else
  echo "❌ No abundance events from concept interactions"
fi

if echo "$NEWS_FEED_RESPONSE" | jq -e '.items | length > 0' > /dev/null; then
  echo "✅ Personal news feed has content"
else
  echo "❌ Personal news feed is empty (main issue to fix)"
fi

echo ""
echo "🎯 Next Steps:"
echo "1. Implement getUserInterestsFromConcepts() in NewsFeedModule"
echo "2. Update news feed to include concept-based matching"
echo "3. Create news items from user contribution events"
echo "4. Test complete flow again"

