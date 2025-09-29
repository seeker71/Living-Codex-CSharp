using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Ice Node Audit Module - Implements "Keep Ice Tiny" principle
    /// Converts generated/regenerable nodes from Ice to Water state
    /// </summary>
    public class IceNodeAuditModule : ModuleBase
    {
        public override string Name => "Ice Node Audit";
        public override string Description => "Implements 'Keep Ice Tiny' principle by converting generated/regenerable nodes from Ice to Water state";
        public override string Version => "1.0.0";

        public IceNodeAuditModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
        }

        public override Node GetModuleNode()
        {
            return new Node(
                Id: "ice-node-audit",
                TypeId: "codex.meta/module",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Ice Node Audit Module",
                Description: "Implements 'Keep Ice Tiny' principle by converting generated/regenerable nodes from Ice to Water state",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new { 
                        name = Name, 
                        version = Version,
                        description = Description,
                        purpose = "Ice node audit and migration"
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = Name,
                    ["version"] = Version,
                    ["description"] = Description,
                    ["purpose"] = "Ice node audit and migration"
                }
            );
        }

        /// <summary>
        /// Perform comprehensive Ice node audit and migration
        /// </summary>
        [ApiRoute("POST", "/ice-audit/migrate", "MigrateIceNodes", "Migrate regenerable Ice nodes to Water state", "codex.ice-audit")]
        public async Task<object> MigrateIceNodesAsync()
        {
            try
            {
                _logger.Info("Starting Ice node audit and migration...");

                // Get all Ice nodes from registry
                var iceNodes = await _registry.GetNodesByStateAsync(ContentState.Ice);
                _logger.Info($"Found {iceNodes.Count()} Ice nodes to audit");

                var migrationResults = new List<MigrationResult>();
                var nodesToMigrate = new List<Node>();
                var nodesToKeep = new List<Node>();

                // Categorize nodes
                foreach (var node in iceNodes)
                {
                    var category = CategorizeNode(node);
                    
                    if (category.ShouldMigrate)
                    {
                        nodesToMigrate.Add(node);
                        migrationResults.Add(new MigrationResult
                        {
                            NodeId = node.Id,
                            TypeId = node.TypeId,
                            CurrentState = node.State,
                            NewState = ContentState.Water,
                            Reason = category.Reason,
                            Category = category.Category
                        });
                    }
                    else
                    {
                        nodesToKeep.Add(node);
                        migrationResults.Add(new MigrationResult
                        {
                            NodeId = node.Id,
                            TypeId = node.TypeId,
                            CurrentState = node.State,
                            NewState = node.State,
                            Reason = category.Reason,
                            Category = category.Category
                        });
                    }
                }

                _logger.Info($"Categorized: {nodesToMigrate.Count()} to migrate, {nodesToKeep.Count()} to keep as Ice");

                // Perform migration
                var migratedCount = 0;
                var errors = new List<string>();

                foreach (var node in nodesToMigrate)
                {
                    try
                    {
                        // Create Water version
                        var waterNode = new Node(
                            Id: node.Id,
                            TypeId: node.TypeId,
                            State: ContentState.Water,
                            Locale: node.Locale,
                            Title: node.Title,
                            Description: node.Description,
                            Content: node.Content,
                            Meta: node.Meta
                        );

                        // Update registry (this will handle storage backend migration)
                        _registry.Upsert(waterNode);
                        
                        migratedCount++;
                        _logger.Debug($"Migrated node {node.Id} from Ice to Water");
                    }
                    catch (Exception ex)
                    {
                        var error = $"Failed to migrate node {node.Id}: {ex.Message}";
                        errors.Add(error);
                        _logger.Error(error, ex);
                    }
                }

                var result = new IceAuditResult
                {
                    TotalNodesAudited = iceNodes.Count(),
                    NodesMigrated = migratedCount,
                    NodesKeptAsIce = nodesToKeep.Count,
                    MigrationResults = migrationResults,
                    Errors = errors,
                    Success = errors.Count == 0
                };

                _logger.Info($"Ice node migration completed: {migratedCount} migrated, {nodesToKeep.Count()} kept as Ice, {errors.Count()} errors");

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ice node audit failed: {ex.Message}", ex);
                return new ErrorResponse($"Ice node audit failed: {ex.Message}", ErrorCodes.INTERNAL_ERROR, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit report without performing migration
        /// </summary>
        [ApiRoute("GET", "/ice-audit/report", "GetIceAuditReport", "Get Ice node audit report without migration", "codex.ice-audit")]
        public async Task<object> GetIceAuditReportAsync()
        {
            try
            {
                // Get all Ice nodes from registry
                var iceNodes = await _registry.GetNodesByStateAsync(ContentState.Ice);
                var migrationResults = new List<MigrationResult>();

                foreach (var node in iceNodes)
                {
                    var category = CategorizeNode(node);
                    migrationResults.Add(new MigrationResult
                    {
                        NodeId = node.Id,
                        TypeId = node.TypeId,
                        CurrentState = node.State,
                        NewState = category.ShouldMigrate ? ContentState.Water : node.State,
                        Reason = category.Reason,
                        Category = category.Category
                    });
                }

                var report = new IceAuditReport
                {
                    TotalNodes = iceNodes.Count(),
                    NodesToMigrate = migrationResults.Count(r => r.NewState == ContentState.Water),
                    NodesToKeep = migrationResults.Count(r => r.NewState == ContentState.Ice),
                    MigrationResults = migrationResults,
                    GeneratedAt = DateTime.UtcNow
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to generate audit report: {ex.Message}", ex);
                return new ErrorResponse($"Failed to generate audit report: {ex.Message}", ErrorCodes.INTERNAL_ERROR, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Categorize a node to determine if it should be migrated
        /// </summary>
        private NodeCategory CategorizeNode(Node node)
        {
            // 1. GENERATED META-NODES - Should be Water
            if (node.TypeId.StartsWith("codex.meta/"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = true,
                    Category = "Generated Meta-Node",
                    Reason = "Generated from code reflection, can be regenerated"
                };
            }

            // 2. USER-GENERATED CONTENT - Should be Water (unless unique intellectual property)
            if (node.TypeId.StartsWith("codex.concept") && !node.TypeId.Contains("ucore"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = true,
                    Category = "User-Generated Concept",
                    Reason = "User-created concept, can be regenerated from user input"
                };
            }

            if (node.TypeId.StartsWith("codex.user") || node.TypeId.StartsWith("codex.session"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = true,
                    Category = "User Identity/Session",
                    Reason = "User account/session, can be regenerated from auth provider"
                };
            }

            // 3. NEWS/EXTERNAL CONTENT - Should be Water
            if (node.TypeId.StartsWith("codex.news") || 
                node.TypeId.StartsWith("codex.content") ||
                node.TypeId.StartsWith("codex.embedding"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = true,
                    Category = "External Content",
                    Reason = "External data that can be regenerated from sources"
                };
            }

            // 4. CONFIGURATION - Should be Water
            if (node.TypeId.StartsWith("codex.config") || 
                node.TypeId.StartsWith("codex.image.config"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = true,
                    Category = "Configuration",
                    Reason = "System configuration that can be regenerated"
                };
            }

            // 5. FILE SYSTEM - Should be Water (unless core system files)
            if (node.TypeId.StartsWith("codex.file/"))
            {
                // Keep core system files as Ice, migrate others
                if (IsCoreSystemFile(node.Id))
                {
                    return new NodeCategory
                    {
                        ShouldMigrate = false,
                        Category = "Core System File",
                        Reason = "Core system file, essential for operation"
                    };
                }
                else
                {
                    return new NodeCategory
                    {
                        ShouldMigrate = true,
                        Category = "User File",
                        Reason = "User-generated file, can be regenerated"
                    };
                }
            }

            // 6. CORE ONTOLOGY - Keep as Ice
            if (node.TypeId.StartsWith("codex.ontology") || 
                node.TypeId.StartsWith("codex.ucore") ||
                node.TypeId.StartsWith("ucore.") ||
                node.TypeId.StartsWith("codex.relationship"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = false,
                    Category = "Core Ontology",
                    Reason = "Core system ontology, essential for operation"
                };
            }

            // 7. RELATIONS - Keep as Ice (core system)
            if (node.TypeId.StartsWith("codex.relations"))
            {
                return new NodeCategory
                {
                    ShouldMigrate = false,
                    Category = "Core Relations",
                    Reason = "Core system relations, essential for operation"
                };
            }

            // Default: Keep as Ice (conservative approach)
            return new NodeCategory
            {
                ShouldMigrate = false,
                Category = "Unknown",
                Reason = "Unknown node type, keeping as Ice for safety"
            };
        }

        /// <summary>
        /// Check if a file is a core system file
        /// </summary>
        private bool IsCoreSystemFile(string nodeId)
        {
            var coreSystemFiles = new[]
            {
                "Program.cs",
                "Program.Partial.cs",
                "appsettings",
                "log4net.config",
                "hotreload.json"
            };

            return coreSystemFiles.Any(coreFile => nodeId.Contains(coreFile));
        }
    }

    /// <summary>
    /// Node categorization result
    /// </summary>
    public class NodeCategory
    {
        public bool ShouldMigrate { get; set; }
        public string Category { get; set; } = "";
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Migration result for a single node
    /// </summary>
    public class MigrationResult
    {
        public string NodeId { get; set; } = "";
        public string TypeId { get; set; } = "";
        public ContentState CurrentState { get; set; }
        public ContentState NewState { get; set; }
        public string Reason { get; set; } = "";
        public string Category { get; set; } = "";
    }

    /// <summary>
    /// Ice audit result
    /// </summary>
    public class IceAuditResult
    {
        public int TotalNodesAudited { get; set; }
        public int NodesMigrated { get; set; }
        public int NodesKeptAsIce { get; set; }
        public List<MigrationResult> MigrationResults { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; }
    }

    /// <summary>
    /// Ice audit report (without migration)
    /// </summary>
    public class IceAuditReport
    {
        public int TotalNodes { get; set; }
        public int NodesToMigrate { get; set; }
        public int NodesToKeep { get; set; }
        public List<MigrationResult> MigrationResults { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
