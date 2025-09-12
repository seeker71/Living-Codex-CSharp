#!/bin/bash

echo "🧪 Testing News Feed Integration"
echo "================================"

# Test 1: Get personalized news feed
echo "📰 Test 1: Getting personalized news feed..."
curl -s -X POST http://localhost:5000/future/news/feed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user1",
    "interestAreas": ["AI", "abundance", "technology"],
    "contributionTypes": ["Create", "Innovation"],
    "investmentAreas": ["technology", "sustainability"],
    "maxItems": 5
  }' | python3 -m json.tool

echo -e "\n"

# Test 2: Get news concepts
echo "🔍 Test 2: Getting news concepts..."
curl -s http://localhost:5000/future/news/concepts | python3 -m json.tool

echo -e "\n"

# Test 3: Ingest a new news item
echo "📥 Test 3: Ingesting a new news item..."
curl -s -X POST http://localhost:5000/future/news/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "id": "news-test-1",
    "title": "Breakthrough in Quantum AI for Abundance Systems",
    "content": "Researchers have developed a new quantum AI system that can process abundance amplification algorithms 1000x faster than classical computers...",
    "source": "QuantumNews",
    "url": "https://example.com/quantum-ai-abundance",
    "publishedAt": "2025-09-12T04:00:00Z",
    "tags": ["quantum", "AI", "abundance", "breakthrough", "computing"],
    "metadata": {
      "category": "technology",
      "sentiment": "positive",
      "impact": "high"
    }
  }' | python3 -m json.tool

echo -e "\n"

# Test 4: Analyze the ingested news item
echo "🔬 Test 4: Analyzing the ingested news item..."
curl -s "http://localhost:5000/future/news/analyze/news-test-1?userId=user1&interestAreas=AI,quantum&contributionTypes=Create&investmentAreas=technology" | python3 -m json.tool

echo -e "\n"
echo "✅ News feed testing completed!"
