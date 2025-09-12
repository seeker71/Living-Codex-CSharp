#!/bin/bash

# Living Codex Server Startup Script
# Ensures clean startup with proper port, directory, and logging

set -e  # Exit on any error

echo "🚀 Starting Living Codex Server..."

# Configuration
PORT=5001
PROJECT_DIR="src/CodexBootstrap"
LOG_DIR="logs"
LOG_FILE="$LOG_DIR/server-$(date +%Y%m%d-%H%M%S).log"

# Ensure we're in the right directory
cd "$(dirname "$0")"

echo "📁 Working directory: $(pwd)"

# Create logs directory if it doesn't exist
mkdir -p "$LOG_DIR"

# Function to stop any running dotnet processes
stop_server() {
    echo "🛑 Stopping any running dotnet processes..."
    
    # Find and kill any dotnet processes
    PIDS=$(pgrep -f "dotnet.*CodexBootstrap" || true)
    if [ ! -z "$PIDS" ]; then
        echo "Found running CodexBootstrap processes: $PIDS"
        kill -TERM $PIDS 2>/dev/null || true
        sleep 2
        
        # Force kill if still running
        PIDS=$(pgrep -f "dotnet.*CodexBootstrap" || true)
        if [ ! -z "$PIDS" ]; then
            echo "Force killing remaining processes: $PIDS"
            kill -9 $PIDS 2>/dev/null || true
        fi
    fi
    
    # Also kill any processes using our target port
    PORT_PIDS=$(lsof -ti:$PORT 2>/dev/null || true)
    if [ ! -z "$PORT_PIDS" ]; then
        echo "Found processes using port $PORT: $PORT_PIDS"
        kill -TERM $PORT_PIDS 2>/dev/null || true
        sleep 1
    fi
    
    echo "✅ Server stopped"
}

# Function to check if port is available
check_port() {
    if lsof -Pi :$PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "❌ Port $PORT is still in use"
        return 1
    else
        echo "✅ Port $PORT is available"
        return 0
    fi
}

# Function to wait for server to be ready
wait_for_server() {
    echo "⏳ Waiting for server to start..."
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -s "http://localhost:$PORT/health" >/dev/null 2>&1; then
            echo "✅ Server is ready!"
            return 0
        fi
        
        attempt=$((attempt + 1))
        echo "Attempt $attempt/$max_attempts - waiting..."
        sleep 2
    done
    
    echo "❌ Server failed to start within expected time"
    return 1
}

# Function to test server endpoints
test_server() {
    echo "🧪 Testing server endpoints..."
    
    # Test health endpoint
    echo "Testing /health..."
    if curl -s "http://localhost:$PORT/health" | jq '.registrationMetrics' >/dev/null 2>&1; then
        echo "✅ Health endpoint working"
    else
        echo "❌ Health endpoint failed"
        return 1
    fi
    
    # Test AI health endpoint
    echo "Testing /ai/health..."
    if curl -s "http://localhost:$PORT/ai/health" >/dev/null 2>&1; then
        echo "✅ AI health endpoint working"
    else
        echo "❌ AI health endpoint failed"
        return 1
    fi
    
    # Test spec endpoints
    echo "Testing /spec/modules..."
    if curl -s "http://localhost:$PORT/spec/modules" >/dev/null 2>&1; then
        echo "✅ Spec modules endpoint working"
    else
        echo "❌ Spec modules endpoint failed"
        return 1
    fi
    
    echo "✅ All endpoint tests passed!"
    return 0
}

# Main execution
main() {
    echo "=========================================="
    echo "🌟 Living Codex Server Startup"
    echo "=========================================="
    echo "Port: $PORT"
    echo "Project: $PROJECT_DIR"
    echo "Logs: $LOG_FILE"
    echo "=========================================="
    
    # Stop any running server
    stop_server
    
    # Wait a moment for cleanup
    sleep 2
    
    # Check if port is available
    if ! check_port; then
        echo "❌ Cannot start server - port $PORT is still in use"
        exit 1
    fi
    
    # Navigate to project directory
    if [ ! -d "$PROJECT_DIR" ]; then
        echo "❌ Project directory not found: $PROJECT_DIR"
        exit 1
    fi
    
    cd "$PROJECT_DIR"
    echo "📁 Changed to project directory: $(pwd)"
    
    # Build the project
    echo "🔨 Building project..."
    if ! dotnet build --configuration Release --verbosity quiet; then
        echo "❌ Build failed"
        exit 1
    fi
    echo "✅ Build successful"
    
    # Start the server with logging
    echo "🚀 Starting server on port $PORT..."
    echo "📝 Logs will be written to: $LOG_FILE"
    
    # Start server in background with full logging
    nohup dotnet run --urls "http://localhost:$PORT" --configuration Release > "../$LOG_FILE" 2>&1 &
    SERVER_PID=$!
    
    echo "🆔 Server PID: $SERVER_PID"
    echo "📝 Log file: $LOG_FILE"
    
    # Wait for server to be ready
    if wait_for_server; then
        echo "✅ Server started successfully!"
        
        # Test the server
        if test_server; then
            echo ""
            echo "🎉 Living Codex Server is running!"
            echo "🌐 URL: http://localhost:$PORT"
            echo "📊 Health: http://localhost:$PORT/health"
            echo "🤖 AI Health: http://localhost:$PORT/ai/health"
            echo "📋 Spec: http://localhost:$PORT/spec/modules"
            echo "📝 Logs: $LOG_FILE"
            echo ""
            echo "To stop the server: kill $SERVER_PID"
            echo "To view logs: tail -f $LOG_FILE"
            echo ""
            
            # Show recent logs
            echo "📋 Recent server output:"
            echo "----------------------------------------"
            tail -n 20 "../$LOG_FILE" 2>/dev/null || echo "No logs yet"
            echo "----------------------------------------"
        else
            echo "❌ Server tests failed"
            kill $SERVER_PID 2>/dev/null || true
            exit 1
        fi
    else
        echo "❌ Server failed to start"
        kill $SERVER_PID 2>/dev/null || true
        echo "📋 Last 20 lines of log:"
        tail -n 20 "../$LOG_FILE" 2>/dev/null || echo "No logs available"
        exit 1
    fi
}

# Run main function
main "$@"
