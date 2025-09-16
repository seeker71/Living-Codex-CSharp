using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Direct HTTP endpoints module for storage operations with comprehensive validation and error handling
/// </summary>
public sealed class StorageEndpointsModule : ModuleBase
{
    private readonly IStorageBackend? _storageBackend;
    private readonly ICacheManager? _cacheManager;

    public override string Name => "Storage Endpoints Module";
    public override string Description => "Direct HTTP endpoints module for storage operations with comprehensive validation and error handling";
    public override string Version => "1.0.0";

    public StorageEndpointsModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, IStorageBackend? storageBackend = null, ICacheManager? cacheManager = null) 
        : base(registry, logger)
    {
        _storageBackend = storageBackend;
        _cacheManager = cacheManager;
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.storage-endpoints",
            name: "Storage Endpoints Module",
            version: "0.1.0",
            description: "Direct HTTP endpoints for storage operations with comprehensive validation",
            tags: new[] { "storage", "endpoints", "validation", "http", "data-management" },
            capabilities: new[] { "get_node", "create_node", "update_node", "delete_node", "list_nodes", "search_nodes", "get_edge", "create_edge", "update_edge", "delete_edge", "list_edges", "search_edges", "get_storage_stats", "backup_storage", "restore_storage", "validate_storage", "optimize_storage" },
            spec: "codex.spec.storage-endpoints"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Storage Endpoints API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Node Management API Methods
    [ApiRoute("GET", "/storage-endpoints/nodes/{id}", "GetNode", "Get a specific node by ID", "codex.storage-endpoints")]
    public async Task<object> GetNodeAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Node ID is required");
            }

            if (!_registry.TryGet(id, out var node))
            {
                return new ErrorResponse("Node not found");
            }

            _logger.Debug($"Retrieved node: {id}");
            return new { success = true, node = node };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting node {id}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get node: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage-endpoints/nodes", "CreateNode", "Create a new node", "codex.storage-endpoints")]
    public async Task<object> CreateNodeAsync([ApiParameter("body", "Node creation request")] CreateNodeRequest request)
    {
        try
        {
            var validator = new CreateNodeRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return new ErrorResponse($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            var node = new Node(
                Id: request.Id ?? GenerateNodeId(),
                TypeId: request.TypeId,
                State: request.State ?? ContentState.Ice,
                Locale: request.Locale,
                Title: request.Title,
                Description: request.Description,
                Content: request.Content,
                Meta: request.Meta ?? new Dictionary<string, object>()
            );

            _registry.Upsert(node);

            // Persist to storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.StoreNodeAsync(node);
            }

            _logger.Info($"Created node: {node.Id}");
            return new { success = true, node = node };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating node: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create node: {ex.Message}");
        }
    }

    [ApiRoute("PUT", "/storage-endpoints/nodes/{id}", "UpdateNode", "Update an existing node", "codex.storage-endpoints")]
    public async Task<object> UpdateNodeAsync(string id, [ApiParameter("body", "Node update request")] UpdateNodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Node ID is required");
            }

            if (!_registry.TryGet(id, out var existingNode))
            {
                return new ErrorResponse("Node not found");
            }

            var validator = new UpdateNodeRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return new ErrorResponse($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            var updatedNode = existingNode with
            {
                TypeId = request.TypeId ?? existingNode.TypeId,
                State = request.State ?? existingNode.State,
                Locale = request.Locale ?? existingNode.Locale,
                Title = request.Title ?? existingNode.Title,
                Description = request.Description ?? existingNode.Description,
                Content = request.Content ?? existingNode.Content,
                Meta = request.Meta ?? existingNode.Meta
            };

            _registry.Upsert(updatedNode);

            // Persist to storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.StoreNodeAsync(updatedNode);
            }

            _logger.Info($"Updated node: {id}");
            return new { success = true, node = updatedNode };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating node {id}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to update node: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/storage-endpoints/nodes/{id}", "DeleteNode", "Delete a node", "codex.storage-endpoints")]
    public async Task<object> DeleteNodeAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Node ID is required");
            }

            if (!_registry.TryGet(id, out var node))
            {
                return new ErrorResponse("Node not found");
            }

            _registry.RemoveNode(id);

            // Remove from storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.DeleteNodeAsync(id);
            }

            _logger.Info($"Deleted node: {id}");
            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting node {id}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to delete node: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/storage-endpoints/nodes", "ListNodes", "List nodes with optional filtering", "codex.storage-endpoints")]
    public async Task<object> ListNodesAsync([ApiParameter("query", "Query parameters")] NodeListQuery? query = null)
    {
        try
        {
            // Debug: Check if _registry is null
            if (_registry == null)
            {
                return new ErrorResponse("Registry is null in StorageEndpointsModule");
            }
            
            // Handle null query parameter
            query ??= new NodeListQuery();
            
            var nodes = _registry.AllNodes().AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.TypeId))
            {
                nodes = nodes.Where(n => n.TypeId == query.TypeId);
            }

            if (query.State.HasValue)
            {
                nodes = nodes.Where(n => n.State == query.State.Value);
            }

            if (!string.IsNullOrEmpty(query.Locale))
            {
                nodes = nodes.Where(n => n.Locale == query.Locale);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchLower = query.SearchTerm.ToLowerInvariant();
                nodes = nodes.Where(n => 
                    (n.Title?.ToLowerInvariant().Contains(searchLower) == true) ||
                    (n.Description?.ToLowerInvariant().Contains(searchLower) == true));
            }

            // Apply pagination
            var totalCount = nodes.Count();
            var pagedNodes = nodes
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Listed {pagedNodes.Count} nodes (total: {totalCount})");
            return new { 
                success = true, 
                nodes = pagedNodes, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing nodes: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list nodes: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage-endpoints/nodes/search", "SearchNodes", "Search nodes with advanced criteria", "codex.storage-endpoints")]
    public async Task<object> SearchNodesAsync([ApiParameter("body", "Search criteria")] NodeSearchRequest request)
    {
        try
        {
            var validator = new NodeSearchRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return new ErrorResponse($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            var nodes = _registry.AllNodes().AsEnumerable();

            // Apply search criteria
            if (request.TypeIds?.Any() == true)
            {
                nodes = nodes.Where(n => request.TypeIds.Contains(n.TypeId));
            }

            if (request.States?.Any() == true)
            {
                nodes = nodes.Where(n => request.States.Contains(n.State));
            }

            if (request.Locales?.Any() == true)
            {
                nodes = nodes.Where(n => request.Locales.Contains(n.Locale));
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLowerInvariant();
                nodes = nodes.Where(n => 
                    (n.Title?.ToLowerInvariant().Contains(searchLower) == true) ||
                    (n.Description?.ToLowerInvariant().Contains(searchLower) == true));
            }

            if (request.MetaFilters?.Any() == true)
            {
                foreach (var filter in request.MetaFilters)
                {
                    foreach (var kvp in filter)
                    {
                        nodes = nodes.Where(n => 
                            n.Meta?.ContainsKey(kvp.Key) == true && 
                            n.Meta[kvp.Key]?.ToString() == kvp.Value);
                    }
                }
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLowerInvariant())
                {
                    case "id":
                        nodes = request.SortDescending ? nodes.OrderByDescending(n => n.Id) : nodes.OrderBy(n => n.Id);
                        break;
                    case "title":
                        nodes = request.SortDescending ? nodes.OrderByDescending(n => n.Title) : nodes.OrderBy(n => n.Title);
                        break;
                    case "typeid":
                        nodes = request.SortDescending ? nodes.OrderByDescending(n => n.TypeId) : nodes.OrderBy(n => n.TypeId);
                        break;
                    case "state":
                        nodes = request.SortDescending ? nodes.OrderByDescending(n => n.State) : nodes.OrderBy(n => n.State);
                        break;
                    default:
                        nodes = nodes.OrderBy(n => n.Id);
                        break;
                }
            }

            // Apply pagination
            var totalCount = nodes.Count();
            var pagedNodes = nodes
                .Skip(request.Skip ?? 0)
                .Take(request.Take ?? 100)
                .ToList();

            _logger.Debug($"Searched nodes: {pagedNodes.Count} results (total: {totalCount})");
            return new { 
                success = true, 
                nodes = pagedNodes, 
                totalCount = totalCount,
                skip = request.Skip ?? 0,
                take = request.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error searching nodes: {ex.Message}", ex);
            return new ErrorResponse($"Failed to search nodes: {ex.Message}");
        }
    }

    // Edge Management API Methods
    [ApiRoute("GET", "/storage-endpoints/edges/{fromId}/{toId}", "GetEdge", "Get a specific edge", "codex.storage-endpoints")]
    public async Task<object> GetEdgeAsync(string fromId, string toId)
    {
        try
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                return new ErrorResponse("FromId and ToId are required");
            }

            var edge = _registry.GetEdge(fromId, toId);
            if (edge == null)
            {
                return new ErrorResponse("Edge not found");
            }

            _logger.Debug($"Retrieved edge: {fromId} -> {toId}");
            return new { success = true, edge = edge };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting edge {fromId} -> {toId}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get edge: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage-endpoints/edges", "CreateEdge", "Create a new edge", "codex.storage-endpoints")]
    public async Task<object> CreateEdgeAsync([ApiParameter("body", "Edge creation request")] CreateEdgeRequest request)
    {
        try
        {
            var validator = new CreateEdgeRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return new ErrorResponse($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            var edge = new Edge(
                FromId: request.FromId,
                ToId: request.ToId,
                Role: request.Role,
                Weight: request.Weight,
                Meta: request.Meta ?? new Dictionary<string, object>()
            );

            _registry.Upsert(edge);

            // Persist to storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.StoreEdgeAsync(edge);
            }

            _logger.Info($"Created edge: {edge.FromId} -> {edge.ToId}");
            return new { success = true, edge = edge };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating edge: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create edge: {ex.Message}");
        }
    }

    [ApiRoute("PUT", "/storage-endpoints/edges/{fromId}/{toId}", "UpdateEdge", "Update an existing edge", "codex.storage-endpoints")]
    public async Task<object> UpdateEdgeAsync(string fromId, string toId, [ApiParameter("body", "Edge update request")] UpdateEdgeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                return new ErrorResponse("FromId and ToId are required");
            }

            var existingEdge = _registry.GetEdge(fromId, toId);
            if (existingEdge == null)
            {
                return new ErrorResponse("Edge not found");
            }

            var validator = new UpdateEdgeRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return new ErrorResponse($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            var updatedEdge = existingEdge with
            {
                Role = request.Role ?? existingEdge.Role,
                Weight = request.Weight ?? existingEdge.Weight,
                Meta = request.Meta ?? existingEdge.Meta
            };

            _registry.Upsert(updatedEdge);

            // Persist to storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.StoreEdgeAsync(updatedEdge);
            }

            _logger.Info($"Updated edge: {fromId} -> {toId}");
            return new { success = true, edge = updatedEdge };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating edge {fromId} -> {toId}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to update edge: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/storage-endpoints/edges/{fromId}/{toId}", "DeleteEdge", "Delete an edge", "codex.storage-endpoints")]
    public async Task<object> DeleteEdgeAsync(string fromId, string toId)
    {
        try
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                return new ErrorResponse("FromId and ToId are required");
            }

            var edge = _registry.GetEdge(fromId, toId);
            if (edge == null)
            {
                return new ErrorResponse("Edge not found");
            }

            _registry.RemoveEdge(fromId, toId);

            // Remove from storage backend if available
            if (_storageBackend != null)
            {
                await _storageBackend.DeleteEdgeAsync(fromId, toId, "");
            }

            _logger.Info($"Deleted edge: {fromId} -> {toId}");
            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting edge {fromId} -> {toId}: {ex.Message}", ex);
            return new ErrorResponse($"Failed to delete edge: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/storage-endpoints/edges", "ListEdges", "List edges with optional filtering", "codex.storage-endpoints")]
    public async Task<object> ListEdgesAsync([ApiParameter("query", "Query parameters")] EdgeListQuery? query = null)
    {
        try
        {
            // Handle null query parameter
            query ??= new EdgeListQuery();
            
            var edges = _registry.AllEdges().AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.FromId))
            {
                edges = edges.Where(e => e.FromId == query.FromId);
            }

            if (!string.IsNullOrEmpty(query.ToId))
            {
                edges = edges.Where(e => e.ToId == query.ToId);
            }

            if (!string.IsNullOrEmpty(query.Role))
            {
                edges = edges.Where(e => e.Role == query.Role);
            }

            if (query.MinWeight.HasValue)
            {
                edges = edges.Where(e => e.Weight >= query.MinWeight.Value);
            }

            if (query.MaxWeight.HasValue)
            {
                edges = edges.Where(e => e.Weight <= query.MaxWeight.Value);
            }

            // Apply pagination
            var totalCount = edges.Count();
            var pagedEdges = edges
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Listed {pagedEdges.Count} edges (total: {totalCount})");
            return new { 
                success = true, 
                edges = pagedEdges, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing edges: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list edges: {ex.Message}");
        }
    }

    // Storage Management API Methods
    [ApiRoute("GET", "/storage-endpoints/stats", "GetStorageStats", "Get storage statistics", "codex.storage-endpoints")]
    public async Task<object> GetStorageStatsAsync()
    {
        try
        {
            var nodeCount = _registry.AllNodes().Count();
            var edgeCount = _registry.AllEdges().Count();
            
            var stats = new
            {
                NodeCount = nodeCount,
                EdgeCount = edgeCount,
                TotalItems = nodeCount + edgeCount,
                StorageBackend = _storageBackend?.GetType().Name ?? "None",
                CacheManager = _cacheManager?.GetType().Name ?? "None",
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger.Debug($"Retrieved storage stats: {nodeCount} nodes, {edgeCount} edges");
            return new { success = true, stats = stats };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting storage stats: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get storage stats: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage-endpoints/backup", "BackupStorage", "Create a backup of the storage", "codex.storage-endpoints")]
    public async Task<object> BackupStorageAsync([ApiParameter("body", "Backup request")] BackupRequest request)
    {
        try
        {
            if (_storageBackend == null)
            {
                return new ErrorResponse("Storage backend not available");
            }

            var backupPath = request.BackupPath ?? $"backup_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            
            // Get all data
            var nodes = _registry.AllNodes().ToList();
            var edges = _registry.AllEdges().ToList();

            var backupData = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                NodeCount = nodes.Count,
                EdgeCount = edges.Count,
                Nodes = nodes,
                Edges = edges
            };

            // Save backup (in a real implementation, this would use the storage backend)
            var backupJson = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(backupPath, backupJson);

            _logger.Info($"Created backup: {backupPath}");
            return new { success = true, backupPath = backupPath, nodeCount = nodes.Count, edgeCount = edges.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating backup: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create backup: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage-endpoints/validate", "ValidateStorage", "Validate storage integrity", "codex.storage-endpoints")]
    public async Task<object> ValidateStorageAsync()
    {
        try
        {
            var issues = new List<string>();
            var nodes = _registry.AllNodes().ToList();
            var edges = _registry.AllEdges().ToList();

            // Check for orphaned edges
            var nodeIds = nodes.Select(n => n.Id).ToHashSet();
            var orphanedEdges = edges.Where(e => !nodeIds.Contains(e.FromId) || !nodeIds.Contains(e.ToId)).ToList();
            
            if (orphanedEdges.Any())
            {
                issues.Add($"Found {orphanedEdges.Count} orphaned edges");
            }

            // Check for duplicate nodes
            var duplicateNodes = nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateNodes.Any())
            {
                issues.Add($"Found {duplicateNodes.Count} duplicate node IDs");
            }

            // Check for duplicate edges
            var duplicateEdges = edges.GroupBy(e => new { e.FromId, e.ToId }).Where(g => g.Count() > 1).ToList();
            if (duplicateEdges.Any())
            {
                issues.Add($"Found {duplicateEdges.Count} duplicate edges");
            }

            var isValid = !issues.Any();
            _logger.Info($"Storage validation completed: {(isValid ? "Valid" : "Issues found")}");
            
            return new { 
                success = true, 
                isValid = isValid, 
                issues = issues,
                nodeCount = nodes.Count,
                edgeCount = edges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error validating storage: {ex.Message}", ex);
            return new ErrorResponse($"Failed to validate storage: {ex.Message}");
        }
    }

    // Helper methods
    private string GenerateNodeId()
    {
        return $"node_{Guid.NewGuid():N}";
    }

    // Data models and validators
    public record CreateNodeRequest(
        string? Id = null,
        [Required] string TypeId = "",
        ContentState? State = null,
        string? Locale = null,
        string? Title = null,
        string? Description = null,
        ContentRef? Content = null,
        Dictionary<string, object>? Meta = null
    );

    public record UpdateNodeRequest(
        string? TypeId = null,
        ContentState? State = null,
        string? Locale = null,
        string? Title = null,
        string? Description = null,
        ContentRef? Content = null,
        Dictionary<string, object>? Meta = null
    );

    public record CreateEdgeRequest(
        [Required] string FromId = "",
        [Required] string ToId = "",
        [Required] string Role = "",
        double? Weight = null,
        Dictionary<string, object>? Meta = null
    );

    public record UpdateEdgeRequest(
        string? Role = null,
        double? Weight = null,
        Dictionary<string, object>? Meta = null
    );

    public record NodeListQuery(
        string? TypeId = null,
        ContentState? State = null,
        string? Locale = null,
        string? SearchTerm = null,
        int? Skip = null,
        int? Take = null
    );

    public record EdgeListQuery(
        string? FromId = null,
        string? ToId = null,
        string? Role = null,
        double? MinWeight = null,
        double? MaxWeight = null,
        int? Skip = null,
        int? Take = null
    );

    public record NodeSearchRequest(
        string[]? TypeIds = null,
        ContentState[]? States = null,
        string[]? Locales = null,
        string? SearchTerm = null,
        Dictionary<string, string>[]? MetaFilters = null,
        string? SortBy = null,
        bool SortDescending = false,
        int? Skip = null,
        int? Take = null
    );

    public record BackupRequest(
        string? BackupPath = null
    );

    // FluentValidation validators
    public class CreateNodeRequestValidator : AbstractValidator<CreateNodeRequest>
    {
        public CreateNodeRequestValidator()
        {
            RuleFor(x => x.TypeId).NotEmpty().WithMessage("TypeId is required");
            RuleFor(x => x.Id).NotEmpty().When(x => x.Id != null).WithMessage("Id cannot be empty");
        }
    }

    public class UpdateNodeRequestValidator : AbstractValidator<UpdateNodeRequest>
    {
        public UpdateNodeRequestValidator()
        {
            RuleFor(x => x.TypeId).NotEmpty().When(x => x.TypeId != null).WithMessage("TypeId cannot be empty");
        }
    }

    public class CreateEdgeRequestValidator : AbstractValidator<CreateEdgeRequest>
    {
        public CreateEdgeRequestValidator()
        {
            RuleFor(x => x.FromId).NotEmpty().WithMessage("FromId is required");
            RuleFor(x => x.ToId).NotEmpty().WithMessage("ToId is required");
            RuleFor(x => x.Role).NotEmpty().WithMessage("Role is required");
            RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue).WithMessage("Weight must be non-negative");
        }
    }

    public class UpdateEdgeRequestValidator : AbstractValidator<UpdateEdgeRequest>
    {
        public UpdateEdgeRequestValidator()
        {
            RuleFor(x => x.Role).NotEmpty().When(x => x.Role != null).WithMessage("Role cannot be empty");
            RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue).WithMessage("Weight must be non-negative");
        }
    }

    public class NodeSearchRequestValidator : AbstractValidator<NodeSearchRequest>
    {
        public NodeSearchRequestValidator()
        {
            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue).WithMessage("Skip must be non-negative");
            RuleFor(x => x.Take).GreaterThan(0).When(x => x.Take.HasValue).WithMessage("Take must be positive");
            RuleFor(x => x.Take).LessThanOrEqualTo(1000).When(x => x.Take.HasValue).WithMessage("Take cannot exceed 1000");
        }
    }
}
