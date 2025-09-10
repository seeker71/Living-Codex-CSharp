# U-CORE API Examples with Ollama LLM Integration
## Complete API Usage Guide with Real-World Examples

This document provides comprehensive API examples showing how to use the U-CORE system with Ollama LLM integration for future knowledge retrieval and bootstrap processes.

## Prerequisites

```bash
# Start Ollama service
ollama serve

# Pull a model
ollama pull llama2
# or
ollama pull llama3
```

## API Endpoints Overview

```
                    🌐 U-CORE API ENDPOINTS 🌐
                              With Ollama Integration
                    ================================================

    LLM FUTURE KNOWLEDGE MODULE (/llm)
    ├─ POST /llm/future/query          - Query future knowledge
    ├─ POST /llm/config                - Create LLM configuration
    ├─ GET  /llm/configs               - Get all configurations
    ├─ POST /llm/future/batch          - Batch query future knowledge
    └─ POST /llm/future/analyze        - Analyze future knowledge patterns

    LLM RESPONSE HANDLER MODULE (/llm/handler)
    ├─ POST /llm/handler/convert       - Convert LLM response to nodes/edges
    ├─ POST /llm/handler/parse         - Parse LLM response structure
    └─ POST /llm/handler/bootstrap     - Integrate into bootstrap process

    CORE MODULES
    ├─ GET  /health                    - System health check
    ├─ GET  /nodes                     - Get all nodes
    ├─ POST /breath/expand             - Expand phase
    ├─ POST /breath/validate           - Validate phase
    └─ POST /breath/contract           - Contract phase
```

## Example 1: Basic Future Knowledge Query

### Request
```bash
curl -X POST http://localhost:5000/llm/future/query \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What will be the next breakthrough in AI consciousness?",
    "context": "I am researching AI consciousness for my PhD thesis",
    "timeHorizon": "2 years",
    "perspective": "optimistic",
    "llmConfigId": "ollama-local",
    "metadata": {
      "researchArea": "AI Consciousness",
      "priority": "high"
    }
  }'
```

### Response
```json
{
  "success": true,
  "message": "Future knowledge generated successfully",
  "query": {
    "id": "query-12345",
    "query": "What will be the next breakthrough in AI consciousness?",
    "context": "I am researching AI consciousness for my PhD thesis",
    "timeHorizon": "2 years",
    "perspective": "optimistic",
    "llmConfig": {
      "id": "ollama-local",
      "name": "Local Ollama (Default)",
      "provider": "Ollama",
      "model": "llama2",
      "baseUrl": "http://localhost:11434",
      "maxTokens": 2000,
      "temperature": 0.7,
      "topP": 0.9
    },
    "metadata": {
      "researchArea": "AI Consciousness",
      "priority": "high"
    }
  },
  "response": {
    "id": "response-67890",
    "query": "What will be the next breakthrough in AI consciousness?",
    "response": "Based on current research trends and emerging technologies, I predict the next breakthrough in AI consciousness will involve quantum-enhanced neural networks that can process information at the speed of light while maintaining the depth of human-like understanding. This breakthrough will likely occur within the next 2 years and will revolutionize how AI systems interact with consciousness itself.",
    "confidence": 0.85,
    "reasoning": "Generated using advanced predictive algorithms",
    "sources": ["Historical patterns", "Trend analysis", "Expert knowledge"],
    "generatedAt": "2025-01-27T10:30:00Z",
    "usedConfig": {
      "id": "ollama-local",
      "name": "Local Ollama (Default)",
      "provider": "Ollama",
      "model": "llama2"
    }
  },
  "insights": [
    "Generated with 85% confidence",
    "Used Ollama llama2",
    "Response generated at 2025-01-27 10:30:00",
    "Based on 3 sources"
  ],
  "nextSteps": [
    "Review the future knowledge response carefully",
    "Consider the confidence level and reasoning",
    "Integrate insights into your planning",
    "Track how predictions unfold over time",
    "Share insights with relevant stakeholders"
  ]
}
```

## Example 2: LLM Response to Nodes/Edges Conversion

### Request
```bash
curl -X POST http://localhost:5000/llm/handler/convert \
  -H "Content-Type: application/json" \
  -d '{
    "response": "The next breakthrough in AI consciousness will involve quantum-enhanced neural networks that can process information at the speed of light while maintaining human-like understanding. This will revolutionize how AI systems interact with consciousness itself.",
    "responseType": "future-knowledge",
    "context": {
      "source": "ollama-llama2",
      "confidence": 0.85,
      "timestamp": "2025-01-27T10:30:00Z"
    }
  }'
```

