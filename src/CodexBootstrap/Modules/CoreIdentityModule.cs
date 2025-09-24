using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Core Identity Module that ensures the core identity node is created first
/// and all other nodes have proper edges linking back to it
/// </summary>
public sealed class CoreIdentityModule : ModuleBase
{
    private const string CORE_IDENTITY_NODE_ID = "codex.core.identity.root";
    private const string CORE_IDENTITY_TYPE_ID = "codex.core.identity";
    private const string U_CORE_ROOT_NODE_ID = "u-core-ontology-root";
    private const string U_CORE_ROOT_TYPE_ID = "codex.ontology.root";

    public override string Name => "Core Identity Module";
    public override string Description => "Manages the core identity node and ensures all nodes can walk back to it";
    public override string Version => "1.0.0";

    public CoreIdentityModule(INodeRegistry registry, ICodexLogger logger) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.core-identity",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "identity", "core", "root", "graph", "traversal" },
            capabilities: new[] { 
                "create_core_identity", "ensure_identity_edges", "validate_node_traversal",
                "create_ucore_root", "link_nodes_to_identity"
            },
            spec: "codex.spec.core-identity"
        );
    }

    public override void Register(INodeRegistry registry)
    {
        base.Register(registry);
        
        // Ensure core identity node exists first
        EnsureCoreIdentityNode();
        
        // Ensure U-CORE ontology root exists
        EnsureUCoreOntologyRoot();
        
        // Ensure edges between core identity and U-CORE root
        EnsureIdentityUcoreEdges();
        
        _logger.Info("Core Identity Module registered and core identity node ensured");
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
    }

    /// <summary>
    /// Ensures the core identity node exists and is created first
    /// </summary>
    public void EnsureCoreIdentityNode()
    {
        var existingNode = _registry.GetNodeAsync(CORE_IDENTITY_NODE_ID).Result;
        
        if (existingNode == null)
        {
            var coreIdentityNode = CreateCoreIdentityNode();
            _registry.Upsert(coreIdentityNode);
            _logger.Info($"Created core identity node: {CORE_IDENTITY_NODE_ID}");
        }
        else
        {
            _logger.Info($"Core identity node already exists: {CORE_IDENTITY_NODE_ID}");
        }
    }

    /// <summary>
    /// Ensures the U-CORE ontology root node exists
    /// </summary>
    public void EnsureUCoreOntologyRoot()
    {
        var existingNode = _registry.GetNodeAsync(U_CORE_ROOT_NODE_ID).Result;
        
        if (existingNode == null)
        {
            var ucoreRootNode = CreateUCoreOntologyRootNode();
            _registry.Upsert(ucoreRootNode);
            _logger.Info($"Created U-CORE ontology root node: {U_CORE_ROOT_NODE_ID}");
        }
        else
        {
            _logger.Info($"U-CORE ontology root node already exists: {U_CORE_ROOT_NODE_ID}");
        }
    }

    /// <summary>
    /// Ensures edges exist between core identity and U-CORE root
    /// </summary>
    public void EnsureIdentityUcoreEdges()
    {
        var existingEdges = _registry.AllEdges().Where(e => 
            (e.FromId == U_CORE_ROOT_NODE_ID && e.ToId == CORE_IDENTITY_NODE_ID) ||
            (e.FromId == CORE_IDENTITY_NODE_ID && e.ToId == U_CORE_ROOT_NODE_ID)
        ).ToList();

        if (!existingEdges.Any(e => e.FromId == U_CORE_ROOT_NODE_ID && e.ToId == CORE_IDENTITY_NODE_ID))
        {
            var ucoreToIdentityEdge = NodeHelpers.CreateEdge(
                U_CORE_ROOT_NODE_ID,
                CORE_IDENTITY_NODE_ID,
                "belongs_to",
                1.0,
                new Dictionary<string, object>
                {
                    ["relationship"] = "ontology-root-to-identity",
                    ["createdBy"] = "core-identity-module"
                }
            );
            _registry.Upsert(ucoreToIdentityEdge);
            _logger.Info($"Created edge from U-CORE root to core identity");
        }
    }

    /// <summary>
    /// Ensures a node has a path back to the core identity
    /// </summary>
    public async Task<bool> EnsureNodePathToIdentity(string nodeId)
    {
        if (nodeId == CORE_IDENTITY_NODE_ID)
            return true;

        // Check if node can already reach core identity
        if (await CanReachNode(nodeId, CORE_IDENTITY_NODE_ID))
            return true;

        // Try to create a path through U-CORE root
        if (await CanReachNode(nodeId, U_CORE_ROOT_NODE_ID))
        {
            // Node can reach U-CORE root, which should reach core identity
            return true;
        }

        // Create edge from node to U-CORE root
        var nodeToUcoreEdge = NodeHelpers.CreateEdge(
            nodeId,
            U_CORE_ROOT_NODE_ID,
            "belongs_to",
            1.0,
            new Dictionary<string, object>
            {
                ["relationship"] = "node-to-ontology",
                ["createdBy"] = "core-identity-module",
                ["autoCreated"] = true
            }
        );
        _registry.Upsert(nodeToUcoreEdge);
        
        _logger.Info($"Created edge from node {nodeId} to U-CORE root to ensure path to core identity");
        return true;
    }

    /// <summary>
    /// Validates that all nodes in the system can reach the core identity
    /// </summary>
    public async Task<IdentityValidationResult> ValidateAllNodesCanReachIdentity()
    {
        var allNodes = _registry.AllNodes().ToList();
        var coreIdentityNodes = allNodes.Where(n => n.TypeId == CORE_IDENTITY_TYPE_ID).ToList();
        
        if (!coreIdentityNodes.Any())
        {
            return new IdentityValidationResult(
                IsValid: false,
                Message: "No core identity node found",
                NodesWithoutPath: allNodes.Select(n => n.Id).ToList()
            );
        }

        var coreIdentity = coreIdentityNodes.First();
        var nodesWithoutPath = new List<string>();
        
        foreach (var node in allNodes.Where(n => n.Id != coreIdentity.Id))
        {
            var canReachIdentity = await CanReachNode(node.Id, coreIdentity.Id);
            if (!canReachIdentity)
            {
                nodesWithoutPath.Add(node.Id);
            }
        }

        var isValid = nodesWithoutPath.Count == 0;
        var message = isValid 
            ? $"All {allNodes.Count} nodes can reach the core identity"
            : $"{nodesWithoutPath.Count} nodes cannot reach the core identity";

        return new IdentityValidationResult(
            IsValid: isValid,
            Message: message,
            NodesWithoutPath: nodesWithoutPath
        );
    }

    /// <summary>
    /// Creates a comprehensive traversal path from any node back to core identity
    /// </summary>
    public async Task<List<Node>> CreateTraversalPathToIdentity(string startNodeId)
    {
        var visited = new HashSet<string>();
        var path = new List<Node>();
        var queue = new Queue<string>();
        
        queue.Enqueue(startNodeId);
        
        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            
            if (visited.Contains(currentNodeId))
                continue;
                
            visited.Add(currentNodeId);
            
            var currentNode = await _registry.GetNodeAsync(currentNodeId);
            if (currentNode == null)
                continue;
                
            path.Add(currentNode);
            
            // If we've reached the core identity, we're done
            if (currentNode.TypeId == CORE_IDENTITY_TYPE_ID)
                break;
            
            // Add all connected nodes to the queue
            var edges = GetOutgoingEdges(currentNodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.ToId))
                {
                    queue.Enqueue(edge.ToId);
                }
            }
        }
        
        return path;
    }

    #region Private Helper Methods

    private Node CreateCoreIdentityNode()
    {
        return NodeHelpers.CreateNode(
            CORE_IDENTITY_NODE_ID,
            CORE_IDENTITY_TYPE_ID,
            ContentState.Ice,
            "Core Identity Root",
            "The root identity node that all other nodes should link back to",
            NodeHelpers.CreateJsonContent(new
            {
                id = CORE_IDENTITY_NODE_ID,
                name = "Core Identity",
                description = "Root identity node for the Living Codex system",
                created = DateTime.UtcNow,
                version = "1.0.0",
                type = "core-identity",
                isRoot = true
            }),
            new Dictionary<string, object>
            {
                ["isCoreIdentity"] = true,
                ["isRoot"] = true,
                ["systemVersion"] = "1.0.0",
                ["createdBy"] = "core-identity-module",
                ["priority"] = 1
            }
        );
    }

    private Node CreateUCoreOntologyRootNode()
    {
        return NodeHelpers.CreateNode(
            U_CORE_ROOT_NODE_ID,
            U_CORE_ROOT_TYPE_ID,
            ContentState.Ice,
            "U-CORE Ontology Root",
            "Root node for U-CORE ontology system",
            NodeHelpers.CreateJsonContent(new
            {
                id = U_CORE_ROOT_NODE_ID,
                name = "u-core",
                description = "Universal Consciousness Resonance Engine ontology root",
                version = "1.0.0",
                type = "ontology-root"
            }),
            new Dictionary<string, object>
            {
                ["ontologyType"] = "root",
                ["version"] = "1.0.0",
                ["createdBy"] = "core-identity-module",
                ["priority"] = 2
            }
        );
    }

    private async Task<bool> CanReachNode(string fromNodeId, string toNodeId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        
        queue.Enqueue(fromNodeId);
        
        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            
            if (currentNodeId == toNodeId)
                return true;
                
            if (visited.Contains(currentNodeId))
                continue;
                
            visited.Add(currentNodeId);
            
            var edges = GetOutgoingEdges(currentNodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.ToId))
                {
                    queue.Enqueue(edge.ToId);
                }
            }
        }
        
        return false;
    }

    private List<Edge> GetOutgoingEdges(string nodeId)
    {
        var allEdges = _registry.AllEdges().ToList();
        return allEdges.Where(e => e.FromId == nodeId).ToList();
    }

    #endregion
}

/// <summary>
/// Result of identity validation
/// </summary>
public record IdentityValidationResult(
    bool IsValid,
    string Message,
    List<string> NodesWithoutPath
);
