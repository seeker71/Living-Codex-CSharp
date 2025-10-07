using System;
using System.Collections.Generic;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Represents the readiness state of a component
    /// </summary>
    public enum ReadinessState
    {
        /// <summary>
        /// Component not yet started
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// Component is currently initializing
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Component is fully initialized and operational
        /// </summary>
        Ready,
        
        /// <summary>
        /// Component is operational but with limitations
        /// </summary>
        Degraded,
        
        /// <summary>
        /// Component initialization failed
        /// </summary>
        Failed
    }

    /// <summary>
    /// Result of a readiness check or initialization attempt
    /// </summary>
    public class ReadinessResult
    {
        public ReadinessState State { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan? EstimatedTimeToReady { get; set; }
        public Exception? Exception { get; set; }

        public static ReadinessResult Success(string message = "Ready") => new()
        {
            State = ReadinessState.Ready,
            Message = message
        };

        public static ReadinessResult Initializing(string message = "Initializing") => new()
        {
            State = ReadinessState.Initializing,
            Message = message
        };

        public static ReadinessResult Failed(string message, Exception? exception = null) => new()
        {
            State = ReadinessState.Failed,
            Message = message,
            Exception = exception
        };

        public static ReadinessResult Degraded(string message) => new()
        {
            State = ReadinessState.Degraded,
            Message = message
        };

        public static ReadinessResult NotStarted(string message = "Not started") => new()
        {
            State = ReadinessState.NotStarted,
            Message = message
        };
    }

    /// <summary>
    /// Event arguments for readiness state changes
    /// </summary>
    public class ReadinessChangedEventArgs : EventArgs
    {
        public string ComponentId { get; set; } = string.Empty;
        public ReadinessState PreviousState { get; set; }
        public ReadinessState CurrentState { get; set; }
        public ReadinessResult Result { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Information about a component's readiness
    /// </summary>
    public class ComponentReadiness
    {
        public string ComponentId { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public ReadinessState State { get; set; }
        public ReadinessResult LastResult { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Overall system readiness status
    /// </summary>
    public class SystemReadiness
    {
        public ReadinessState OverallState { get; set; }
        public int TotalComponents { get; set; }
        public int ReadyComponents { get; set; }
        public int InitializingComponents { get; set; }
        public int FailedComponents { get; set; }
        public int DegradedComponents { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<ComponentReadiness> Components { get; set; } = new();
        public bool IsFullyReady => OverallState == ReadinessState.Ready && FailedComponents == 0;
    }
}