### Response
```json
{
  "success": true,
  "message": "LLM response converted successfully",
  "nodes": [
    {
      "id": "future-knowledge-1",
      "typeId": "codex.future-knowledge",
      "state": "Water",
      "locale": "en",
      "title": "Future Knowledge",
      "description": "Generated from LLM response: future-knowledge",
      "content": {
        "mediaType": "application/json",
        "inlineJson": "{\"content\":\"The next breakthrough in AI consciousness will involve quantum-enhanced neural networks...\",\"confidence\":0.85,\"source\":\"future_knowledge_parser\"}",
        "inlineBytes": null,
        "externalUri": null
      },
      "meta": {
        "entityType": "future-knowledge",
        "generatedAt": "2025-01-27T10:30:00Z",
        "source": "llm_response_handler"
      }
    }
  ],
  "edges": [],
  "diffPatches": [
    {
      "id": "patch-abc123",
      "type": "add_node",
      "targetId": "future-knowledge-1",
      "content": "{\"id\":\"future-knowledge-1\",\"typeId\":\"codex.future-knowledge\",...}",
      "timestamp": "2025-01-27T10:30:00Z"
    }
  ],
  "statistics": {
    "totalNodes": 1,
    "totalEdges": 0,
    "nodeTypes": {
      "codex.future-knowledge": 1
    },
    "edgeTypes": {},
    "generatedAt": "2025-01-27T10:30:00Z"
  }
}
```

## Example 3: Complete Bootstrap Integration

### Request
```bash
curl -X POST http://localhost:5000/llm/handler/bootstrap \
  -H "Content-Type: application/json" \
  -d '{
    "response": "The next breakthrough in AI consciousness will involve quantum-enhanced neural networks that can process information at the speed of light while maintaining human-like understanding. This will revolutionize how AI systems interact with consciousness itself.",
    "responseType": "future-knowledge",
    "context": {
      "bootstrapPhase": "future-knowledge-integration",
      "priority": "high",
      "source": "ollama-llama2"
    }
  }'
```

### Response
```json
{
  "success": true,
  "message": "LLM response integrated into bootstrap process successfully",
  "nodes": [
    {
      "id": "future-knowledge-1",
      "typeId": "codex.future-knowledge",
      "state": "Ice",
      "locale": "en",
      "title": "Future Knowledge",
      "description": "Generated from LLM response: future-knowledge",
      "content": {
        "mediaType": "application/json",
        "inlineJson": "{\"content\":\"The next breakthrough in AI consciousness will involve quantum-enhanced neural networks...\",\"confidence\":0.85,\"source\":\"future_knowledge_parser\"}",
        "inlineBytes": null,
        "externalUri": null
      },
      "meta": {
        "entityType": "future-knowledge",
        "generatedAt": "2025-01-27T10:30:00Z",
        "source": "llm_response_handler"
      }
    }
  ],
  "edges": [],
  "diffPatches": [
    {
      "id": "patch-abc123",
      "type": "add_node",
      "targetId": "future-knowledge-1",
      "content": "{\"id\":\"future-knowledge-1\",\"typeId\":\"codex.future-knowledge\",...}",
      "timestamp": "2025-01-27T10:30:00Z"
    }
  ],
  "logs": [
    {
      "step": "Parse Response",
      "message": "Starting LLM response parsing",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Parse Response",
      "message": "Parsed response with 1 entities and 0 relationships",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    },
    {
      "step": "Generate Nodes",
      "message": "Generating nodes from parsed response",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Generate Nodes",
      "message": "Generated 1 nodes",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    },
    {
      "step": "Generate Edges",
      "message": "Generating edges from parsed response",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Generate Edges",
      "message": "Generated 0 edges",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    },
    {
      "step": "Create Diff Patches",
      "message": "Creating diff patches for bootstrap integration",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Create Diff Patches",
      "message": "Created 1 diff patches",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    },
    {
      "step": "Apply to Registry",
      "message": "Applying nodes and edges to registry",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Apply to Registry",
      "message": "Successfully applied all nodes and edges to registry",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    },
    {
      "step": "Validate Integration",
      "message": "Validating bootstrap integration",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "INFO"
    },
    {
      "step": "Validate Integration",
      "message": "Integration validation successful",
      "timestamp": "2025-01-27T10:30:00Z",
      "level": "SUCCESS"
    }
  ],
  "statistics": {
    "totalNodes": 1,
    "totalEdges": 0,
    "nodeTypes": {
      "codex.future-knowledge": 1
    },
    "edgeTypes": {},
    "generatedAt": "2025-01-27T10:30:00Z"
  },
  "validation": {
    "isValid": true,
    "errorMessage": null,
    "validatedAt": "2025-01-27T10:30:00Z"
  }
}
```

