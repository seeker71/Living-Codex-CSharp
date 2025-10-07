using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Test collection for isolated readiness unit tests
    /// This ensures these tests run in complete isolation from other tests
    /// </summary>
    [CollectionDefinition("IsolatedReadinessTests")]
    public class IsolatedReadinessTestCollection : ICollectionFixture<object>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and ensure
        // these tests run in isolation from other test collections
    }
}


