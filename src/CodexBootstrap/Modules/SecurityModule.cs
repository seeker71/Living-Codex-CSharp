using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using System.Text.Json.Serialization;

namespace CodexBootstrap.Modules
{
    [ApiModule(Name = "SecurityModule", Version = "1.0.0", Description = "Authentication and authorization services via REST API")]
    public class SecurityModule : ModuleBase
    {
        private readonly string _userModuleBaseUrl;
        private readonly HttpClient _httpClient;

        public override string Name => "Security Module";
        public override string Description => "Authentication and authorization via REST API calls";
        public override string Version => "1.0.0";

        public SecurityModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
            _httpClient = httpClient;
            _userModuleBaseUrl = "http://localhost:5002"; // Same server, different module
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "codex.security",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "security", "auth", "rest", "api" },
                capabilities: new[] { "authentication", "authorization", "registration", "rest-api" },
                spec: "codex.spec.security"
            );
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            // API handlers are registered via attribute-based routing
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // HTTP endpoints are registered via attribute-based routing
        }


        [ApiRoute("POST", "/auth/register", "register", "Register a new user account", "codex.security")]
        public async Task<object> RegisterAsync([ApiParameter("request", "Registration request")] RegisterRequest request)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return new { success = false, error = "Email, username, and password are required", timestamp = DateTime.UtcNow };
                }

                // Call UserModule via REST API
                var createUserRequest = new UserCreateRequest(
                    Username: request.Username,
                    Email: request.Email,
                    DisplayName: request.Username,
                    Password: request.Password
                );

                var response = await CallUserModuleApi("user/create", HttpMethod.Post, createUserRequest);
                if (response.Success)
                {
                    _logger.Info($"New user registered via REST API: {request.Email}");
                    return new
                    {
                        success = true,
                        message = "User registered successfully",
                        userId = response.UserId,
                        timestamp = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.Warn($"Registration failed for user {request.Email}: {response.Message}");
                    return new { success = false, error = response.Message, timestamp = DateTime.UtcNow };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Registration error for user {request.Email}: {ex.Message}", ex);
                return new { success = false, error = "Registration failed due to system error", timestamp = DateTime.UtcNow };
            }
        }

        [ApiRoute("POST", "/auth/login", "login", "Authenticate user", "codex.security")]
        public async Task<object> LoginAsync([ApiParameter("request", "Login request")] LoginRequest request)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return new { success = false, error = "Username and password are required", timestamp = DateTime.UtcNow };
                }

                // Call UserModule via REST API
                var authRequest = new UserAuthRequest(
                    Username: request.Username,
                    Password: request.Password
                );

                var response = await CallUserModuleApi("user/authenticate", HttpMethod.Post, authRequest);
                if (response.Success)
                {
                    _logger.Info($"User authenticated via REST API: {request.Username}");
                    return new
                    {
                        success = true,
                        token = response.Token,
                        message = response.Message,
                        timestamp = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.Warn($"Authentication failed for user {request.Username}: {response.Message}");
                    return new { success = false, error = response.Message, timestamp = DateTime.UtcNow };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Authentication error for user {request.Username}: {ex.Message}", ex);
                return new { success = false, error = "Authentication failed due to system error", timestamp = DateTime.UtcNow };
            }
        }

        // Helper method for REST API calls to UserModule
        private async Task<ApiResponse> CallUserModuleApi(string endpoint, HttpMethod method, object data = null)
        {
            var url = $"{_userModuleBaseUrl}/{endpoint}";
            
            var request = new HttpRequestMessage(method, url);
            
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    return result ?? new ApiResponse { Success = false, Message = "Invalid response format" };
                }
                else
                {
                    return new ApiResponse { Success = false, Message = $"HTTP {response.StatusCode}: {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"API call failed: {ex.Message}", ex);
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }

    [RequestType(Name = "RegisterRequest")]
    public record RegisterRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("password")] string Password
    );

    [RequestType(Name = "LoginRequest")]
    public record LoginRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password
    );

    // Note: UserCreateRequest and UserAuthRequest are defined in UserModule.cs

    // Response types for UserModule API calls
    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