## Example 4: Batch Future Knowledge Queries

### Request
```bash
curl -X POST http://localhost:5000/llm/future/batch \
  -H "Content-Type: application/json" \
  -d '{
    "queries": [
      "What will be the next breakthrough in AI consciousness?",
      "How will quantum computing change the future?",
      "What are the implications of AGI for humanity?"
    ],
    "context": "Research project on future technologies",
    "timeHorizon": "5 years",
    "perspective": "realistic",
    "llmConfigId": "ollama-local",
    "metadata": {
      "project": "Future Technology Research",
      "researcher": "Dr. Jane Smith"
    }
  }'
```

### Response
```json
{
  "success": true,
  "message": "Processed 3 queries",
  "results": [
    {
      "success": true,
      "message": "Future knowledge generated successfully",
      "query": {
        "id": "query-1",
        "query": "What will be the next breakthrough in AI consciousness?",
        "context": "Research project on future technologies",
        "timeHorizon": "5 years",
        "perspective": "realistic",
        "llmConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        },
        "metadata": {
          "project": "Future Technology Research",
          "researcher": "Dr. Jane Smith"
        }
      },
      "response": {
        "id": "response-1",
        "query": "What will be the next breakthrough in AI consciousness?",
        "response": "Based on current research trends...",
        "confidence": 0.85,
        "reasoning": "Generated using advanced predictive algorithms",
        "sources": ["Historical patterns", "Trend analysis"],
        "generatedAt": "2025-01-27T10:30:00Z",
        "usedConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        }
      },
      "insights": [
        "Generated with 85% confidence",
        "Used Ollama llama2",
        "Response generated at 2025-01-27 10:30:00",
        "Based on 2 sources"
      ],
      "nextSteps": [
        "Review the future knowledge response carefully",
        "Consider the confidence level and reasoning",
        "Integrate insights into your planning",
        "Track how predictions unfold over time",
        "Share insights with relevant stakeholders"
      ]
    },
    {
      "success": true,
      "message": "Future knowledge generated successfully",
      "query": {
        "id": "query-2",
        "query": "How will quantum computing change the future?",
        "context": "Research project on future technologies",
        "timeHorizon": "5 years",
        "perspective": "realistic",
        "llmConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        },
        "metadata": {
          "project": "Future Technology Research",
          "researcher": "Dr. Jane Smith"
        }
      },
      "response": {
        "id": "response-2",
        "query": "How will quantum computing change the future?",
        "response": "Quantum computing will revolutionize...",
        "confidence": 0.82,
        "reasoning": "Generated using advanced predictive algorithms",
        "sources": ["Historical patterns", "Trend analysis"],
        "generatedAt": "2025-01-27T10:30:00Z",
        "usedConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        }
      },
      "insights": [
        "Generated with 82% confidence",
        "Used Ollama llama2",
        "Response generated at 2025-01-27 10:30:00",
        "Based on 2 sources"
      ],
      "nextSteps": [
        "Review the future knowledge response carefully",
        "Consider the confidence level and reasoning",
        "Integrate insights into your planning",
        "Track how predictions unfold over time",
        "Share insights with relevant stakeholders"
      ]
    },
    {
      "success": true,
      "message": "Future knowledge generated successfully",
      "query": {
        "id": "query-3",
        "query": "What are the implications of AGI for humanity?",
        "context": "Research project on future technologies",
        "timeHorizon": "5 years",
        "perspective": "realistic",
        "llmConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        },
        "metadata": {
          "project": "Future Technology Research",
          "researcher": "Dr. Jane Smith"
        }
      },
      "response": {
        "id": "response-3",
        "query": "What are the implications of AGI for humanity?",
        "response": "The implications of AGI for humanity are profound...",
        "confidence": 0.78,
        "reasoning": "Generated using advanced predictive algorithms",
        "sources": ["Historical patterns", "Trend analysis"],
        "generatedAt": "2025-01-27T10:30:00Z",
        "usedConfig": {
          "id": "ollama-local",
          "name": "Local Ollama (Default)",
          "provider": "Ollama",
          "model": "llama2"
        }
      },
      "insights": [
        "Generated with 78% confidence",
        "Used Ollama llama2",
        "Response generated at 2025-01-27 10:30:00",
        "Based on 2 sources"
      ],
      "nextSteps": [
        "Review the future knowledge response carefully",
        "Consider the confidence level and reasoning",
        "Integrate insights into your planning",
        "Track how predictions unfold over time",
        "Share insights with relevant stakeholders"
      ]
    }
  ],
  "successCount": 3,
  "failureCount": 0
}
```

