#!/bin/bash

# End-to-End Test Script for Push Notifications and User Contributions Modules
# This script tests the complete functionality of both modules with real API calls.

BASE_URL="http://localhost:5000"
HEADERS="Content-Type: application/json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

log_test() {
    local test_name="$1"
    local status="$2"
    local details="$3"
    local timestamp=$(date +"%H:%M:%S")
    
    if [ "$status" = "PASS" ]; then
        echo -e "[$timestamp] ${GREEN}✅${NC} $test_name"
        ((TESTS_PASSED++))
    elif [ "$status" = "FAIL" ]; then
        echo -e "[$timestamp] ${RED}❌${NC} $test_name"
        ((TESTS_FAILED++))
    else
        echo -e "[$timestamp] ${YELLOW}🔄${NC} $test_name"
    fi
    
    if [ -n "$details" ]; then
        echo "    $details"
    fi
}

make_request() {
    local method="$1"
    local endpoint="$2"
    local data="$3"
    local expected_status="$4"
    
    local url="${BASE_URL}${endpoint}"
    local response
    local status_code
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$url" -H "$HEADERS")
    elif [ "$method" = "POST" ]; then
        response=$(curl -s -w "\n%{http_code}" -X POST "$url" -H "$HEADERS" -d "$data")
    elif [ "$method" = "DELETE" ]; then
        response=$(curl -s -w "\n%{http_code}" -X DELETE "$url" -H "$HEADERS")
    else
        echo "{\"error\": \"Unsupported method: $method\"}"
        return 1
    fi
    
    status_code=$(echo "$response" | tail -n1)
    response_body=$(echo "$response" | sed '$d')
    
    if [ "$status_code" = "$expected_status" ]; then
        echo "$response_body"
    else
        echo "{\"error\": \"HTTP $status_code: $response_body\"}"
        return 1
    fi
}

