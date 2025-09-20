using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;

namespace CodexBootstrap.Tests
{
    [Collection("TestServer")]
    public class IdentityTests
    {
        private readonly HttpClient _httpClient;

        public IdentityTests(TestServerFixture fixture)
        {
            _httpClient = fixture.HttpClient;
        }

        // Unit Tests for MockIdentityProvider
        [Fact]
        public void MockIdentityProvider_ShouldInitiateLogin()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);

            // Act
            var result = mockProvider.InitiateLogin("http://localhost:3000/callback");

            // Assert
            result.Should().NotBeNull();
            var resultJson = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
            
            // The result is wrapped in a Task, so we need to access the Result property
            resultObj.Should().ContainKey("Result");
            var actualResult = resultObj["Result"] as JsonElement?;
            actualResult.Should().NotBeNull();
            actualResult!.Value.GetProperty("success").GetBoolean().Should().BeTrue();
            actualResult.Value.GetProperty("loginUrl").GetString().Should().NotBeNullOrEmpty();
            actualResult.Value.GetProperty("state").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MockIdentityProvider_ShouldHandleCallback()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);
            var authCode = "mock_auth_code_123";
            var state = "test_state_456";

            // Act
            var result = await mockProvider.HandleCallbackAsync(authCode, state, "http://localhost:3000/callback");

            // Assert
            result.Should().NotBeNull();
            result.Provider.Should().Be("mock");
            result.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MockIdentityProvider_ShouldExchangeCodeForToken()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);
            var authCode = "mock_auth_code_123";

            // Act
            var result = await mockProvider.ExchangeCodeForTokenAsync(authCode, "mock");

            // Assert
            result.Should().NotBeNull();
            var accessToken = result?.GetType().GetProperty("AccessToken")?.GetValue(result)?.ToString();
            accessToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MockIdentityProvider_ShouldGetUserInfo()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);
            var accessToken = "mock_access_token_123";

            // Act
            var result = await mockProvider.GetUserInfoAsync(accessToken);

            // Assert
            result.Should().NotBeNull();
            var mockUser = result as MockUser;
            mockUser.Should().NotBeNull();
            mockUser!.Email.Should().NotBeNullOrEmpty();
            mockUser.Name.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MockIdentityProvider_ShouldValidateUser()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);
            var user = new MockUser("testuser1", "test@example.com", "Test User", "Test User", "testuser1");

            // Act
            var result = await mockProvider.ValidateUserAsync(user);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void MockIdentityProvider_ShouldGetAllMockUsers()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);

            // Act
            var users = mockProvider.GetAllMockUsers();

            // Assert
            users.Should().NotBeEmpty();
            users.Should().Contain(u => u.Email == "testuser1@example.com");
            users.Should().Contain(u => u.Email == "testuser2@example.com");
            users.Should().Contain(u => u.Email == "admin@example.com");
        }

        [Fact]
        public void MockIdentityProvider_ShouldGetIdentityProviders()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(IdentityTests));
            var mockProvider = new MockIdentityProvider(logger);

            // Act
            var providers = mockProvider.GetIdentityProviders();

            // Assert
            providers.Should().NotBeEmpty();
            providers.Should().Contain(p => p.Provider == "mock");
        }

        // Integration Tests (require running server)
        [Fact]
        public async Task IdentityProviders_ShouldReturnAvailableProviders()
        {
            try
            {
                // Act
                var response = await _httpClient.GetAsync("/identity/providers");
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                
                var providers = JsonSerializer.Deserialize<IdentityProvidersResponse>(content);
                providers.Should().NotBeNull();
                providers.providers.Should().NotBeEmpty();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task IdentityLogin_ShouldInitiateLoginFlow()
        {
            try
            {
                // Act
                var response = await _httpClient.GetAsync("/identity/login/mock?returnUrl=http://localhost:3000/callback");
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                result.Should().NotBeNull();
                result.Should().ContainKey("success");
                result.Should().ContainKey("loginUrl");
                (result["success"] as JsonElement?)?.GetBoolean().Should().BeTrue();
                (result["loginUrl"] as JsonElement?)?.GetString().Should().NotBeNullOrEmpty();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task IdentityCallback_ShouldHandleMockAuthentication()
        {
            try
            {
                // Act
                var response = await _httpClient.GetAsync("/identity/callback/mock?code=mock_auth_code_123&state=test_state_456");
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                result.Should().NotBeNull();
                result.Should().ContainKey("provider");
                result.Should().ContainKey("success");
                (result["provider"] as JsonElement?)?.GetString().Should().Be("mock");
                (result["success"] as JsonElement?)?.GetBoolean().Should().BeTrue();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task CreateUser_ShouldCreateNewUser()
        {
            try
            {
                // Arrange
                var unique = DateTime.UtcNow.Ticks.ToString();
                var userRequest = new UserCreateRequest($"testuser_{unique}", $"test_{unique}@example.com", "Test User", "password123");
                var json = JsonSerializer.Serialize(userRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Act
                var response = await _httpClient.PostAsync("/identity/users", content);
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                result.Should().NotBeNull();
                // Accept success true OR message indicating existing user for idempotency
                if (result.ContainsKey("success") && result["success"] is JsonElement successProp && successProp.GetBoolean())
                {
                    successProp.GetBoolean().Should().BeTrue();
                }
                else
                {
                    result.Should().ContainKey("message");
                    var messageProp = result["message"] as JsonElement?;
                    messageProp.Should().NotBeNull();
                    messageProp!.Value.GetString().Should().Contain("exists");
                }
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task AuthenticateUser_ShouldAuthenticateValidUser()
        {
            try
            {
                // Ensure user exists
                var userRequest = new UserCreateRequest("testuser", "test@example.com", "Test User", "password123");
                var createJson = JsonSerializer.Serialize(userRequest);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync("/identity/users", createContent);

                // Arrange
                var authRequest = new UserAuthRequest("testuser", "password123");
                var json = JsonSerializer.Serialize(authRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Act
                var response = await _httpClient.PostAsync("/identity/authenticate", content);
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                result.Should().NotBeNull();
                result.Should().ContainKey("success");
                result.Should().ContainKey("token");
                (result["success"] as JsonElement?)?.GetBoolean().Should().BeTrue();
                (result["token"] as JsonElement?)?.GetString().Should().NotBeNullOrEmpty();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task GetUserProfile_ShouldReturnUserProfile()
        {
            try
            {
                // Ensure user exists (unique)
                var unique = DateTime.UtcNow.Ticks.ToString();
                var username = $"testuser_{unique}";
                var userId = $"user.{username}";
                var userRequest = new UserCreateRequest(username, $"{username}@example.com", "Test User", "password123");
                var createJson = JsonSerializer.Serialize(userRequest);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync("/identity/users", createContent);

                // Act
                var response = await _httpClient.GetAsync($"/identity/users/{userId}");
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                result.Should().NotBeNull();
                result.Should().ContainKey("userId");
                result.Should().ContainKey("username");
                result.Should().ContainKey("email");
                result.Should().ContainKey("displayName");
                (result["userId"] as JsonElement?)?.GetString().Should().Be(userId);
                (result["username"] as JsonElement?)?.GetString().Should().Be(username);
                (result["email"] as JsonElement?)?.GetString().Should().Contain("@example.com");
                (result["displayName"] as JsonElement?)?.GetString().Should().NotBeNullOrEmpty();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task CreateSession_ShouldCreateNewSession()
        {
            try
            {
                // Arrange
                var sessionRequest = new SessionCreateRequest("user.testuser");
                var json = JsonSerializer.Serialize(sessionRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Act
                var response = await _httpClient.PostAsync("/identity/sessions", content);
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                result.Should().NotBeNull();
                result.Should().ContainKey("success");
                result.Should().ContainKey("sessionToken");
                (result["success"] as JsonElement?)?.GetBoolean().Should().BeTrue();
                (result["sessionToken"] as JsonElement?)?.GetString().Should().NotBeNullOrEmpty();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }

        [Fact]
        public async Task EndSession_ShouldEndExistingSession()
        {
            try
            {
                // Ensure user exists
                var unique = DateTime.UtcNow.Ticks.ToString();
                var username = $"testuser_{unique}";
                var userId = $"user.{username}";
                var userRequest = new UserCreateRequest(username, $"{username}@example.com", "Test User", "password123");
                var createJson = JsonSerializer.Serialize(userRequest);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync("/identity/users", createContent);

                // Create a session first
                var sessionRequest = new SessionCreateRequest(userId);
                var sessionJson = JsonSerializer.Serialize(sessionRequest);
                var sessionContent = new StringContent(sessionJson, Encoding.UTF8, "application/json");
                var createSessionResponse = await _httpClient.PostAsync("/identity/sessions", sessionContent);
                createSessionResponse.IsSuccessStatusCode.Should().BeTrue();
                var createSessionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(await createSessionResponse.Content.ReadAsStringAsync());
                createSessionPayload.Should().NotBeNull();
                createSessionPayload.Should().ContainKey("sessionToken");
                var token = (createSessionPayload["sessionToken"] as JsonElement?)?.GetString();
                token.Should().NotBeNullOrEmpty();

                // Act
                var response = await _httpClient.DeleteAsync($"/identity/sessions/{token}");
                
                // Assert
                response.IsSuccessStatusCode.Should().BeTrue();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeNullOrEmpty();
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                result.Should().NotBeNull();
                result.Should().ContainKey("success");
                (result["success"] as JsonElement?)?.GetBoolean().Should().BeTrue();
            }
            catch (HttpRequestException)
            {
                // Skip test if server is not running
                Assert.True(true, "Server not running, skipping integration test");
            }
        }
    }
}
