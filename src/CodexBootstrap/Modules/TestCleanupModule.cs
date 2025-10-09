using System.Text.RegularExpressions;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Test Cleanup Module - TESTING ONLY
/// Provides endpoints to clean up test data between integration tests
/// </summary>
[ApiModule(Name = "TestCleanupModule", Version = "1.0.0", Description = "Test data cleanup endpoints (TESTING ONLY)", Tags = new[] { "test", "cleanup" })]
public class TestCleanupModule : ModuleBase
{
    public override string Name => "Test Cleanup Module";
    public override string Version => "1.0.0";
    public override string Description => "Test data cleanup endpoints (TESTING ONLY)";

    public TestCleanupModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.test.cleanup",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "test", "cleanup", "integration" },
            capabilities: new[] { "test-cleanup", "data-reset" },
            spec: "codex.spec.test-cleanup"
        );
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Only register endpoints in Testing environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Testing" && environment != "Development")
        {
            _logger.Warn("TestCleanupModule is disabled in non-testing environments");
            return;
        }

        _logger.Info("TestCleanupModule registered (TESTING MODE)");
    }

    /// <summary>
    /// Clean up test users based on username pattern
    /// </summary>
    [ApiRoute("POST", "/test/cleanup/users", "CleanupTestUsers", "Delete test users by pattern", "codex.test.cleanup")]
    [AlwaysAvailable] // Allow in degraded mode
    public async Task<IResult> CleanupTestUsersAsync([ApiParameter("body", "Cleanup request")] CleanupRequest request)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment != "Testing" && environment != "Development")
            {
                return Results.BadRequest(new { success = false, error = "Test cleanup only available in Testing/Development environments" });
            }

            var pattern = request.Pattern ?? "^(testuser|test_)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            
            var allUsers = _registry.GetNodesByType("codex.identity.user");
            var testUsers = allUsers.Where(u => {
                var username = u.Meta?.GetValueOrDefault("username")?.ToString() ?? "";
                return regex.IsMatch(username);
            }).ToList();
            
            var deletedCount = 0;
            foreach (var user in testUsers)
            {
                try
                {
                    _registry.RemoveNode(user.Id);
                    deletedCount++;
                    _logger.Info($"Deleted test user: {user.Id}");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to delete user {user.Id}: {ex.Message}");
                }
            }
            
            return Results.Ok(new { success = true, deletedCount = deletedCount, pattern = pattern });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error cleaning up test users: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
        }
    }

    /// <summary>
    /// Clean up test concepts based on ID pattern
    /// </summary>
    [ApiRoute("POST", "/test/cleanup/concepts", "CleanupTestConcepts", "Delete test concepts by pattern", "codex.test.cleanup")]
    [AlwaysAvailable]
    public async Task<IResult> CleanupTestConceptsAsync([ApiParameter("body", "Cleanup request")] CleanupRequest request)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment != "Testing" && environment != "Development")
            {
                return Results.BadRequest(new { success = false, error = "Test cleanup only available in Testing/Development environments" });
            }

            var pattern = request.Pattern ?? "test";
            var allConcepts = _registry.GetNodesByType("codex.concept");
            var extractedConcepts = _registry.GetNodesByType("codex.concept.extracted");
            var allConceptNodes = allConcepts.Concat(extractedConcepts).ToList();
            
            var testConcepts = allConceptNodes.Where(c => 
                c.Id.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                (c.Title?.Contains(pattern, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            
            var deletedCount = 0;
            foreach (var concept in testConcepts)
            {
                try
                {
                    _registry.RemoveNode(concept.Id);
                    deletedCount++;
                    _logger.Info($"Deleted test concept: {concept.Id}");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to delete concept {concept.Id}: {ex.Message}");
                }
            }
            
            return Results.Ok(new { success = true, deletedCount = deletedCount, pattern = pattern });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error cleaning up test concepts: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
        }
    }

    /// <summary>
    /// Clean up test edges based on pattern
    /// </summary>
    [ApiRoute("POST", "/test/cleanup/edges", "CleanupTestEdges", "Delete test edges by pattern", "codex.test.cleanup")]
    [AlwaysAvailable]
    public async Task<IResult> CleanupTestEdgesAsync([ApiParameter("body", "Cleanup request")] CleanupRequest request)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment != "Testing" && environment != "Development")
            {
                return Results.BadRequest(new { success = false, error = "Test cleanup only available in Testing/Development environments" });
            }

            var pattern = request.Pattern ?? "test";
            var allEdges = _registry.AllEdges();
            
            var testEdges = allEdges.Where(e => 
                e.FromId.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                e.ToId.Contains(pattern, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            var deletedCount = 0;
            foreach (var edge in testEdges)
            {
                try
                {
                    _registry.RemoveEdge(edge.FromId, edge.ToId);
                    deletedCount++;
                    _logger.Info($"Deleted test edge: {edge.FromId} -> {edge.ToId}");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to delete edge {edge.FromId} -> {edge.ToId}: {ex.Message}");
                }
            }
            
            return Results.Ok(new { success = true, deletedCount = deletedCount, pattern = pattern });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error cleaning up test edges: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
        }
    }

    /// <summary>
    /// Reset entire test database (nuclear option)
    /// </summary>
    [ApiRoute("POST", "/test/reset", "ResetTestDatabase", "Reset entire test database", "codex.test.cleanup")]
    [AlwaysAvailable]
    public async Task<IResult> ResetTestDatabaseAsync()
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment != "Testing")
            {
                return Results.BadRequest(new { success = false, error = "Database reset only available in Testing environment" });
            }

            _logger.Warn("⚠️ RESETTING TEST DATABASE ⚠️");
            
            // Clean all test data
            var usersResult = await CleanupTestUsersAsync(new CleanupRequest { Pattern = ".*" });
            var conceptsResult = await CleanupTestConceptsAsync(new CleanupRequest { Pattern = ".*" });
            var edgesResult = await CleanupTestEdgesAsync(new CleanupRequest { Pattern = ".*" });
            
            return Results.Ok(new { 
                success = true, 
                message = "Test database reset complete",
                users = usersResult,
                concepts = conceptsResult,
                edges = edgesResult
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error resetting test database: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
        }
    }
}

/// <summary>
/// Cleanup request model
/// </summary>
[MetaNode(Id = "codex.test.cleanup-request", Name = "Cleanup Request", Description = "Request to clean up test data")]
public record CleanupRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("pattern")] string? Pattern = null
);

