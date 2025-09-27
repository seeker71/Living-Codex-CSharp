#!/bin/bash

# Living Codex Server Startup Script
# Ensures clean startup with proper port, directory, and logging

set -e  # Exit on any error

echo "🚀 Starting Living Codex Server..."

export DISABLE_AI="${DISABLE_AI:-true}"
export NEWS_INGESTION_ENABLED="${NEWS_INGESTION_ENABLED:-false}"
export USE_OLLAMA_ONLY="${USE_OLLAMA_ONLY:-false}"

# Cheap AI defaults for news concept extraction (can be overridden)
export NEWS_AI_PROVIDER="${NEWS_AI_PROVIDER:-ollama}"
export NEWS_AI_MODEL="${NEWS_AI_MODEL:-llama3.2:3b}"

# Get the script directory and set up paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/CodexBootstrap"
LOG_DIR="$SCRIPT_DIR/logs"
LOG_FILE="$LOG_DIR/server-$(date +%Y%m%d-%H%M%S).log"

# Configuration - will be determined by PortConfigurationService
PORT=5002  # Default fallback port

# Function to find available port
find_available_port() {
    local start_port=$1
    local max_port=$((start_port + 100))
    
    for ((port=start_port; port<=max_port; port++)); do
        if ! lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
            echo $port
            return 0
        fi
    done
    
    echo "❌ No available ports found in range $start_port-$max_port"
    return 1
}

# Ensure we're in the script directory
cd "$SCRIPT_DIR"

echo "📁 Working directory: $(pwd)"

# Create logs directory if it doesn't exist
mkdir -p "$LOG_DIR"

# Create/refresh a symlink to the latest log for easy tailing
ln -sfn "$LOG_FILE" "$LOG_DIR/server.log"

# Function to stop any running dotnet processes
stop_server() {
    echo "🛑 Stopping any running dotnet processes..."
    
    # Find and kill any dotnet processes (including dotnet watch)
    PIDS=$(pgrep -f "dotnet.*CodexBootstrap\|dotnet.*watch" || true)
    if [ ! -z "$PIDS" ]; then
        echo "Found running dotnet processes: $PIDS"
        kill -TERM $PIDS 2>/dev/null || true
        sleep 3
        
        # Force kill if still running
        PIDS=$(pgrep -f "dotnet.*CodexBootstrap\|dotnet.*watch" || true)
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
    local server_pid=$1
    echo "⏳ Waiting for server to start (PID: $server_pid)..."
    local max_attempts=30  # Increased timeout for health checks
    local attempt=0
    local start_time=$(date +%s)

    sleep 2  # Reduced initial wait
    while [ $attempt -lt $max_attempts ]; do
        # Check if the process is still running
        if ! kill -0 $server_pid 2>/dev/null; then
            echo "❌ Server process (PID: $server_pid) has died unexpectedly"
            echo "📋 Checking logs for startup errors..."
            tail -n 30 "$LOG_FILE" 2>/dev/null || echo "No logs available"
            return 1
        fi
        
        # Check if server is responding
        if curl -s "http://127.0.0.1:$PORT/health" >/dev/null 2>&1; then
            local end_time=$(date +%s)
            local startup_time=$((end_time - start_time))
            echo "✅ Server is ready! (startup time: ${startup_time}s)"
            export DISABLE_AI=false
            return 0
        fi
        
        attempt=$((attempt + 1))
        local current_time=$(date +%s)
        local elapsed=$((current_time - start_time))
        echo "Attempt $attempt/$max_attempts - waiting... (PID: $server_pid still running, elapsed: ${elapsed}s)"
        sleep 1
    done
    
    local end_time=$(date +%s)
    local total_time=$((end_time - start_time))
    echo "❌ Server failed to start within ${total_time} seconds"
    echo "📋 Checking logs for startup issues..."
    tail -n 30 "$LOG_FILE" 2>/dev/null || echo "No logs available"
    return 1
}

# Function to test server endpoints
test_server() {
    echo "🧪 Testing server endpoints..."
    
    # Test health endpoint
    echo "Testing /health..."
    if curl -s "http://127.0.0.1:$PORT/health" | jq '.registrationMetrics' >/dev/null 2>&1; then
        echo "✅ Health endpoint working"
    else
        echo "❌ Health endpoint failed"
        return 1
    fi
    
    # Test AI health endpoint
    echo "Testing /ai/health..."
    if curl -s "http://127.0.0.1:$PORT/ai/health" >/dev/null 2>&1; then
        echo "✅ AI health endpoint working"
    else
        echo "❌ AI health endpoint failed"
        return 1
    fi
    
    # Test spec endpoints
    echo "Testing /spec/modules..."
    if curl -s "http://127.0.0.1:$PORT/spec/modules" >/dev/null 2>&1; then
        echo "✅ Spec modules endpoint working"
    else
        echo "❌ Spec modules endpoint failed"
        return 1
    fi
    
    echo "✅ All endpoint tests passed!"
    return 0
}

