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
            
            // Use a random port to avoid collisions during parallel test runs
            _port = Random.Shared.Next(5010, 5999);
            Console.WriteLine($"[TestServerFixture] Using port {_port}");
            
            // Start the server as a separate process
            Console.WriteLine($"[TestServerFixture] Starting server process...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --project src/CodexBootstrap/CodexBootstrap.csproj --urls http://localhost:{_port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = "/Users/ursmuff/source/Living-Codex-CSharp" // Set working directory to project root
            };

            // Disable realtime news ingestion during tests to reduce noise and speed up shutdown
            startInfo.EnvironmentVariables["NEWS_INGESTION_ENABLED"] = "false";
            // Enable AI services during integration tests
            startInfo.EnvironmentVariables["DISABLE_AI"] = "false";
            startInfo.EnvironmentVariables["USE_OLLAMA_ONLY"] = "true";
            startInfo.EnvironmentVariables["OLLAMA_HOST"] = "http://localhost:11434";
            // Ensure server binds to the expected port and runs with predictable environment
            startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://localhost:{_port}";
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
            startInfo.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Testing";

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
            var maxWaitTime = TimeSpan.FromSeconds(120);
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
                        // Check if readiness system is available
                        try
                        {
                            var readinessResponse = await testClient.GetAsync($"http://localhost:{_port}/readiness");
                            if (readinessResponse.IsSuccessStatusCode)
                            {
                                serverReady = true;
                                Console.WriteLine($"[TestServerFixture] SUCCESS: Server and readiness system ready on port {_port} after {elapsed.TotalSeconds:F1}s");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"[TestServerFixture] Health check passed but readiness system not ready yet: {readinessResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TestServerFixture] Readiness check failed: {ex.Message}");
                        }
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
                    // macOS/Linux: try SIGINT, then SIGTERM
                    try
                    {
                        Console.WriteLine($"[TestServerFixture] Sending SIGINT to process {_serverProcess.Id}...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "/bin/kill",
                            Arguments = $"-2 {_serverProcess.Id}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        })?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TestServerFixture] Failed to send SIGINT: {ex.Message}");
                    }

                    var exitTask = _serverProcess.WaitForExitAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                    var completed = await Task.WhenAny(exitTask, timeoutTask);

                    if (completed != exitTask && !_serverProcess.HasExited)
                    {
                        try
                        {
                            Console.WriteLine($"[TestServerFixture] Sending SIGTERM to process {_serverProcess.Id}...");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "/bin/kill",
                                Arguments = $"-15 {_serverProcess.Id}",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            })?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TestServerFixture] Failed to send SIGTERM: {ex.Message}");
                        }

                        // Wait a bit after SIGTERM
                        var termExitTask = _serverProcess.WaitForExitAsync();
                        var termTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                        completed = await Task.WhenAny(termExitTask, termTimeoutTask);
                    }

                    if (_serverProcess.HasExited)
                    {
                        Console.WriteLine($"[TestServerFixture] Server process exited gracefully with code {_serverProcess.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine($"[TestServerFixture] Graceful signals failed, falling back to Kill...");

                        // Fallback: attempt to close main window (no-op for console apps) then kill
                        if (!_serverProcess.CloseMainWindow())
                        {
                            Console.WriteLine($"[TestServerFixture] CloseMainWindow failed, forcing kill...");
                            _serverProcess.Kill();
                        }

                        Console.WriteLine($"[TestServerFixture] Waiting for server process to exit (max 10s)...");
                        var forceExitTask = _serverProcess.WaitForExitAsync();
                        var forceTimeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                        var forceCompletedTask = await Task.WhenAny(forceExitTask, forceTimeoutTask);

                        if (forceCompletedTask == forceExitTask)
                        {
                            Console.WriteLine($"[TestServerFixture] Server process killed with code {_serverProcess.ExitCode}");
                        }
                        else
                        {
                            Console.WriteLine($"[TestServerFixture] Server process did not exit within 10s, forcing kill of process tree...");
                            _serverProcess.Kill(true);
                            var treeExitTask = _serverProcess.WaitForExitAsync();
                            var treeTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                            var treeCompletedTask = await Task.WhenAny(treeExitTask, treeTimeoutTask);
                            if (treeCompletedTask == treeExitTask)
                            {
                                Console.WriteLine($"[TestServerFixture] Server process tree killed with code {_serverProcess.ExitCode}");
                            }
                            else
                            {
                                Console.WriteLine($"[TestServerFixture] WARNING: Server process still not exited after forced kill");
                            }
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
