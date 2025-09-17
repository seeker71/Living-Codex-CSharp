#!/bin/bash

# UI Performance and Link Testing Script
# Tests page load times and link functionality for Living Codex UI

UI_URL="http://localhost:3000"
LOAD_TIME_LIMIT=2  # 2 seconds limit
TIMEOUT_LIMIT=10   # 10 seconds timeout

echo "🧪 Testing UI Page Performance and Link Functionality"
echo "UI URL: $UI_URL"
echo "Load Time Limit: ${LOAD_TIME_LIMIT}s"
echo "Timeout Limit: ${TIMEOUT_LIMIT}s"
echo "=================================================="

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
TOTAL_PAGES=0
FAST_PAGES=0
SLOW_PAGES=0
FAILED_PAGES=0
TOTAL_LINKS=0
WORKING_LINKS=0
BROKEN_LINKS=0

# Test page loading function
test_page_load() {
    local path=$1
    local description=$2
    local url="$UI_URL$path"
    
    TOTAL_PAGES=$((TOTAL_PAGES + 1))
    echo -n "Testing page: $description ($path)... "
    
    # Test page load time with curl
    local result=$(curl -s -w 'STATUS:%{http_code},TIME:%{time_total},SIZE:%{size_download}' \
                   --max-time $TIMEOUT_LIMIT \
                   "$url" -o /dev/null 2>&1)
    local exit_code=$?
    
    if [ $exit_code -eq 28 ]; then
        # Timeout
        echo -e "${RED}TIMEOUT${NC} (>${TIMEOUT_LIMIT}s)"
        FAILED_PAGES=$((FAILED_PAGES + 1))
        return 1
    elif [ $exit_code -ne 0 ]; then
        # Other error
        echo -e "${RED}ERROR${NC} (curl exit code: $exit_code)"
        FAILED_PAGES=$((FAILED_PAGES + 1))
        return 1
    else
        # Parse result
        local status=$(echo "$result" | grep -o 'STATUS:[0-9]*' | cut -d: -f2)
        local time=$(echo "$result" | grep -o 'TIME:[0-9.]*' | cut -d: -f2)
        local size=$(echo "$result" | grep -o 'SIZE:[0-9]*' | cut -d: -f2)
        
        if [ -z "$status" ] || [ -z "$time" ]; then
            echo -e "${RED}PARSE_ERROR${NC} (result: $result)"
            FAILED_PAGES=$((FAILED_PAGES + 1))
            return 1
        fi
        
        # Check if slow
        local is_slow=$(echo "$time > $LOAD_TIME_LIMIT" | bc -l 2>/dev/null || echo "0")
        
        if [ "$status" -ge 200 ] && [ "$status" -lt 300 ]; then
            if [ "$is_slow" = "1" ]; then
                echo -e "${YELLOW}SLOW${NC} (${time}s, ${size} bytes, HTTP $status)"
                SLOW_PAGES=$((SLOW_PAGES + 1))
            else
                echo -e "${GREEN}FAST${NC} (${time}s, ${size} bytes, HTTP $status)"
                FAST_PAGES=$((FAST_PAGES + 1))
            fi
            return 0
        else
            echo -e "${RED}HTTP_ERROR${NC} (${time}s, HTTP $status)"
            FAILED_PAGES=$((FAILED_PAGES + 1))
            return 1
        fi
    fi
}

