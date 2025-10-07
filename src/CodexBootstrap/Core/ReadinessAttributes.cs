using System;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Attribute to mark API endpoints with readiness requirements
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ReadinessRequiredAttribute : Attribute
    {
        /// <summary>
        /// The module that must be ready for this endpoint to work
        /// </summary>
        public string RequiredModule { get; set; } = string.Empty;

        /// <summary>
        /// Custom readiness message when endpoint is not ready
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether to wait for the module to be ready (vs just checking)
        /// </summary>
        public bool WaitForReady { get; set; } = false;

        /// <summary>
        /// Timeout for waiting (in seconds)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        public ReadinessRequiredAttribute(string requiredModule = "", string message = "")
        {
            RequiredModule = requiredModule;
            Message = message;
        }
    }

    /// <summary>
    /// Attribute to mark endpoints as always available (skip readiness checks)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AlwaysAvailableAttribute : Attribute
    {
        public string Reason { get; set; } = "Always available";

        public AlwaysAvailableAttribute(string reason = "Always available")
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Attribute to mark endpoints as degraded when module is not ready
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DegradedWhenNotReadyAttribute : Attribute
    {
        /// <summary>
        /// The module that affects this endpoint's functionality
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Message to show when in degraded mode
        /// </summary>
        public string DegradedMessage { get; set; } = "Limited functionality available";

        public DegradedWhenNotReadyAttribute(string module = "", string degradedMessage = "")
        {
            Module = module;
            DegradedMessage = degradedMessage;
        }
    }
}