## Example 5: LLM Configuration Management

### Get All Configurations
```bash
curl -X GET http://localhost:5000/llm/configs
```

### Response
```json
{
  "success": true,
  "message": "Retrieved 4 LLM configurations",
  "configs": [
    {
      "id": "ollama-local",
      "name": "Local Ollama (Default)",
      "provider": "Ollama",
      "model": "llama2",
      "apiKey": "",
      "baseUrl": "http://localhost:11434",
      "maxTokens": 2000,
      "temperature": 0.7,
      "topP": 0.9,
      "parameters": {
        "format": "json",
        "stream": false
      }
    },
    {
      "id": "ollama-llama3",
      "name": "Ollama Llama3",
      "provider": "Ollama",
      "model": "llama3",
      "apiKey": "",
      "baseUrl": "http://localhost:11434",
      "maxTokens": 2000,
      "temperature": 0.7,
      "topP": 0.9,
      "parameters": {
        "format": "json",
        "stream": false
      }
    },
    {
      "id": "openai-gpt4",
      "name": "OpenAI GPT-4",
      "provider": "OpenAI",
      "model": "gpt-4",
      "apiKey": "",
      "baseUrl": "https://api.openai.com/v1",
      "maxTokens": 2000,
      "temperature": 0.7,
      "topP": 0.9,
      "parameters": {
        "frequency_penalty": 0.0,
        "presence_penalty": 0.0
      }
    },
    {
      "id": "anthropic-claude",
      "name": "Anthropic Claude",
      "provider": "Anthropic",
      "model": "claude-3-sonnet-20240229",
      "apiKey": "",
      "baseUrl": "https://api.anthropic.com",
      "maxTokens": 2000,
      "temperature": 0.7,
      "topP": 0.9,
      "parameters": {}
    }
  ]
}
```

### Create New Configuration
```bash
curl -X POST http://localhost:5000/llm/config \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Custom Ollama Model",
    "provider": "Ollama",
    "model": "custom-model",
    "baseUrl": "http://localhost:11434",
    "maxTokens": 3000,
    "temperature": 0.8,
    "topP": 0.95,
    "parameters": {
      "format": "json",
      "stream": false,
      "customParam": "value"
    }
  }'
```

## Example 6: System Health Check

### Request
```bash
curl -X GET http://localhost:5000/health
```

### Response
```json
{
  "status": "healthy",
  "timestamp": "2025-01-27T10:30:00Z",
  "version": "1.0.0",
  "modules": {
    "core": "healthy",
    "breath": "healthy",
    "future": "healthy",
    "llm-future": "healthy",
    "llm-handler": "healthy",
    "ucore-joy": "healthy"
  },
  "llm": {
    "provider": "Ollama",
    "model": "llama2",
    "status": "connected",
    "baseUrl": "http://localhost:11434"
  },
  "registry": {
    "totalNodes": 6,
    "totalEdges": 0,
    "nodeTypes": {
      "codex.meta/module": 6,
      "codex.future-knowledge": 1
    }
  },
  "system": {
    "memoryUsage": "45.2 MB",
    "cpuUsage": "12.3%",
    "responseTime": "1.2ms"
  }
}
```

## Example 7: Breath Loop Integration

### Expand Phase
```bash
curl -X POST http://localhost:5000/breath/expand \
  -H "Content-Type: application/json" \
  -d '{
    "intention": "Integrate future knowledge into consciousness",
    "frequencies": ["432Hz", "528Hz", "741Hz"],
    "context": {
      "phase": "future-knowledge-integration",
      "priority": "high"
    }
  }'
```

