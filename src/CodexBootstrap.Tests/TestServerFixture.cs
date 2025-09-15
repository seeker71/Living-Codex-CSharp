using System;
using System.Diagnostics;
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
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _httpClient;

        public HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("Server not started");
        public string BaseUrl => "http://localhost";

        public async Task InitializeAsync()
        {
            // Create the test server factory without trying to change URLs
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Add any test-specific services here
                        services.AddLogging(logging => logging.AddConsole());
                    });
                });

            // Create HTTP client
            _httpClient = _factory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            _factory?.Dispose();
        }
    }
}
