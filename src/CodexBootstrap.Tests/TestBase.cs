using System;
using System.Net.Http;
using CodexBootstrap.Core;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly ICodexLogger Logger;
        protected readonly HttpClient HttpClient;

        protected TestBase()
        {
            Logger = new Log4NetLogger(this.GetType());
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