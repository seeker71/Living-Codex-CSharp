using System;
using System.Threading.Tasks;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Service to track startup state and determine when AI services are ready
    /// </summary>
    [MetaNodeAttribute("codex.core.startup-state", "codex.meta/type", "StartupStateService", "Tracks system startup state")]
    public class StartupStateService
    {
        private volatile bool _isStartupComplete = false;
        private volatile bool _isAIReady = false;
        private readonly ICodexLogger _logger;

        public StartupStateService(ICodexLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets whether the system startup is complete
        /// </summary>
        public bool IsStartupComplete => _isStartupComplete;

        /// <summary>
        /// Gets whether AI services are ready for use
        /// </summary>
        public bool IsAIReady => _isAIReady;

        /// <summary>
        /// Marks the system startup as complete
        /// </summary>
        public void MarkStartupComplete()
        {
            _isStartupComplete = true;
            _logger.Info("[StartupState] System startup marked as complete");
        }

        /// <summary>
        /// Marks AI services as ready for use
        /// </summary>
        public void MarkAIReady()
        {
            _isAIReady = true;
            _logger.Info("[StartupState] AI services marked as ready");
        }

        /// <summary>
        /// Marks AI services as not ready (during startup)
        /// </summary>
        public void MarkAINotReady()
        {
            _isAIReady = false;
            _logger.Info("[StartupState] AI services marked as not ready");
        }

        /// <summary>
        /// Waits for AI services to be ready
        /// </summary>
        public async Task WaitForAIReadyAsync(TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (!_isAIReady && (DateTime.UtcNow - startTime) < timeout)
            {
                await Task.Delay(100);
            }

            if (!_isAIReady)
            {
                throw new TimeoutException($"AI services not ready within {timeout.TotalSeconds} seconds");
            }
        }
    }
}


