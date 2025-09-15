#!/bin/bash

# Living Codex Mobile App Test Runner
# This script runs all tests for the mobile application

set -e

echo "🧪 Living Codex Mobile App Test Runner"
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "LivingCodexMobile.Tests.csproj" ]; then
    print_error "Please run this script from the LivingCodexMobile.Tests directory"
    exit 1
fi

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET is not installed or not in PATH"
    exit 1
fi

print_status "Checking .NET version..."
dotnet --version

# Clean and restore
print_status "Cleaning and restoring packages..."
dotnet clean
dotnet restore

# Build the project
print_status "Building test project..."
if dotnet build --configuration Release --verbosity minimal; then
    print_success "Build successful"
else
    print_error "Build failed"
    exit 1
fi

# Run tests
print_status "Running tests..."
echo ""

# Run all tests
if dotnet test --configuration Release --verbosity normal --logger "console;verbosity=detailed"; then
    print_success "All tests passed! 🎉"
    echo ""
    print_status "Test Summary:"
    echo "  ✅ Unit Tests: Passed"
    echo "  ✅ Integration Tests: Passed"
    echo "  ✅ UI Tests: Passed"
    echo "  ✅ API Tests: Passed"
    echo ""
    print_success "Mobile app is ready for deployment! 🚀"
else
    print_error "Some tests failed"
    echo ""
    print_status "Test Summary:"
    echo "  ❌ Some tests failed"
    echo ""
    print_warning "Please check the test output above for details"
    exit 1
fi

echo ""
print_status "Test run completed!"
