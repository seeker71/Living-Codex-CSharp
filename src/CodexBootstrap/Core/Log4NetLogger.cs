using System;
using log4net;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Log4Net implementation of ILogger
    /// </summary>
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _logger;

        public Log4NetLogger(Type type)
        {
            _logger = LogManager.GetLogger(type);
        }

        public Log4NetLogger(string name)
        {
            _logger = LogManager.GetLogger(name);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            _logger.Debug(message, exception);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Info(string message, Exception exception)
        {
            _logger.Info(message, exception);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Warn(string message, Exception exception)
        {
            _logger.Warn(message, exception);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            _logger.Error(message, exception);
        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            _logger.Fatal(message, exception);
        }
    }
}
