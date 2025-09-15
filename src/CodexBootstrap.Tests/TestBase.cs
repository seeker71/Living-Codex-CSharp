using System;
using System.Net.Http;
using CodexBootstrap.Core;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly Mock<CodexBootstrap.Core.ICodexLogger> MockLogger;
        protected readonly HttpClient HttpClient;

        protected TestBase()
        {
            MockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            HttpClient = new HttpClient();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                HttpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}