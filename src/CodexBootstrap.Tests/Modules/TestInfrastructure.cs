using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Test infrastructure for module testing using real storage backends
/// </summary>
public static class TestInfrastructure
{
    /// <summary>
    /// Creates a test NodeRegistry with real storage backends
    /// </summary>
    public static NodeRegistry CreateTestNodeRegistry()
    {
        // Use real storage backends with test-specific configurations
        var iceStorage = new InMemoryIceStorageBackend();
        var waterStorage = new InMemoryWaterStorageBackend();
        var logger = new Log4NetLogger(typeof(TestInfrastructure));
        var registry = new NodeRegistry(iceStorage, waterStorage, logger);
        registry.InitializeAsync().Wait();
        return registry;
    }

    /// <summary>
    /// Creates a test NodeRegistry with SQLite storage backends for persistence testing
    /// </summary>
    public static NodeRegistry CreatePersistentTestNodeRegistry()
    {
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_codex_{Guid.NewGuid():N}.db");
        var iceStorage = new SqliteIceStorageBackend($"Data Source={testDbPath}");
        var waterStorage = new SqliteWaterStorageBackend($"Data Source={Path.GetTempPath()}/test_water_{Guid.NewGuid():N}.db");
        var logger = new Log4NetLogger(typeof(TestInfrastructure));
        var registry = new NodeRegistry(iceStorage, waterStorage, logger);
        registry.InitializeAsync().Wait();
        return registry;
    }

    /// <summary>
    /// Creates a real logger for testing
    /// </summary>
    public static ICodexLogger CreateTestLogger()
    {
        return new Log4NetLogger(typeof(TestInfrastructure));
    }
}