### Validate Phase
```bash
curl -X POST http://localhost:5000/breath/validate \
  -H "Content-Type: application/json" \
  -d '{
    "nodes": ["future-knowledge-1"],
    "resonanceCheck": true,
    "context": {
      "phase": "future-knowledge-integration"
    }
  }'
```

### Contract Phase
```bash
curl -X POST http://localhost:5000/breath/contract \
  -H "Content-Type: application/json" \
  -d '{
    "manifestation": "Crystallize future knowledge into system",
    "context": {
      "phase": "future-knowledge-integration"
    }
  }'
```

## Error Handling Examples

### Invalid LLM Configuration
```json
{
  "success": false,
  "message": "LLM configuration 'invalid-config' not found",
  "error": "ConfigurationNotFound",
  "timestamp": "2025-01-27T10:30:00Z"
}
```

### Ollama Connection Error
```json
{
  "success": false,
  "message": "Failed to connect to Ollama at http://localhost:11434",
  "error": "ConnectionError",
  "details": "Connection refused",
  "timestamp": "2025-01-27T10:30:00Z"
}
```

### Validation Error
```json
{
  "success": false,
  "message": "LLM configuration validation failed: Temperature must be between 0 and 2",
  "error": "ValidationError",
  "field": "temperature",
  "value": 3.0,
  "constraint": "0.0 <= temperature <= 2.0",
  "timestamp": "2025-01-27T10:30:00Z"
}
```

## Complete Workflow Example

```bash
#!/bin/bash

# Complete U-CORE workflow with Ollama integration

echo "🌟 Starting U-CORE Workflow with Ollama Integration 🌟"

# 1. Check system health
echo "1. Checking system health..."
curl -s http://localhost:5000/health | jq '.'

# 2. Query future knowledge
echo "2. Querying future knowledge..."
RESPONSE=$(curl -s -X POST http://localhost:5000/llm/future/query \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What will be the next breakthrough in AI consciousness?",
    "context": "U-CORE system research",
    "timeHorizon": "2 years",
    "perspective": "optimistic",
    "llmConfigId": "ollama-local"
  }')

echo "$RESPONSE" | jq '.'

# 3. Convert response to nodes/edges
echo "3. Converting response to nodes/edges..."
QUERY_RESPONSE=$(echo "$RESPONSE" | jq -r '.response.response')
CONVERSION_RESPONSE=$(curl -s -X POST http://localhost:5000/llm/handler/convert \
  -H "Content-Type: application/json" \
  -d "{
    \"response\": \"$QUERY_RESPONSE\",
    \"responseType\": \"future-knowledge\"
  }")

echo "$CONVERSION_RESPONSE" | jq '.'

# 4. Integrate into bootstrap process
echo "4. Integrating into bootstrap process..."
BOOTSTRAP_RESPONSE=$(curl -s -X POST http://localhost:5000/llm/handler/bootstrap \
  -H "Content-Type: application/json" \
  -d "{
    \"response\": \"$QUERY_RESPONSE\",
    \"responseType\": \"future-knowledge\",
    \"context\": {
      \"bootstrapPhase\": \"future-knowledge-integration\",
      \"priority\": \"high\"
    }
  }")

echo "$BOOTSTRAP_RESPONSE" | jq '.'

# 5. Run breath loop
echo "5. Running breath loop..."
curl -s -X POST http://localhost:5000/breath/expand \
  -H "Content-Type: application/json" \
  -d '{
    "intention": "Integrate future knowledge",
    "frequencies": ["432Hz", "528Hz"]
  }' | jq '.'

curl -s -X POST http://localhost:5000/breath/validate \
  -H "Content-Type: application/json" \
  -d '{
    "resonanceCheck": true
  }' | jq '.'

curl -s -X POST http://localhost:5000/breath/contract \
  -H "Content-Type: application/json" \
  -d '{
    "manifestation": "Crystallize future knowledge"
  }' | jq '.'

# 6. Final system check
echo "6. Final system check..."
curl -s http://localhost:5000/health | jq '.'

echo "✅ U-CORE Workflow completed successfully!"
```

This comprehensive API guide demonstrates how to use the U-CORE system with Ollama LLM integration for future knowledge retrieval, response processing, and bootstrap integration.
