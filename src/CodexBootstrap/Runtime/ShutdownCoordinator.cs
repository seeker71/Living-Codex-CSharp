using Microsoft.Extensions.Hosting;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Coordinates graceful shutdown of all background tasks and resources
/// </summary>
public sealed class ShutdownCoordinator : IHostedService, IDisposable
{
    private readonly ICodexLogger _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly List<Func<Task>> _shutdownHandlers = new();
    private readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(30);
    private bool _isShuttingDown = false;
    
    /// <summary>
    /// Cancellation token that signals when application shutdown is initiated
    /// </summary>
    public CancellationToken ShutdownToken => _shutdownCts.Token;
    
    public ShutdownCoordinator(
        ICodexLogger logger, 
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register for shutdown notification
        _lifetime.ApplicationStopping.Register(() =>
        {
            _isShuttingDown = true;
            _logger.Info("[Shutdown] Graceful shutdown initiated - cancelling all background tasks...");
            _shutdownCts.Cancel();
        });
        
        _logger.Info("[ShutdownCoordinator] Started - ready to coordinate graceful shutdown");
        return Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Info("[Shutdown] Stopping all background services and cleaning up resources...");
        
        var shutdownStart = DateTime.UtcNow;
        
        try
        {
            // Execute custom shutdown handlers first
            foreach (var handler in _shutdownHandlers)
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[Shutdown] Shutdown handler error: {ex.Message}");
                }
            }
            
            // Dispose all registered resources
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[Shutdown] Error disposing resource: {ex.Message}");
                }
            }
            
            // Give tasks a moment to notice cancellation and clean up
            await Task.Delay(1000);
            
            var elapsed = DateTime.UtcNow - shutdownStart;
            _logger.Info($"[Shutdown] Graceful shutdown completed successfully in {elapsed.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            _logger.Error($"[Shutdown] Error during shutdown: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Register a disposable resource for cleanup during shutdown
    /// </summary>
    public void RegisterDisposable(IDisposable disposable)
    {
        if (_isShuttingDown)
        {
            _logger.Warn("[Shutdown] Cannot register disposable during shutdown");
            return;
        }
        
        _disposables.Add(disposable);
    }
    
    /// <summary>
    /// Register a custom shutdown handler
    /// </summary>
    public void RegisterShutdownHandler(Func<Task> handler)
    {
        if (_isShuttingDown)
        {
            _logger.Warn("[Shutdown] Cannot register handler during shutdown");
            return;
        }
        
        _shutdownHandlers.Add(handler);
    }
    
    public void Dispose()
    {
        _shutdownCts?.Dispose();
    }
}