# Test link functionality
test_links_on_page() {
    local path=$1
    local description=$2
    local url="$UI_URL$path"
    
    echo "  Checking links on $description..."
    
    # Get page content and extract links
    local page_content=$(curl -s --max-time 5 "$url" 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$page_content" ]; then
        echo -e "    ${RED}Could not fetch page content${NC}"
        return 1
    fi
    
    # Extract href attributes from the page (simplified - looks for common patterns)
    local links=$(echo "$page_content" | grep -o 'href="[^"]*"' | sed 's/href="//g' | sed 's/"//g' | grep -E '^/' | sort -u)
    
    if [ -z "$links" ]; then
        echo -e "    ${YELLOW}No internal links found${NC}"
        return 0
    fi
    
    local page_link_count=0
    local page_working_links=0
    local page_broken_links=0
    
    for link in $links; do
        page_link_count=$((page_link_count + 1))
        TOTAL_LINKS=$((TOTAL_LINKS + 1))
        
        # Test the link
        local link_url="$UI_URL$link"
        local link_result=$(curl -s -w '%{http_code}' --max-time 3 "$link_url" -o /dev/null 2>&1)
        local link_exit_code=$?
        
        if [ $link_exit_code -eq 0 ] && [ "$link_result" -ge 200 ] && [ "$link_result" -lt 400 ]; then
            echo -e "    ✅ $link (HTTP $link_result)"
            page_working_links=$((page_working_links + 1))
            WORKING_LINKS=$((WORKING_LINKS + 1))
        else
            echo -e "    ${RED}❌ $link (HTTP $link_result, exit: $link_exit_code)${NC}"
            page_broken_links=$((page_broken_links + 1))
            BROKEN_LINKS=$((BROKEN_LINKS + 1))
        fi
    done
    
    echo -e "    Links summary: ${GREEN}$page_working_links working${NC}, ${RED}$page_broken_links broken${NC}"
}

# Check if UI is running
echo "🔍 Checking if UI is accessible..."
if ! curl -s --max-time 3 "$UI_URL" > /dev/null 2>&1; then
    echo -e "${RED}❌ UI is not accessible at $UI_URL${NC}"
    echo "Please make sure the UI development server is running:"
    echo "  cd living-codex-ui && npm run dev"
    exit 1
fi
echo -e "${GREEN}✅ UI is accessible${NC}"
echo ""

# Test all pages
echo "📄 Testing Page Load Performance"
echo "--------------------------------"
test_page_load "/" "Home Page"
test_page_load "/discover" "Discover Page"
test_page_load "/graph" "Graph Page"
test_page_load "/resonance" "Resonance Page"
test_page_load "/about" "About Page"
test_page_load "/auth" "Authentication Page"
test_page_load "/profile" "Profile Page"

echo ""
echo "🔗 Testing Link Functionality"
echo "-----------------------------"

# Test links on each page (only if page loaded successfully)
if test_page_load "/" "Home Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/" "Home Page"
fi

if test_page_load "/discover" "Discover Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/discover" "Discover Page"
fi

if test_page_load "/graph" "Graph Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/graph" "Graph Page"
fi

if test_page_load "/resonance" "Resonance Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/resonance" "Resonance Page"
fi

if test_page_load "/about" "About Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/about" "About Page"
fi

if test_page_load "/auth" "Auth Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/auth" "Auth Page"
fi

if test_page_load "/profile" "Profile Page (for link testing)" > /dev/null 2>&1; then
    test_links_on_page "/profile" "Profile Page"
fi

echo ""
echo "📊 Performance Test Results"
echo "==========================="
echo -e "Total Pages:     ${BLUE}$TOTAL_PAGES${NC}"
echo -e "Fast (<${LOAD_TIME_LIMIT}s):     ${GREEN}$FAST_PAGES${NC}"
echo -e "Slow (>${LOAD_TIME_LIMIT}s):     ${YELLOW}$SLOW_PAGES${NC}"
echo -e "Failed:          ${RED}$FAILED_PAGES${NC}"

if [ $TOTAL_PAGES -gt 0 ]; then
    FAST_RATE=$(echo "scale=1; $FAST_PAGES * 100 / $TOTAL_PAGES" | bc -l 2>/dev/null || echo "0")
    echo -e "Fast Page Rate:  ${GREEN}${FAST_RATE}%${NC}"
fi

echo ""
echo "🔗 Link Test Results"
echo "==================="
echo -e "Total Links:     ${BLUE}$TOTAL_LINKS${NC}"
echo -e "Working Links:   ${GREEN}$WORKING_LINKS${NC}"
echo -e "Broken Links:    ${RED}$BROKEN_LINKS${NC}"

if [ $TOTAL_LINKS -gt 0 ]; then
    LINK_SUCCESS_RATE=$(echo "scale=1; $WORKING_LINKS * 100 / $TOTAL_LINKS" | bc -l 2>/dev/null || echo "0")
    echo -e "Link Success Rate: ${GREEN}${LINK_SUCCESS_RATE}%${NC}"
fi

echo ""
echo "🎯 Overall Assessment"
echo "===================="

# Performance assessment
if [ $FAILED_PAGES -eq 0 ] && [ $SLOW_PAGES -eq 0 ]; then
    echo -e "${GREEN}✅ All pages load fast (<${LOAD_TIME_LIMIT}s)${NC}"
elif [ $FAILED_PAGES -eq 0 ] && [ $SLOW_PAGES -le 1 ]; then
    echo -e "${YELLOW}⚠️ Most pages load fast, but $SLOW_PAGES page(s) are slow${NC}"
elif [ $FAILED_PAGES -eq 0 ]; then
    echo -e "${YELLOW}⚠️ $SLOW_PAGES pages are slower than ${LOAD_TIME_LIMIT}s but functional${NC}"
else
    echo -e "${RED}❌ $FAILED_PAGES pages failed to load properly${NC}"
fi

# Link assessment
if [ $BROKEN_LINKS -eq 0 ] && [ $TOTAL_LINKS -gt 0 ]; then
    echo -e "${GREEN}✅ All navigation links work correctly${NC}"
elif [ $BROKEN_LINKS -le 2 ] && [ $TOTAL_LINKS -gt 0 ]; then
    echo -e "${YELLOW}⚠️ Most links work, but $BROKEN_LINKS link(s) are broken${NC}"
elif [ $TOTAL_LINKS -gt 0 ]; then
    echo -e "${RED}❌ $BROKEN_LINKS out of $TOTAL_LINKS links are broken${NC}"
else
    echo -e "${YELLOW}⚠️ No navigation links found to test${NC}"
fi

echo ""
echo "💡 Recommendations:"
if [ $SLOW_PAGES -gt 0 ]; then
    echo "   • Optimize slow-loading pages with code splitting or caching"
    echo "   • Check for large bundle sizes or blocking API calls"
    echo "   • Consider lazy loading for heavy components"
fi
if [ $BROKEN_LINKS -gt 0 ]; then
    echo "   • Fix broken navigation links"
    echo "   • Verify all route definitions in Next.js"
    echo "   • Check for typos in href attributes"
fi
if [ $FAILED_PAGES -gt 0 ]; then
    echo "   • Investigate failed pages for JavaScript errors"
    echo "   • Check browser console for error messages"
    echo "   • Verify all required dependencies are loaded"
fi

# Exit code based on results
if [ $FAILED_PAGES -eq 0 ] && [ $BROKEN_LINKS -eq 0 ] && [ $SLOW_PAGES -le 1 ]; then
    echo -e "\n${GREEN}🎉 UI performance test PASSED!${NC}"
    exit 0
elif [ $FAILED_PAGES -eq 0 ] && [ $BROKEN_LINKS -le 2 ]; then
    echo -e "\n${YELLOW}⚠️ UI performance test passed with minor issues${NC}"
    exit 0
else
    echo -e "\n${RED}❌ UI performance test FAILED - significant issues found${NC}"
    exit 1
fi
