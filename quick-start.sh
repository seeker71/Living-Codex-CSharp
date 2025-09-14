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
