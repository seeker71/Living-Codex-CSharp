using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CodexBootstrap.Tests
{
    public class TestServerFixture : IAsyncLifetime
    {
        private Process? _serverProcess;
        private HttpClient? _httpClient;
        private int _port;

        public HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("Server not started");
        public string BaseUrl => $"http://localhost:{_port}";

        public async Task InitializeAsync()
        {
            Console.WriteLine($"[TestServerFixture] Starting InitializeAsync at {DateTime.UtcNow:HH:mm:ss.fff}");
            
            // Use the hardcoded port that the server is configured to use
            _port = 5002;
            Console.WriteLine($"[TestServerFixture] Using port {_port}");
            
            // Start the server as a separate process
            Console.WriteLine($"[TestServerFixture] Starting server process...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project src/CodexBootstrap/CodexBootstrap.csproj",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = "/Users/ursmuff/source/Living-Codex-CSharp" // Set working directory to project root
            };

            _serverProcess = Process.Start(startInfo);
            if (_serverProcess == null)
            {
                Console.WriteLine($"[TestServerFixture] ERROR: Failed to start server process");
                throw new InvalidOperationException("Failed to start server process");
            }

            Console.WriteLine($"[TestServerFixture] Server process started with PID {_serverProcess.Id}");

            // Capture server output for debugging
            _serverProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"Server Output: {e.Data}");
                }
            };

            _serverProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"Server Error: {e.Data}");
                }
            };

            Console.WriteLine($"[TestServerFixture] Starting output capture...");
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            // Wait for server to start
            var maxWaitTime = TimeSpan.FromSeconds(60);
            var startTime = DateTime.UtcNow;
            var serverReady = false;
            var attemptCount = 0;
            
            Console.WriteLine($"[TestServerFixture] Waiting for server to be ready (max {maxWaitTime.TotalSeconds}s)...");
            
            while (DateTime.UtcNow - startTime < maxWaitTime && !serverReady)
            {
                attemptCount++;
                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine($"[TestServerFixture] Attempt {attemptCount} - Elapsed: {elapsed.TotalSeconds:F1}s");
                
                try
                {
                    Console.WriteLine($"[TestServerFixture] Creating test client...");
                    using var testClient = new HttpClient();
                    testClient.Timeout = TimeSpan.FromSeconds(5);
                    
                    Console.WriteLine($"[TestServerFixture] Making health check request to http://localhost:{_port}/health...");
                    var response = await testClient.GetAsync($"http://localhost:{_port}/health");
                    Console.WriteLine($"[TestServerFixture] Health check response: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        serverReady = true;
                        Console.WriteLine($"[TestServerFixture] SUCCESS: Server is ready on port {_port} after {elapsed.TotalSeconds:F1}s");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Server not ready yet, continue waiting
                    Console.WriteLine($"[TestServerFixture] Health check failed (attempt {attemptCount}): {ex.Message}");
                }
                
                Console.WriteLine($"[TestServerFixture] Waiting 1 second before next attempt...");
                await Task.Delay(1000);
            }
            
            if (!serverReady)
            {
                Console.WriteLine($"[TestServerFixture] ERROR: Server did not start within {maxWaitTime.TotalSeconds} seconds on port {_port}");
                throw new InvalidOperationException($"Server did not start within {maxWaitTime.TotalSeconds} seconds on port {_port}");
            }

            // Create HTTP client for tests
            Console.WriteLine($"[TestServerFixture] Creating HTTP client for tests...");
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri($"http://localhost:{_port}");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            Console.WriteLine($"[TestServerFixture] InitializeAsync completed successfully at {DateTime.UtcNow:HH:mm:ss.fff}");
        }

        public async Task DisposeAsync()
        {
            Console.WriteLine($"[TestServerFixture] Starting DisposeAsync at {DateTime.UtcNow:HH:mm:ss.fff}");
            
            Console.WriteLine($"[TestServerFixture] Disposing HTTP client...");
            _httpClient?.Dispose();
            
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                Console.WriteLine($"[TestServerFixture] Attempting to gracefully stop server process (PID {_serverProcess.Id})...");
                
                try
                {
                    // First try to close the main window (if it has one)
                    if (!_serverProcess.CloseMainWindow())
                    {
                        Console.WriteLine($"[TestServerFixture] CloseMainWindow failed, forcing kill...");
                        _serverProcess.Kill();
                    }
                    
                    // Wait for the process to exit with a timeout
                    Console.WriteLine($"[TestServerFixture] Waiting for server process to exit (max 10s)...");
                    var exitTask = _serverProcess.WaitForExitAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    
                    var completedTask = await Task.WhenAny(exitTask, timeoutTask);
                    
                    if (completedTask == exitTask)
                    {
                        Console.WriteLine($"[TestServerFixture] Server process exited gracefully with code {_serverProcess.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine($"[TestServerFixture] Server process did not exit within 10s, forcing kill...");
                        _serverProcess.Kill(true); // Kill the entire process tree
                        
                        // Wait a bit more for the forced kill
                        var forceExitTask = _serverProcess.WaitForExitAsync();
                        var forceTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                        
                        var forceCompletedTask = await Task.WhenAny(forceExitTask, forceTimeoutTask);
                        
                        if (forceCompletedTask == forceExitTask)
                        {
                            Console.WriteLine($"[TestServerFixture] Server process killed with code {_serverProcess.ExitCode}");
                        }
                        else
                        {
                            Console.WriteLine($"[TestServerFixture] WARNING: Server process still not exited after forced kill");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestServerFixture] Error during server process cleanup: {ex.Message}");
                }
                finally
                {
                    _serverProcess.Dispose();
                }
            }
            else
            {
                Console.WriteLine($"[TestServerFixture] Server process already exited or was null");
            }
            
            Console.WriteLine($"[TestServerFixture] DisposeAsync completed at {DateTime.UtcNow:HH:mm:ss.fff}");
        }

    }
}
