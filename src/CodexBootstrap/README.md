# CodexBootstrap (C#/.NET 8)

Minimal, self‑describing wire API that supports **nodes** (Ice/Water/Gas), **edges**, **spec atoms**, **phase transitions**, **resonance checks**, and **dynamic module routing**. The spec is a **living document**: it unfolds through the API (atoms → spec → prototype → validate → *phase melt/refreeze* → contract), and can be exchanged between nodes.

## Build & Run

```bash
# from repo root
dotnet build CodexBootstrap.sln
ASPNETCORE_URLS=http://localhost:5055 dotnet run --project src/CodexBootstrap
```

## Endpoints (Core)

- `GET /core/atoms` – minimal bootstrap atoms (seed coil)
- `GET /core/spec` – core module spec view
- `POST /nodes` / `GET /nodes/{id}` / `POST /edges` / `GET /edges`
- `POST /hydrate/{id}` – promote Gas/Ice → Water via adapters/synthesizer
- `POST /route` – dynamic API dispatch
- **Phase**: `POST /phase/melt/{id}` → Water, `POST /phase/refreeze/{id}` → Ice
- **Resonance**: `POST /resonance/check` – validate compatibility
- **Spec**: `POST /spec/atoms`, `POST /spec/compose`, `GET /spec/{id}`
- **Breath**: `POST /breath/expand/{id}`, `POST /breath/validate/{id}`, `POST /breath/contract/{id}`
- **One-shot**: `POST /oneshot/apply` – atoms → spec → prototype in one call
- **Reflect**: `GET /reflect/spec/{id}`, `POST /ingest/spec` – spec ⇄ node graph
- **Diff/Patch**: `GET /diff/{id}`, `POST /patch/{targetId}` – git-like deltas
- **Exchange**: `GET /spec/export/{id}`, `POST /spec/import` – fractal sharing
- **Adapters**: `POST /adapters/register` – register new source adapters

## Architecture

- **Nodes**: Core entities with Ice (frozen/spec), Water (mutable), Gas (transient) states
- **Edges**: Relationships between nodes with roles and weights
- **Modules**: Self-describing components with types, APIs, and dependencies
- **Adapters**: Resolve external content (file://, http://, https://)
- **Synthesizers**: Hydrate nodes from descriptions or external sources
- **Phase Engine**: Manage state transitions (melt/refreeze)
- **Resonance**: Check compatibility between nodes/modules
- **Breath Engine**: Expand/validate/contract specifications
- **Spec Registry**: Store and manage module specifications
- **Dynamic Routing**: Self-describing API invocation

## Usage Examples

### Create a Node
```bash
curl -X POST http://localhost:5055/nodes \
  -H "Content-Type: application/json" \
  -d '{
    "id": "my-node",
    "typeId": "custom/type",
    "state": "Gas",
    "title": "My Node",
    "description": "A sample node"
  }'
```

### Hydrate a Node
```bash
curl -X POST http://localhost:5055/hydrate/my-node
```

### Dynamic API Call
```bash
curl -X POST http://localhost:5055/route \
  -H "Content-Type: application/json" \
  -d '{
    "moduleId": "codex.hello",
    "api": "hello",
    "args": {"name": "World"}
  }'
```

### Phase Transition
```bash
# Melt to Water (mutable)
curl -X POST http://localhost:5055/phase/melt/my-node

# Refreeze to Ice (immutable)
curl -X POST http://localhost:5055/phase/refreeze/my-node
```

### Spec Management
```bash
# Get core atoms
curl http://localhost:5055/core/atoms

# Compose spec from atoms
curl -X POST http://localhost:5055/spec/compose \
  -H "Content-Type: application/json" \
  -d '{"id": "my-module", "nodes": [], "edges": []}'
```

## Development

The project uses .NET 8 with minimal dependencies. All core functionality is self-contained in the single solution file. External modules can be loaded from the `./modules/` directory as .dll files.

### Key Features

- **Self-describing**: All APIs expose their own specifications
- **Modular**: Dynamic module loading and registration
- **Extensible**: Adapter pattern for external content sources
- **Stateful**: Phase transitions and resonance checking
- **Fractal**: Spec exchange and reflection capabilities
- **Minimal**: Single-file solution with no external dependencies beyond .NET 8
