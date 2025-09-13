using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private string? _authToken;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthToken()
        {
            _authToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // Authentication
        public async Task<ApiResponse<User>> AuthenticateAsync(string username, string password)
        {
            try
            {
                var request = new { username, password };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/user/authenticate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<User>>(responseContent, _jsonOptions);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Store auth token if provided
                        if (response.Headers.Contains("Authorization"))
                        {
                            var token = response.Headers.GetValues("Authorization").FirstOrDefault();
                            if (!string.IsNullOrEmpty(token))
                            {
                                SetAuthToken(token.Replace("Bearer ", ""));
                            }
                        }
                    }
                    return apiResponse ?? new ApiResponse<User> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"Authentication failed: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"Authentication error: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<User>> CreateUserAsync(string username, string email, string password)
        {
            try
            {
                var request = new { username, email, password };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/user/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<User>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<User> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"User creation failed: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"User creation error: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<User>> GetUserProfileAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/user/profile/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<User>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<User> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"Failed to get user profile: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<User> 
                { 
                    Success = false, 
                    Message = $"Error getting user profile: {ex.Message}" 
                };
            }
        }

        public async Task<User?> GetUserAsync(string userId)
        {
            try
            {
                var response = await GetUserProfileAsync(userId);
                return response.Success ? response.Data : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUser error: {ex.Message}");
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            ClearAuthToken();
            await Task.CompletedTask;
        }

        // Concepts
        public async Task<ApiResponse<List<Concept>>> GetConceptsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/concepts");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<List<Concept>>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<List<Concept>> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<List<Concept>> 
                { 
                    Success = false, 
                    Message = $"Failed to get concepts: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Concept>> 
                { 
                    Success = false, 
                    Message = $"Error getting concepts: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<Concept>> GetConceptAsync(string conceptId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/concepts/{conceptId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<Concept>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<Concept> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Failed to get concept: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Error getting concept: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<Concept>> CreateConceptAsync(Concept concept)
        {
            try
            {
                var json = JsonSerializer.Serialize(concept, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/concepts", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<Concept>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<Concept> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Failed to create concept: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Error creating concept: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<Concept>> UpdateConceptAsync(string conceptId, Concept concept)
        {
            try
            {
                var json = JsonSerializer.Serialize(concept, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/concepts/{conceptId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<Concept>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<Concept> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Failed to update concept: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Concept> 
                { 
                    Success = false, 
                    Message = $"Error updating concept: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse> DeleteConceptAsync(string conceptId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/concepts/{conceptId}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse { Success = true };
                }

                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Failed to delete concept: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Error deleting concept: {ex.Message}" 
                };
            }
        }

        // Contributions
        public async Task<ApiResponse<List<Contribution>>> GetUserContributionsAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/contributions/user/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<List<Contribution>>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<List<Contribution>> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<List<Contribution>> 
                { 
                    Success = false, 
                    Message = $"Failed to get contributions: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Contribution>> 
                { 
                    Success = false, 
                    Message = $"Error getting contributions: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<Contribution>> CreateContributionAsync(Contribution contribution)
        {
            try
            {
                var json = JsonSerializer.Serialize(contribution, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/contributions/record", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<Contribution>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<Contribution> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<Contribution> 
                { 
                    Success = false, 
                    Message = $"Failed to create contribution: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Contribution> 
                { 
                    Success = false, 
                    Message = $"Error creating contribution: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<Contribution>> UpdateContributionAsync(string contributionId, Contribution contribution)
        {
            try
            {
                var json = JsonSerializer.Serialize(contribution, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/contributions/{contributionId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<Contribution>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<Contribution> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<Contribution> 
                { 
                    Success = false, 
                    Message = $"Failed to update contribution: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Contribution> 
                { 
                    Success = false, 
                    Message = $"Error updating contribution: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse> DeleteContributionAsync(string contributionId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/contributions/{contributionId}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse { Success = true };
                }

                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Failed to delete contribution: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Error deleting contribution: {ex.Message}" 
                };
            }
        }

        // Resonance and Energy
        public async Task<ApiResponse<double>> GetCollectiveEnergyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/contributions/abundance/collective-energy");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<double>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<double> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<double> 
                { 
                    Success = false, 
                    Message = $"Failed to get collective energy: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<double> 
                { 
                    Success = false, 
                    Message = $"Error getting collective energy: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<double>> GetContributorEnergyAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/contributions/abundance/contributor-energy/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<double>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<double> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<double> 
                { 
                    Success = false, 
                    Message = $"Failed to get contributor energy: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<double> 
                { 
                    Success = false, 
                    Message = $"Error getting contributor energy: {ex.Message}" 
                };
            }
        }

        public async Task<ApiResponse<List<Contribution>>> GetAbundanceEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/contributions/abundance/events");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<ApiResponse<List<Contribution>>>(responseContent, _jsonOptions) 
                        ?? new ApiResponse<List<Contribution>> { Success = false, Message = "Failed to parse response" };
                }

                return new ApiResponse<List<Contribution>> 
                { 
                    Success = false, 
                    Message = $"Failed to get abundance events: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Contribution>> 
                { 
                    Success = false, 
                    Message = $"Error getting abundance events: {ex.Message}" 
                };
            }
        }

        // Health Check
        public async Task<ApiResponse> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse { Success = true, Message = "API is healthy" };
                }

                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Health check failed: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse 
                { 
                    Success = false, 
                    Message = $"Health check error: {ex.Message}" 
                };
            }
        }
    }
}
