using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime
{
    /// <summary>
    /// API controller for readiness status and events
    /// </summary>
    [ApiController]
    [Route("readiness")]
    public class ReadinessController : ControllerBase
    {
        private readonly ReadinessTracker _readinessTracker;
        private readonly ReadinessEventStream _eventStream;
        private readonly ICodexLogger _logger;

        public ReadinessController(ReadinessTracker readinessTracker, ReadinessEventStream eventStream, ICodexLogger logger)
        {
            _readinessTracker = readinessTracker;
            _eventStream = eventStream;
            _logger = logger;
        }

        /// <summary>
        /// Get overall system readiness status
        /// </summary>
        [HttpGet]
        public ActionResult<SystemReadiness> GetSystemReadiness()
        {
            var readiness = _readinessTracker.GetSystemReadiness();
            return Ok(readiness);
        }

        /// <summary>
        /// Get readiness status for all modules
        /// </summary>
        [HttpGet("modules")]
        public ActionResult<IEnumerable<ComponentReadiness>> GetModulesReadiness()
        {
            var modules = _readinessTracker.GetComponentsByType("Module");
            return Ok(modules);
        }

        /// <summary>
        /// Get readiness status for a specific module
        /// </summary>
        [HttpGet("modules/{moduleName}")]
        public ActionResult<ComponentReadiness> GetModuleReadiness(string moduleName)
        {
            var module = _readinessTracker.GetComponentReadiness(moduleName);
            if (module == null)
            {
                return NotFound($"Module '{moduleName}' not found");
            }
            return Ok(module);
        }

        /// <summary>
        /// Get readiness status for all endpoints
        /// </summary>
        [HttpGet("endpoints")]
        public ActionResult<IEnumerable<ComponentReadiness>> GetEndpointsReadiness()
        {
            var endpoints = _readinessTracker.GetComponentsByType("Endpoint");
            return Ok(endpoints);
        }

        /// <summary>
        /// Get readiness status for a specific endpoint
        /// </summary>
        [HttpGet("endpoints/{endpoint}")]
        public ActionResult<ComponentReadiness> GetEndpointReadiness(string endpoint)
        {
            var endpointComponent = _readinessTracker.GetComponentReadiness(endpoint);
            if (endpointComponent == null)
            {
                return NotFound($"Endpoint '{endpoint}' not found");
            }
            return Ok(endpointComponent);
        }

        /// <summary>
        /// Get components that are ready
        /// </summary>
        [HttpGet("ready")]
        public ActionResult<IEnumerable<ComponentReadiness>> GetReadyComponents()
        {
            var readyComponents = _readinessTracker.GetReadyComponents();
            return Ok(readyComponents);
        }

        /// <summary>
        /// Get components that are not ready
        /// </summary>
        [HttpGet("not-ready")]
        public ActionResult<IEnumerable<ComponentReadiness>> GetNotReadyComponents()
        {
            var notReadyComponents = _readinessTracker.GetNotReadyComponents();
            return Ok(notReadyComponents);
        }

        /// <summary>
        /// Server-Sent Events endpoint for real-time readiness notifications
        /// </summary>
        [HttpGet("events")]
        public async Task GetReadinessEvents()
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Headers", "Cache-Control");

            var clientId = Guid.NewGuid().ToString();
            var writer = new StreamWriter(Response.Body, leaveOpen: true);

            try
            {
                _eventStream.AddClient(clientId, writer);

                // Keep connection alive
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(1000, HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in readiness event stream: {ex.Message}", ex);
            }
            finally
            {
                _eventStream.RemoveClient(clientId);
                await writer.DisposeAsync();
            }
        }

        /// <summary>
        /// Wait for a specific component to become ready
        /// </summary>
        [HttpGet("wait/{componentId}")]
        public async Task<ActionResult<ReadinessResult>> WaitForComponent(string componentId, [FromQuery] int timeoutSeconds = 30)
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(timeoutSeconds);
                var result = await _readinessTracker.WaitForComponentAsync(componentId, timeout);
                return Ok(result);
            }
            catch (TimeoutException)
            {
                return StatusCode(408, new { message = $"Component '{componentId}' did not become ready within {timeoutSeconds} seconds" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error waiting for component {componentId}: {ex.Message}", ex);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Wait for the entire system to become ready
        /// </summary>
        [HttpGet("wait-system")]
        public async Task<ActionResult<SystemReadiness>> WaitForSystemReady([FromQuery] int timeoutSeconds = 60)
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(timeoutSeconds);
                var result = await _readinessTracker.WaitForSystemReadyAsync(timeout);
                return Ok(result);
            }
            catch (TimeoutException)
            {
                return StatusCode(408, new { message = $"System did not become ready within {timeoutSeconds} seconds" });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error waiting for system ready: {ex.Message}", ex);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}


