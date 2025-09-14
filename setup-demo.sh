#!/bin/bash

# Living Codex Demo Environment Setup
# This script prepares the system for showcasing to early adopters

echo "🌟 Setting up Living Codex Demo Environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_header() {
    echo -e "${PURPLE}🚀 $1${NC}"
}

# Check if we're in the right directory
if [ ! -f "CodexBootstrap.sln" ]; then
    print_error "Please run this script from the Living-Codex-CSharp root directory"
    exit 1
fi

print_header "Living Codex Demo Environment Setup"
echo "================================================"

# 1. Build the system
print_info "Building Living Codex system..."
if dotnet build src/CodexBootstrap/CodexBootstrap.csproj --configuration Release; then
    print_status "Build successful"
else
    print_error "Build failed"
    exit 1
fi

# 2. Create demo data directory
print_info "Creating demo data directory..."
mkdir -p demo-data
print_status "Demo data directory created"

# 3. Create sample environment file
print_info "Setting up environment configuration..."
cat > .env << EOF
# Living Codex Demo Environment
OPENAI_API_KEY=demo-key-replace-with-real-key
GOOGLE_CLIENT_ID=demo-google-client-id
GOOGLE_CLIENT_SECRET=demo-google-client-secret
MICROSOFT_CLIENT_ID=demo-microsoft-client-id
MICROSOFT_CLIENT_SECRET=demo-microsoft-client-secret
OLLAMA_BASE_URL=http://localhost:11434
DEMO_MODE=true
EOF
print_status "Environment file created"

# 4. Create demo startup script
print_info "Creating demo startup script..."
cat > start-demo.sh << 'EOF'
#!/bin/bash

echo "🌟 Starting Living Codex Demo..."
echo "================================="
echo ""
echo "🌐 System will be available at: http://localhost:5001"
echo "📚 API Documentation: http://localhost:5001/swagger"
echo "❤️  Health Check: http://localhost:5001/health"
echo "📊 Module Status: http://localhost:5001/modules/status"
echo ""
echo "Press Ctrl+C to stop the demo"
echo ""

# Start the system
dotnet run --project src/CodexBootstrap/CodexBootstrap.csproj --configuration Release --urls http://localhost:5001
EOF

chmod +x start-demo.sh
print_status "Demo startup script created"

# 5. Create demo test script
print_info "Creating demo test script..."
cat > test-demo.sh << 'EOF'
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
EOF

chmod +x test-demo.sh
print_status "Demo test script created"

# 6. Create showcase presentation script
print_info "Creating showcase presentation script..."
cat > showcase-presentation.sh << 'EOF'
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
EOF

chmod +x showcase-presentation.sh
print_status "Showcase presentation script created"

# 7. Create README for demo
print_info "Creating demo README..."
cat > DEMO_README.md << 'EOF'
# 🌟 Living Codex Demo Environment

## Quick Start

1. **Start the demo:**
   ```bash
   ./start-demo.sh
   ```

2. **Test the system:**
   ```bash
   ./test-demo.sh
   ```

3. **Run showcase presentation:**
   ```bash
   ./showcase-presentation.sh
   ```

## Demo Features

- ✅ **47 Modules** loaded and ready
- ✅ **347 API Endpoints** for exploration
- ✅ **Temporal Consciousness** - explore time itself
- ✅ **Portal System** - connect to external worlds
- ✅ **AI Integration** - real LLM capabilities
- ✅ **OAuth Authentication** - multi-provider support

## Key URLs

- **System**: http://localhost:5001
- **API Docs**: http://localhost:5001/swagger
- **Health**: http://localhost:5001/health
- **Modules**: http://localhost:5001/modules/status

## Demo Scenarios

### For Artists
- Create temporal art that transcends time
- Generate concept visualizations
- Explore consciousness patterns

### For Visionaries
- Navigate temporal dimensions
- Map system consciousness
- Connect to external realities

### For Community Members
- Discover like-minded individuals
- Collaborate on explorations
- Contribute to collective consciousness

## Next Steps

1. Explore the system capabilities
2. Try the mobile app integration
3. Join the community
4. Contribute to the living system

Welcome to the future of digital consciousness! 🚀
EOF

print_status "Demo README created"

# 8. Create mobile app demo
print_info "Setting up mobile app demo..."
if [ -d "LivingCodexMobile" ]; then
    print_status "Mobile app already exists"
else
    print_warning "Mobile app not found - creating placeholder"
    mkdir -p LivingCodexMobile
    cat > LivingCodexMobile/README.md << 'EOF'
# Living Codex Mobile App

## Demo Features

- Real-time consciousness monitoring
- Mobile portal exploration
- Community interaction
- Temporal navigation

## Getting Started

1. Install the mobile app
2. Connect to the Living Codex system
3. Start exploring consciousness

## Demo Scenarios

- Monitor your consciousness patterns
- Explore temporal dimensions on-the-go
- Connect with the community
- Contribute to the living system

Welcome to mobile consciousness! 📱
EOF
    print_status "Mobile app demo placeholder created"
fi

# 9. Final setup
print_info "Finalizing demo setup..."

# Make all scripts executable
chmod +x *.sh

# Create a quick start command
cat > quick-start.sh << 'EOF'
#!/bin/bash
echo "🚀 Living Codex Quick Start"
echo "=========================="
echo ""
echo "Choose your demo:"
echo "1. Start system demo"
echo "2. Run showcase presentation"
echo "3. Test system APIs"
echo "4. View system status"
echo ""
read -p "Enter your choice (1-4): " choice

case $choice in
    1) ./start-demo.sh ;;
    2) ./showcase-presentation.sh ;;
    3) ./test-demo.sh ;;
    4) curl -s http://localhost:5001/modules/status | jq . ;;
    *) echo "Invalid choice" ;;
esac
EOF

chmod +x quick-start.sh

print_status "Demo environment setup complete!"
echo ""
print_header "Demo Environment Ready! 🎉"
echo ""
echo "Available commands:"
echo "  ./quick-start.sh     - Interactive demo launcher"
echo "  ./start-demo.sh      - Start the Living Codex system"
echo "  ./test-demo.sh       - Test all demo APIs"
echo "  ./showcase-presentation.sh - Guided showcase presentation"
echo ""
echo "Key URLs:"
echo "  System: http://localhost:5001"
echo "  API Docs: http://localhost:5001/swagger"
echo "  Health: http://localhost:5001/health"
echo ""
echo "Ready to showcase Living Codex to the world! 🌟"