# Global variable to track server PID
SERVER_PID=""
SERVER_STARTED_SUCCESSFULLY=false

# Cleanup function
cleanup() {
    if [ "$SERVER_STARTED_SUCCESSFULLY" = false ] && [ ! -z "$SERVER_PID" ] && kill -0 $SERVER_PID 2>/dev/null; then
        echo ""
        echo "🛑 Script interrupted during startup. Stopping server (PID: $SERVER_PID)..."
        kill $SERVER_PID 2>/dev/null || true
        sleep 2
        if kill -0 $SERVER_PID 2>/dev/null; then
            echo "🔨 Force killing server..."
            kill -9 $SERVER_PID 2>/dev/null || true
        fi
        echo "✅ Server stopped"
    fi
}

# Set up signal handlers
trap cleanup EXIT INT TERM

# Function to analyze startup logs for issues
analyze_startup_logs() {
    local log_file="$1"
    local issues_found=false
    
    echo "🔍 Analyzing startup logs for issues..."
    
    if [ ! -f "$log_file" ]; then
        echo "⚠️  Log file not found: $log_file"
        return 1
    fi
    
    # Check for critical errors
    local error_count=$(grep -c "ERROR\|Exception\|Error:" "$log_file" 2>/dev/null | tail -1 || echo "0")
    if [ "$error_count" -gt 0 ]; then
        echo "❌ Found $error_count error(s) in startup logs:"
        grep -n "ERROR\|Exception\|Error:" "$log_file" 2>/dev/null | head -10 | while read -r line; do
            echo "   $line"
        done
        issues_found=true
    fi
    
    # Check for warnings
    local warning_count=$(grep -c "WARN\|Warning:" "$log_file" 2>/dev/null | tail -1 || echo "0")
    if [ "$warning_count" -gt 0 ]; then
        echo "⚠️  Found $warning_count warning(s) in startup logs:"
        grep -n "WARN\|Warning:" "$log_file" 2>/dev/null | head -5 | while read -r line; do
            echo "   $line"
        done
        if [ "$warning_count" -gt 5 ]; then
            echo "   ... and $((warning_count - 5)) more warnings"
        fi
        issues_found=true
    fi
    
    # Check for module loading issues
    local module_errors=$(grep -c "Failed to register HTTP endpoints\|Unable to find the required services\|Object reference not set" "$log_file" 2>/dev/null | tail -1 || echo "0")
    if [ "$module_errors" -gt 0 ]; then
        echo "🔧 Found $module_errors module loading issue(s):"
        grep -n "Failed to register HTTP endpoints\|Unable to find the required services\|Object reference not set" "$log_file" 2>/dev/null | while read -r line; do
            echo "   $line"
        done
        issues_found=true
    fi
    
    # Check for spec loading issues
    local spec_errors=$(grep -c "Error loading spec file\|spec files to process" "$log_file" 2>/dev/null | tail -1 || echo "0")
    if [ "$spec_errors" -gt 0 ]; then
        echo "📋 Spec loading analysis:"
        grep -n "Error loading spec file\|spec files to process" "$log_file" 2>/dev/null | while read -r line; do
            echo "   $line"
        done
        issues_found=true
    fi
    
    # Check for build warnings/errors
    local build_issues=$(grep -c "warning CS\|error CS" "$log_file" 2>/dev/null | tail -1 || echo "0")
    if [ "$build_issues" -gt 0 ]; then
        echo "🔨 Found $build_issues build issue(s):"
        grep -n "warning CS\|error CS" "$log_file" 2>/dev/null | head -5 | while read -r line; do
            echo "   $line"
        done
        if [ "$build_issues" -gt 5 ]; then
            echo "   ... and $((build_issues - 5)) more build issues"
        fi
        issues_found=true
    fi
    
    # Summary
    if [ "$issues_found" = true ]; then
        echo ""
        echo "⚠️  ISSUES DETECTED DURING STARTUP"
        echo "   The server is running but may have problems."
        echo "   Check the logs above for details."
        echo "   Server remains accessible for debugging."
        echo ""
    else
        echo "✅ No critical issues detected in startup logs"
    fi
}

