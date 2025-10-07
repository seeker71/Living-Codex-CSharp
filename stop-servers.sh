#!/bin/bash

# Stop all running CodexBootstrap server instances
# This script prevents the file locking issues that cause build failures

echo "🛑 Stopping all CodexBootstrap server instances..."

# Kill any running CodexBootstrap processes
pkill -f "CodexBootstrap" 2>/dev/null || true
pkill -f "dotnet.*CodexBootstrap" 2>/dev/null || true

# Wait a moment for processes to terminate
sleep 2

# Check if any processes are still running
if pgrep -f "CodexBootstrap" > /dev/null; then
    echo "⚠️  Some CodexBootstrap processes are still running. Force killing..."
    pkill -9 -f "CodexBootstrap" 2>/dev/null || true
    sleep 1
fi

# Verify cleanup
if pgrep -f "CodexBootstrap" > /dev/null; then
    echo "❌ Failed to stop all CodexBootstrap processes"
    echo "Running processes:"
    pgrep -f "CodexBootstrap" | xargs ps -p
    exit 1
else
    echo "✅ All CodexBootstrap server instances stopped successfully"
fi

echo "🧹 You can now build and start the server without file locking issues"

