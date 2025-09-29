# FRACTAL_CONCEPT_EXCHANGE Specification

## How to read this spec
- This defines a cross-node exchange protocol. Use it with the main spec’s Status Ledger and Appendix B for concrete endpoints.
- Everything is a Node: protocol objects must round‑trip to nodes and edges.

## Integration Status
- The endpoints listed here are a contract baseline; treat them as Stub/Untested unless present in Appendix B and implemented in modules.
- Real-time sync and multi-dimensional mapping are ExternalInfo until backed by running services.

## Overview
This specification defines the fractal concept exchange system for the Living Codex platform, enabling multi-dimensional concept sharing and resonance across distributed nodes.

## Core Concepts

### Fractal Nodes
- **Type**: `fractal.concept.node`
- **Properties**: 
  - `id`: Unique identifier
  - `dimension`: Fractal dimension (1-∞)
  - `resonance`: Resonance frequency
  - `connections`: Linked concept nodes

### Concept Exchange Protocol
- **Method**: HTTP/WebSocket
- **Format**: JSON with fractal metadata
- **Authentication**: JWT with fractal signatures

## API Endpoints

### GET /fractal/concepts
Retrieve all available fractal concepts

### POST /fractal/concepts
Create a new fractal concept

### PUT /fractal/concepts/{id}
Update an existing fractal concept

### DELETE /fractal/concepts/{id}
Remove a fractal concept

## Data Model

```json
{
  "fractalId": "fractal-123",
  "dimension": 3,
  "resonance": 440.0,
  "concepts": [
    {
      "id": "concept-1",
      "name": "Quantum Entanglement",
      "description": "Non-local correlation between particles",
      "metadata": {
        "category": "physics",
        "complexity": "high",
        "resonance": 0.95
      }
    }
  ],
  "connections": [
    {
      "targetId": "fractal-456",
      "strength": 0.8,
      "type": "resonance"
    }
  ]
}
```

## Implementation Notes
- Uses persistent node registry for storage
- Implements real-time synchronization
- Supports multi-dimensional concept mapping
- Enables cross-fractal concept exchange
