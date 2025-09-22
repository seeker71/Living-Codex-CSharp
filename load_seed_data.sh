#!/bin/bash

BACKEND_URL="http://localhost:5002"
SEED_FILE="/Users/ursmuff/source/Living-Codex-go/spec/seed.jsonl"

echo "Loading seed data from $SEED_FILE"

# Count total lines
total_lines=$(wc -l < "$SEED_FILE")
echo "Total objects to process: $total_lines"

# Process nodes first (non-edge lines)
echo "Creating nodes..."
node_count=0
while IFS= read -r line; do
    if [[ "$line" == *'"id":"edge:'* ]]; then
        continue  # Skip edges for now
    fi
    
    # Extract the JSON object and convert to Node format
    id=$(echo "$line" | jq -r '.id')
    name=$(echo "$line" | jq -r '.name // .id')
    type=$(echo "$line" | jq -r '.type // "GenericNode"')
    meta=$(echo "$line" | jq '.meta // {}')
    content=$(echo "$line" | jq '.content // null')
    structure=$(echo "$line" | jq '.structure // null')
    
    # Build the node JSON
    node_json=$(cat << EOF
{
  "id": "$id",
  "title": "$name",
  "description": "U-Core entity: $name",
  "type": "$type",
  "state": "Ice",
  "meta": $meta,
  "content": {
    "externalUri": null
  }
}
EOF
)
    
    # Add content data if present
    if [[ "$content" != "null" ]]; then
        node_json=$(echo "$node_json" | jq ".content.data = $content")
    fi
    
    # Add structure to meta if present
    if [[ "$structure" != "null" ]]; then
        node_json=$(echo "$node_json" | jq ".meta.structure = $structure")
    fi
    
    # Create the node
    response=$(curl -s -X POST "$BACKEND_URL/storage-endpoints/nodes" \
        -H "Content-Type: application/json" \
        -d "$node_json")
    
    if [[ $? -eq 0 ]]; then
        ((node_count++))
        if (( node_count % 10 == 0 )); then
            echo "Created $node_count nodes..."
        fi
    else
        echo "Failed to create node: $id"
    fi
    
done < "$SEED_FILE"

echo "Created $node_count nodes"

# Process edges
echo "Creating edges..."
edge_count=0
while IFS= read -r line; do
    if [[ "$line" != *'"id":"edge:'* ]]; then
        continue  # Skip non-edges
    fi
    
    # Extract edge data
    id=$(echo "$line" | jq -r '.id')
    subj=$(echo "$line" | jq -r '.subj')
    obj=$(echo "$line" | jq -r '.obj')
    pred=$(echo "$line" | jq -r '.pred')
    meta=$(echo "$line" | jq '.meta // {}')
    
    # Build the edge JSON
    edge_json=$(cat << EOF
{
  "id": "$id",
  "fromId": "$subj",
  "toId": "$obj",
  "relationshipType": "$pred",
  "weight": 1.0,
  "meta": $meta
}
EOF
)
    
    # Create the edge
    response=$(curl -s -X POST "$BACKEND_URL/storage-endpoints/edges" \
        -H "Content-Type: application/json" \
        -d "$edge_json")
    
    if [[ $? -eq 0 ]]; then
        ((edge_count++))
        if (( edge_count % 10 == 0 )); then
            echo "Created $edge_count edges..."
        fi
    else
        echo "Failed to create edge: $id"
    fi
    
done < "$SEED_FILE"

echo "Created $edge_count edges"
echo "Seed data loading complete!"

# Test that concepts are now available
echo "Testing concepts endpoint..."
concept_count=$(curl -s "$BACKEND_URL/concepts" | jq '.concepts | length')
echo "Available concepts: $concept_count"