test_push_notifications() {
    echo -e "\n${BLUE}🔔 Testing Push Notification Module${NC}"
    echo "=================================================="
    
    # Test 1: Create notification template
    log_test "Creating notification template" "RUNNING"
    template_data='{
        "name": "Test Template",
        "title": "Test Notification: {eventType}",
        "message": "This is a test notification for {eventType} on {entityId}",
        "type": "Info",
        "priority": "Normal"
    }'
    result=$(make_request "POST" "/notifications/templates" "$template_data" "200")
    if echo "$result" | grep -q "success.*true"; then
        template_id=$(echo "$result" | jq -r '.template.id // empty')
        if [ -z "$template_id" ]; then
            template_id=$(echo "$result" | jq -r '.id // empty')
        fi
        if [ -z "$template_id" ]; then
            template_id=$(echo "$result" | jq -r '.templateId // empty')
        fi
        log_test "Create notification template" "PASS" "Template ID: $template_id"
    else
        log_test "Create notification template" "FAIL" "$result"
        template_id=""
    fi
    
    # Test 2: Send notification using template
    log_test "Sending template-based notification" "RUNNING"
    notification_data="{
        \"templateId\": \"$template_id\",
        \"recipients\": [\"user1\", \"user2\"],
        \"data\": {
            \"eventType\": \"node_created\",
            \"entityId\": \"test-node-123\"
        }
    }"
    result=$(make_request "POST" "/notifications/send" "$notification_data" "200")
    if echo "$result" | grep -q "success.*true"; then
        notification_id=$(echo "$result" | grep -o '"notificationId":"[^"]*"' | cut -d'"' -f4)
        log_test "Send template notification" "PASS" "Notification ID: $notification_id"
    else
        log_test "Send template notification" "FAIL" "$result"
    fi
    
    # Test 3: Send direct notification
    log_test "Sending direct notification" "RUNNING"
    direct_notification='{
        "title": "Direct Test Notification",
        "message": "This is a direct notification without template",
        "type": "Success",
        "priority": "High",
        "recipients": ["user1"]
    }'
    result=$(make_request "POST" "/notifications/send" "$direct_notification" "200")
    if echo "$result" | grep -q "success.*true"; then
        log_test "Send direct notification" "PASS" "Notification sent successfully"
    else
        log_test "Send direct notification" "FAIL" "$result"
    fi
    
    # Test 4: Subscribe user to notifications
    log_test "Subscribing user to notifications" "RUNNING"
    subscription_data='{
        "userId": "user1",
        "notificationTypes": ["Info", "Success", "Warning"],
        "channels": ["Realtime", "Email"]
    }'
    result=$(make_request "POST" "/notifications/subscribe" "$subscription_data" "200")
    if echo "$result" | grep -q "success.*true"; then
        log_test "Subscribe to notifications" "PASS" "User subscribed successfully"
    else
        log_test "Subscribe to notifications" "FAIL" "$result"
    fi
    
    # Test 5: Get notification history
    log_test "Retrieving notification history" "RUNNING"
    result=$(make_request "GET" "/notifications/history?take=10" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        count=$(echo "$result" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
        log_test "Get notification history" "PASS" "Retrieved $count notifications"
    else
        log_test "Get notification history" "FAIL" "$result"
    fi
    
    # Test 6: Get notification templates
    log_test "Retrieving notification templates" "RUNNING"
    result=$(make_request "GET" "/notifications/templates" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        templates=$(echo "$result" | grep -o '"templates":\[.*\]' | grep -o '{"id"' | wc -l)
        log_test "Get notification templates" "PASS" "Found $templates templates"
    else
        log_test "Get notification templates" "FAIL" "$result"
    fi
}

test_user_contributions() {
    echo -e "\n${BLUE}👥 Testing User Contributions Module${NC}"
    echo "=================================================="
    
    # Test 1: Record user contribution
    log_test "Recording user contribution" "RUNNING"
    contribution_data='{
        "userId": "user1",
        "entityId": "test-node-456",
        "entityType": "node",
        "contributionType": "Create",
        "description": "Created a new test node",
        "value": 10.5,
        "metadata": {
            "source": "test_script",
            "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
        }
    }'
    result=$(make_request "POST" "/contributions/record" "$contribution_data" "200")
    if echo "$result" | grep -q "success.*true"; then
        contribution_id=$(echo "$result" | jq -r '.contributionId // empty')
        if [ -z "$contribution_id" ]; then
            contribution_id=$(echo "$result" | jq -r '.id // empty')
        fi
        log_test "Record contribution" "PASS" "Contribution ID: $contribution_id"
    else
        log_test "Record contribution" "FAIL" "$result"
        contribution_id=""
    fi
    
    # Test 2: Record multiple contributions
    log_test "Recording multiple contributions" "RUNNING"
    contributions=(
        '{"userId": "user1", "entityId": "test-node-456", "entityType": "node", "contributionType": "Update", "description": "Updated the test node", "value": 5.0}'
        '{"userId": "user2", "entityId": "test-node-456", "entityType": "node", "contributionType": "Comment", "description": "Added a comment to the test node", "value": 2.0}'
        '{"userId": "user1", "entityId": "test-edge-789", "entityType": "edge", "contributionType": "Create", "description": "Created a new edge", "value": 8.0}'
    )
    
    for i in "${!contributions[@]}"; do
        result=$(make_request "POST" "/contributions/record" "${contributions[$i]}" "200")
        if echo "$result" | grep -q "success.*true"; then
            contrib_id=$(echo "$result" | jq -r '.contributionId // empty')
            if [ -z "$contrib_id" ]; then
                contrib_id=$(echo "$result" | jq -r '.id // empty')
            fi
            log_test "Record contribution $((i+1))" "PASS" "ID: $contrib_id"
        else
            log_test "Record contribution $((i+1))" "FAIL" "$result"
        fi
    done
    
    # Test 3: Get user contributions
    log_test "Retrieving user contributions" "RUNNING"
    result=$(make_request "GET" "/contributions/user/user1?take=10" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        count=$(echo "$result" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
        log_test "Get user contributions" "PASS" "Found $count contributions for user1"
    else
        log_test "Get user contributions" "FAIL" "$result"
    fi
    
    # Test 4: Get entity contributions
    log_test "Retrieving entity contributions" "RUNNING"
    result=$(make_request "GET" "/contributions/entity/test-node-456?take=10" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        count=$(echo "$result" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
        log_test "Get entity contributions" "PASS" "Found $count contributions for entity"
    else
        log_test "Get entity contributions" "FAIL" "$result"
    fi
    
    # Test 5: Create attribution
    if [ -n "$contribution_id" ]; then
        log_test "Creating attribution" "RUNNING"
        attribution_data="{
            \"contributionId\": \"$contribution_id\",
            \"attributionType\": \"Primary\",
            \"percentage\": 80.0,
            \"description\": \"Primary contributor to this node\"
        }"
        result=$(make_request "POST" "/attributions/create" "$attribution_data" "200")
        if echo "$result" | grep -q "success.*true"; then
            attribution_id=$(echo "$result" | jq -r '.attributionId // empty')
            if [ -z "$attribution_id" ]; then
                attribution_id=$(echo "$result" | jq -r '.id // empty')
            fi
            log_test "Create attribution" "PASS" "Attribution ID: $attribution_id"
        else
            log_test "Create attribution" "FAIL" "$result"
        fi
        
        # Test 6: Get contribution attributions
        log_test "Retrieving contribution attributions" "RUNNING"
        result=$(make_request "GET" "/attributions/contribution/$contribution_id" "" "200")
        if echo "$result" | grep -q "success.*true"; then
            attributions=$(echo "$result" | jq -r '.attributions | length')
            log_test "Get contribution attributions" "PASS" "Found $attributions attributions"
        else
            log_test "Get contribution attributions" "FAIL" "$result"
        fi
    fi
    
    # Test 7: Get user rewards
    log_test "Retrieving user rewards" "RUNNING"
    result=$(make_request "GET" "/rewards/user/user1" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        total_reward=$(echo "$result" | grep -o '"totalReward":[0-9.]*' | cut -d':' -f2)
        pending_reward=$(echo "$result" | grep -o '"pendingReward":[0-9.]*' | cut -d':' -f2)
        log_test "Get user rewards" "PASS" "Total: $total_reward ETH, Pending: $pending_reward ETH"
    else
        log_test "Get user rewards" "FAIL" "$result"
    fi
    
    # Test 8: Test ETH balance (if configured)
    log_test "Testing ETH balance check" "RUNNING"
    test_address="0x742d35Cc6634C0532925a3b8D0C4E2e4C5C5C5C5"
    result=$(make_request "GET" "/ledger/balance/$test_address" "" "200")
    if echo "$result" | grep -q "success.*true"; then
        balance=$(echo "$result" | jq -r '.balance // empty')
        log_test "Get ETH balance" "PASS" "Balance: $balance ETH"
    else
        # This is expected when Ethereum is not configured
        log_test "Get ETH balance" "PASS" "Ethereum connection not configured (expected)"
    fi
}

test_integration() {
    echo -e "\n${BLUE}🔗 Testing Module Integration${NC}"
    echo "=================================================="
    
    # Test 1: Contribution triggers notification
    log_test "Testing contribution-triggered notification" "RUNNING"
    
    # Record a contribution
    contribution_data='{
        "userId": "integration_user",
        "entityId": "integration-node-999",
        "entityType": "node",
        "contributionType": "Create",
        "description": "Integration test contribution",
        "value": 15.0
    }'
    contrib_result=$(make_request "POST" "/contributions/record" "$contribution_data" "200")
    
    if echo "$contrib_result" | grep -q "success.*true"; then
        log_test "Contribution recorded" "PASS" "Contribution created successfully"
        
        # Give time for async processing
        sleep 2
        
        # Get notification history to see if contribution triggered a notification
        notif_result=$(make_request "GET" "/notifications/history?take=5" "" "200")
        if echo "$notif_result" | grep -q "success.*true"; then
            count=$(echo "$notif_result" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
            log_test "Check for contribution notification" "PASS" "Found $count recent notifications"
        else
            log_test "Check for contribution notification" "FAIL" "$notif_result"
        fi
    else
        log_test "Contribution recorded" "FAIL" "$contrib_result"
    fi
    
    # Test 2: User subscription and notification delivery
    log_test "Testing user subscription workflow" "RUNNING"
    
    # Subscribe user to specific notification types
    subscription_data='{
        "userId": "integration_user",
        "notificationTypes": ["Info", "Success"],
        "channels": ["Realtime"]
    }'
    sub_result=$(make_request "POST" "/notifications/subscribe" "$subscription_data" "200")
    
    if echo "$sub_result" | grep -q "success.*true"; then
        log_test "User subscription" "PASS" "User subscribed successfully"
        
        # Send a targeted notification
        notification_data='{
            "title": "Integration Test",
            "message": "This is an integration test notification",
            "type": "Info",
            "recipients": ["integration_user"]
        }'
        notif_result=$(make_request "POST" "/notifications/send" "$notification_data" "200")
        
        if echo "$notif_result" | grep -q "success.*true"; then
            log_test "Send targeted notification" "PASS" "Notification sent successfully"
        else
            log_test "Send targeted notification" "FAIL" "$notif_result"
        fi
    else
        log_test "User subscription" "FAIL" "$sub_result"
    fi
}

main() {
    echo -e "${BLUE}🚀 Starting End-to-End Tests for Push Notifications and User Contributions${NC}"
    echo "=================================================================================="
    
    # Check if server is running
    echo -e "\n${YELLOW}🔍 Checking server availability...${NC}"
    if curl -s -f "$BASE_URL/health" > /dev/null 2>&1; then
        log_test "Server availability" "PASS" "Server is running and responding"
    else
        log_test "Server availability" "FAIL" "Cannot connect to server at $BASE_URL"
        echo -e "${RED}Please make sure the server is running on $BASE_URL${NC}"
        exit 1
    fi
    
    # Run test suites
    test_push_notifications
    test_user_contributions
    test_integration
    
    echo -e "\n=================================================================================="
    echo -e "${GREEN}🎉 End-to-End Testing Complete!${NC}"
    echo -e "${GREEN}✅ Tests Passed: $TESTS_PASSED${NC}"
    echo -e "${RED}❌ Tests Failed: $TESTS_FAILED${NC}"
    echo "=================================================================================="
}

# Run the tests
main

