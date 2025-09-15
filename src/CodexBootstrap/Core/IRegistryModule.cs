using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for modules that need access to the global NodeRegistry
/// This ensures all modules use the same global registry instance
/// </summary>
public interface IRegistryModule
{
    /// <summary>
    /// Sets the global registry for this module
    /// This should be called by the ModuleLoader after module creation
    /// </summary>
    /// <param name="registry">The global NodeRegistry instance</param>
    void SetGlobalRegistry(NodeRegistry registry);
}
