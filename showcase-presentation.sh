#!/bin/bash

echo "🎪 Living Codex Showcase Presentation"
echo "====================================="
echo ""
echo "This script will guide you through a live demonstration"
echo "of Living Codex's key features for early adopters."
echo ""

# Function to wait for user input
wait_for_user() {
    echo ""
    read -p "Press Enter to continue to the next demo..."
    echo ""
}

# Function to run a demo command
run_demo() {
    local title="$1"
    local command="$2"
    local description="$3"
    
    echo "🎯 $title"
    echo "Description: $description"
    echo "Command: $command"
    echo ""
    echo "Running command..."
    eval "$command"
    echo ""
    wait_for_user
}

echo "Starting showcase presentation..."
echo ""

# Demo 1: System Overview
run_demo "System Overview" \
  "curl -s http://localhost:5001/health | jq ." \
  "Show system health and basic status"

# Demo 2: Module Status
run_demo "Module Status" \
  "curl -s http://localhost:5001/modules/status | jq '.totalModules, .activeModules'" \
  "Display loaded modules and their status"

# Demo 3: Temporal Consciousness
run_demo "Temporal Consciousness - Portal Creation" \
  "curl -s -X POST http://localhost:5001/temporal/portal/connect -H 'Content-Type: application/json' -d '{\"temporalType\": \"future\", \"targetMoment\": \"2025-12-31T23:59:59Z\", \"consciousnessLevel\": 0.9}' | jq ." \
  "Create a temporal portal to the future"

# Demo 4: Concept Creation
run_demo "Concept Creation" \
  "curl -s -X POST http://localhost:5001/concept/create -H 'Content-Type: application/json' -d '{\"name\": \"Digital Consciousness\", \"description\": \"The intersection of technology and awareness\", \"tags\": [\"consciousness\", \"technology\", \"digital\"]}' | jq ." \
  "Create a concept for digital consciousness"

# Demo 5: Portal System
run_demo "Portal System - External World Connection" \
  "curl -s -X POST http://localhost:5001/portal/connect -H 'Content-Type: application/json' -d '{\"portalType\": \"website\", \"targetUrl\": \"https://example.com\", \"explorationDepth\": 2}' | jq ." \
  "Connect to an external world through a portal"

echo "🎉 Showcase presentation completed!"
echo ""
echo "Next steps:"
echo "1. Explore the Swagger UI at http://localhost:5001/swagger"
echo "2. Try the mobile app integration"
echo "3. Join the community and contribute to the system"
echo ""
echo "Thank you for experiencing Living Codex! 🌟"