# Main execution
main() {
    local script_start_time=$(date +%s)
    echo "=========================================="
    echo "🌟 Living Codex Server Startup"
    echo "=========================================="
    echo "Port: $PORT"
    echo "Project: $PROJECT_DIR"
    echo "Logs: $LOG_FILE"
    echo "Start Time: $(date)"
    echo "=========================================="
    
    # Stop any running server
    stop_server
    
    # Wait a moment for cleanup
    sleep 2
    
    # Check if port is available, find alternative if needed
    if ! check_port; then
        echo "⚠️  Port $PORT is in use, finding alternative..."
        AVAILABLE_PORT=$(find_available_port $PORT)
        if [ $? -eq 0 ]; then
            PORT=$AVAILABLE_PORT
            echo "✅ Using alternative port: $PORT"
        else
            echo "❌ Cannot find available port - exiting"
            exit 1
        fi
    fi
    
    # Navigate to project directory
    if [ ! -d "$PROJECT_DIR" ]; then
        echo "❌ Project directory not found: $PROJECT_DIR"
        exit 1
    fi
    
    if [ ! -f "$PROJECT_DIR/CodexBootstrap.csproj" ]; then
        echo "❌ Project file not found: $PROJECT_DIR/CodexBootstrap.csproj"
        exit 1
    fi
    
    cd "$PROJECT_DIR"
    echo "📁 Changed to project directory: $(pwd)"
    
    # Check if dotnet is available
    if ! command -v dotnet &> /dev/null; then
        echo "❌ dotnet command not found - please install .NET SDK"
        exit 1
    fi
    
    # Build the project
    echo "🔨 Building project..."
    if ! dotnet build --configuration Release --verbosity quiet; then
        echo "❌ Build failed - stopping startup"
        echo "📋 Build errors:"
        dotnet build --configuration Release --verbosity normal 2>&1 | tail -20
        exit 1
    fi
    echo "✅ Build successful"
    
    # Start the server with logging
    echo "🚀 Starting server on port $PORT..."
    echo "📝 Logs will be written to: $LOG_FILE"
    echo "📺 Use 'tail -f $LOG_FILE' to monitor logs"
    
    # Start server with logging to file
    # Use regular dotnet run for stability (can be changed to dotnet watch for development)
    # Explicitly bind to IPv4 localhost to avoid IPv6 issues
    if [ "$1" = "--watch" ] || [ "$1" = "-w" ]; then
        echo "🔄 Starting with hot-reload (development mode)..."
        dotnet watch run --hot-reload --urls "http://127.0.0.1:$PORT" --configuration Release > "$LOG_FILE" 2>&1 &
    else
        echo "🚀 Starting in production mode..."
        dotnet run --urls "http://127.0.0.1:$PORT" --configuration Release > "$LOG_FILE" 2>&1 &
    fi
    SERVER_PID=$!
    
    echo "🆔 Server PID: $SERVER_PID"
    echo "📝 Log file: $LOG_FILE"
    
    # Give the server a moment to start
    sleep 1
    
    # Wait for server to be ready
    if wait_for_server $SERVER_PID; then
        echo "✅ Server started successfully!"
        
        # Test the server
        if test_server; then
            local script_end_time=$(date +%s)
            local total_startup_time=$((script_end_time - script_start_time))
            echo ""
            echo "🎉 Living Codex Server is running!"
            echo "⏱️  Total startup time: ${total_startup_time}s"
            echo "🌐 URL: http://127.0.0.1:$PORT"
            echo "📊 Health: http://127.0.0.1:$PORT/health"
            echo "🤖 AI Health: http://127.0.0.1:$PORT/ai/health"
            echo "📋 Spec: http://127.0.0.1:$PORT/spec/modules"
            echo "📖 Swagger: http://127.0.0.1:$PORT/swagger"
            echo "📝 Logs: $LOG_FILE"
            echo ""
            echo "To stop the server: kill $SERVER_PID"
            echo "To view logs: tail -f $LOG_FILE"
            echo ""
            
            # Analyze startup logs for issues
            analyze_startup_logs "$LOG_FILE"
            
            # Mark server as successfully started
            SERVER_STARTED_SUCCESSFULLY=true
            
            echo "🚀 Server is running in the background (PID: $SERVER_PID)."
            echo "   The script will now exit, leaving the server running."
            echo "   You can interact with the server immediately."
            echo ""
            echo "📋 Note: Background initialization tasks are still running:"
            echo "   - U-CORE ontology seeding"
            echo "   - Reflection tree building"
            echo "   - Edge ensurance processing"
            echo "   Check logs for progress: tail -f $LOG_FILE"
            echo ""
            
            # Server is running successfully in background - exit script
            exit 0
        else
            echo "❌ Server tests failed"
            echo "🛑 Stopping server..."
            kill $SERVER_PID 2>/dev/null || true
            sleep 2
            # Force kill if still running
            if kill -0 $SERVER_PID 2>/dev/null; then
                echo "🔨 Force killing server..."
                kill -9 $SERVER_PID 2>/dev/null || true
            fi
            exit 1
        fi
    else
        echo "❌ Server failed to start"
        echo "🛑 Stopping server..."
        kill $SERVER_PID 2>/dev/null || true
        sleep 2
        # Force kill if still running
        if kill -0 $SERVER_PID 2>/dev/null; then
            echo "🔨 Force killing server..."
            kill -9 $SERVER_PID 2>/dev/null || true
        fi
        echo "📋 Last 20 lines of log:"
        tail -n 20 "$LOG_FILE" 2>/dev/null || echo "No logs available"
        exit 1
    fi
}

# Run main function
main "$@"
