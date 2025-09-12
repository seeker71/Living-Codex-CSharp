#!/bin/bash

echo "🌟 Testing Real-Time Fractal News Streaming System"
echo "=================================================="

# Test 1: Check system health
echo "🔍 Test 1: Checking system health..."
curl -s http://localhost:5000/health | python3 -m json.tool

echo -e "\n"

# Test 2: Subscribe to news stream
echo "📡 Test 2: Subscribing to news stream..."
SUBSCRIPTION_RESPONSE=$(curl -s -X POST http://localhost:5000/news/stream/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user1",
    "interestAreas": ["AI", "abundance", "technology"],
    "contributionTypes": ["Create", "Innovation"],
    "investmentAreas": ["technology", "sustainability"],
    "beliefSystemFilters": ["abundance", "amplification", "collective"]
  }')

echo "$SUBSCRIPTION_RESPONSE" | python3 -m json.tool
SUBSCRIPTION_ID=$(echo "$SUBSCRIPTION_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('subscriptionId', ''))")

echo -e "\n"

# Test 3: Ingest news items into the stream
echo "📥 Test 3: Ingesting news items into the stream..."

# Ingest multiple news items
curl -s -X POST http://localhost:5000/news/stream/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "id": "news-stream-1",
    "title": "AI Breakthrough Enables 10x Abundance Amplification",
    "content": "Scientists have developed a revolutionary AI algorithm that can amplify individual contributions by up to 10x through advanced collective resonance analysis...",
    "source": "TechNews",
    "url": "https://example.com/ai-abundance-breakthrough",
    "publishedAt": "2025-09-12T04:00:00Z",
    "tags": ["AI", "abundance", "amplification", "breakthrough", "technology"],
    "metadata": {
      "category": "technology",
      "sentiment": "positive",
      "impact": "high"
    }
  }' | python3 -m json.tool

echo -e "\n"

curl -s -X POST http://localhost:5000/news/stream/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "id": "news-stream-2",
    "title": "Global Community Platform Reaches 100M Users",
    "content": "A community collaboration platform focused on abundance amplification has reached 100 million active users worldwide...",
    "source": "CommunityNews",
    "url": "https://example.com/community-platform-growth",
    "publishedAt": "2025-09-12T04:05:00Z",
    "tags": ["community", "collaboration", "platform", "growth", "global"],
    "metadata": {
      "category": "social",
      "sentiment": "positive",
      "impact": "high"
    }
  }' | python3 -m json.tool

echo -e "\n"

curl -s -X POST http://localhost:5000/news/stream/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "id": "news-stream-3",
    "title": "Quantum Computing Breakthrough for Collective Intelligence",
    "content": "Researchers have made a breakthrough in quantum computing that could revolutionize collective intelligence and abundance calculation...",
    "source": "ScienceDaily",
    "url": "https://example.com/quantum-collective-intelligence",
    "publishedAt": "2025-09-12T04:10:00Z",
    "tags": ["quantum", "computing", "collective", "intelligence", "breakthrough"],
    "metadata": {
      "category": "science",
      "sentiment": "positive",
      "impact": "high"
    }
  }' | python3 -m json.tool

echo -e "\n"

# Test 4: Get user news feed (fractal analysis)
echo "📰 Test 4: Getting user news feed with fractal analysis..."
curl -s "http://localhost:5000/news/stream/feed/user1?maxItems=10" | python3 -m json.tool

echo -e "\n"

# Test 5: Get news sources
echo "🔍 Test 5: Getting available news sources..."
curl -s http://localhost:5000/news/stream/sources | python3 -m json.tool

echo -e "\n"

# Test 6: Get fractal analysis of specific news item
echo "🔬 Test 6: Getting fractal analysis of specific news item..."
curl -s http://localhost:5000/news/stream/fractal/fractal-news-stream-1 | python3 -m json.tool

echo -e "\n"

# Test 7: Simulate real-time feed polling
echo "⏰ Test 7: Simulating real-time feed polling (3 iterations)..."
for i in {1..3}; do
  echo "--- Poll $i ---"
  curl -s "http://localhost:5000/news/stream/feed/user1?maxItems=5" | python3 -c "
import sys, json
data = json.load(sys.stdin)
if data.get('success'):
  print(f'Found {len(data[\"newsItems\"])} news items')
  for item in data['newsItems'][:2]:  # Show first 2 items
    print(f'  - {item[\"headline\"]}')
    print(f'    Impact: {item[\"impactAreas\"]}')
    print(f'    Amplification: {item[\"amplificationFactors\"]}')
    print()
else:
  print('No news items found')
"
  sleep 2
done

echo -e "\n"

# Test 8: Unsubscribe from stream
if [ ! -z "$SUBSCRIPTION_ID" ]; then
  echo "🚫 Test 8: Unsubscribing from news stream..."
  curl -s -X DELETE "http://localhost:5000/news/stream/unsubscribe/$SUBSCRIPTION_ID" | python3 -m json.tool
else
  echo "⚠️  Test 8: Skipped (no subscription ID available)"
fi

echo -e "\n"
echo "✅ Real-time news streaming testing completed!"
echo ""
echo "🌟 Key Features Demonstrated:"
echo "  - Real-time news ingestion and processing"
echo "  - Fractal analysis with belief system translation"
echo "  - Pub/sub architecture with user subscriptions"
echo "  - Concept extraction and abundance amplification"
echo "  - Personalized news feeds based on user interests"
echo "  - Resonance calculation and collective impact analysis"
