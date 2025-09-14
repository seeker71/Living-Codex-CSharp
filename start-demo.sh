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
