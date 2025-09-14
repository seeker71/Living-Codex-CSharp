using System.Text.Json;

namespace CodexBootstrap.Core
{
    public class PortConfigurationService
    {
        private readonly Dictionary<string, int> _servicePorts;
        private Dictionary<string, object> _configuration;
        private readonly string _environment;

        public PortConfigurationService(string environment = "development")
        {
            _environment = environment;
            _servicePorts = new Dictionary<string, int>();
            _configuration = new Dictionary<string, object>();
            
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "config", "ports.json");
                if (!File.Exists(configPath))
                {
                    // Fallback to default configuration
                    SetDefaultPorts();
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (config != null)
                {
                    _configuration = config;
                    LoadServicePorts();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load port configuration: {ex.Message}");
                SetDefaultPorts();
            }
        }

        private void SetDefaultPorts()
        {
            _servicePorts["codex-bootstrap"] = 5002;
            _servicePorts["codex-ai"] = 5003;
            _servicePorts["codex-storage"] = 5004;
            _servicePorts["codex-events"] = 5005;
            _servicePorts["codex-mobile"] = 5006;
            _servicePorts["codex-admin"] = 5007;
        }

        private void LoadServicePorts()
        {
            if (_configuration.TryGetValue("services", out var servicesObj) && 
                servicesObj is JsonElement servicesElement)
            {
                foreach (var service in servicesElement.EnumerateObject())
                {
                    if (service.Value.TryGetProperty("port", out var portElement) &&
                        portElement.TryGetInt32(out var port))
                    {
                        _servicePorts[service.Name] = port;
                    }
                }
            }
        }

        public int GetPort(string serviceName)
        {
            if (_servicePorts.TryGetValue(serviceName, out var port))
            {
                return port;
            }

            throw new ArgumentException($"Service '{serviceName}' not found in port configuration");
        }

        public string GetUrl(string serviceName, string protocol = "http", string host = "localhost")
        {
            var port = GetPort(serviceName);
            return $"{protocol}://{host}:{port}";
        }

        public Dictionary<string, int> GetAllPorts()
        {
            return new Dictionary<string, int>(_servicePorts);
        }

        public bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int FindAvailablePort(int startPort = 5000, int maxAttempts = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var port = startPort + i;
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            throw new InvalidOperationException($"No available port found starting from {startPort}");
        }

        public void ValidateConfiguration()
        {
            var conflicts = new List<string>();
            var usedPorts = new HashSet<int>();

            foreach (var service in _servicePorts)
            {
                if (usedPorts.Contains(service.Value))
                {
                    conflicts.Add($"Port {service.Value} is used by multiple services");
                }
                else
                {
                    usedPorts.Add(service.Value);
                }

                if (!IsPortAvailable(service.Value))
                {
                    conflicts.Add($"Port {service.Value} for service '{service.Key}' is not available");
                }
            }

            if (conflicts.Any())
            {
                throw new InvalidOperationException($"Port configuration conflicts: {string.Join(", ", conflicts)}");
            }
        }

        public string GetConfigurationSummary()
        {
            var summary = $"Port Configuration (Environment: {_environment})\n";
            summary += "=====================================\n";
            
            foreach (var service in _servicePorts.OrderBy(s => s.Value))
            {
                var available = IsPortAvailable(service.Value) ? "✓" : "✗";
                summary += $"{service.Key,-20} : {service.Value,-5} {available}\n";
            }
            
            return summary;
        }
    }
}
