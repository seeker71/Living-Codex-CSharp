using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class ConversationService : IConversationService
{
    private readonly HttpClient _httpClient;
    private readonly IApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly JsonSerializerOptions _jsonOptions;

    public event EventHandler<ConversationMessage>? MessageReceived;
    public event EventHandler<ChannelResonance>? ResonanceDetected;
    public event EventHandler<MessageCorrelation>? CorrelationFound;
    public event EventHandler<Conversation>? ConversationUpdated;
    public event EventHandler<FractalChannel>? FractalChannelCreated;

    public ConversationService(HttpClient httpClient, IApiService apiService, ISignalRService signalRService)
    {
        _httpClient = httpClient;
        _apiService = apiService;
        _signalRService = signalRService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // Subscribe to SignalR events
        _signalRService.NodeEventReceived += OnNodeEventReceived;
        _signalRService.SystemEventReceived += OnSystemEventReceived;
    }

    public async Task<ApiResponse<Conversation>> CreateConversationAsync(string name, string? description, ConversationType type, List<string> participantIds)
    {
        try
        {
            var request = new ConversationCreateRequest(
                Name: name,
                Description: description,
                Type: type,
                CreatedBy: participantIds.FirstOrDefault() ?? "system",
                Metadata: new Dictionary<string, object>
                {
                    ["participantIds"] = participantIds,
                    ["createdVia"] = "mobile_app"
                }
            );

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/conversations/create", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<Conversation>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<Conversation> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Failed to create conversation: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Error creating conversation: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Conversation>> GetConversationAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<Conversation>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<Conversation> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Failed to get conversation: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Error getting conversation: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<Conversation>>> GetUserConversationsAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/user/{userId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<Conversation>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<Conversation>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<Conversation>>
            {
                Success = false,
                Message = $"Failed to get user conversations: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<Conversation>>
            {
                Success = false,
                Message = $"Error getting user conversations: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<Conversation>> UpdateConversationAsync(string conversationId, string? name, string? description)
    {
        try
        {
            var request = new
            {
                Name = name,
                Description = description
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/conversations/{conversationId}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<Conversation>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<Conversation> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Failed to update conversation: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Conversation>
            {
                Success = false,
                Message = $"Error updating conversation: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteConversationAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/conversations/{conversationId}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete conversation: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error deleting conversation: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> AddParticipantAsync(string conversationId, string userId, ParticipantRole role = ParticipantRole.Contributor)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                Role = role.ToString()
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/conversations/{conversationId}/participants", content);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to add participant: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error adding participant: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> RemoveParticipantAsync(string conversationId, string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/conversations/{conversationId}/participants/{userId}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to remove participant: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error removing participant: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> UpdateParticipantRoleAsync(string conversationId, string userId, ParticipantRole role)
    {
        try
        {
            var request = new { Role = role.ToString() };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/conversations/{conversationId}/participants/{userId}", content);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to update participant role: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error updating participant role: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ConversationParticipant>>> GetParticipantsAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/participants");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<ConversationParticipant>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<ConversationParticipant>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<ConversationParticipant>>
            {
                Success = false,
                Message = $"Failed to get participants: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ConversationParticipant>>
            {
                Success = false,
                Message = $"Error getting participants: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ConversationMessage>> SendMessageAsync(string conversationId, string content, MessageType messageType = MessageType.Text, string? parentMessageId = null)
    {
        try
        {
            var request = new
            {
                ConversationId = conversationId,
                Content = content,
                MessageType = messageType.ToString(),
                ParentMessageId = parentMessageId
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/conversations/messages/send", contentData);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ConversationMessage>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<ConversationMessage> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Failed to send message: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Error sending message: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ConversationMessage>>> GetMessagesAsync(string conversationId, int page = 1, int pageSize = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/messages?page={page}&pageSize={pageSize}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<ConversationMessage>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<ConversationMessage>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Failed to get messages: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Error getting messages: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ConversationMessage>> UpdateMessageAsync(string messageId, string content)
    {
        try
        {
            var request = new { Content = content };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var contentData = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/conversations/messages/{messageId}", contentData);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ConversationMessage>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<ConversationMessage> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Failed to update message: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Error updating message: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(string messageId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/conversations/messages/{messageId}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete message: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error deleting message: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ConversationMessage>> ConvertToWaterNodeAsync(string messageId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/conversations/messages/{messageId}/convert-to-water-node", null);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ConversationMessage>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<ConversationMessage> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Failed to convert to water node: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Error converting to water node: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ConversationMessage>>> GetWaterNodeMessagesAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/messages/water-nodes");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<ConversationMessage>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<ConversationMessage>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Failed to get water node messages: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Error getting water node messages: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ConversationMessage>> LinkToConceptAsync(string messageId, string conceptId)
    {
        try
        {
            var request = new { ConceptId = conceptId };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/conversations/messages/{messageId}/link-concept", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ConversationMessage>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<ConversationMessage> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Failed to link to concept: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ConversationMessage>
            {
                Success = false,
                Message = $"Error linking to concept: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<FractalChannel>> CreateFractalChannelAsync(string conversationId, string name, string? description, string? parentChannelId = null, string? conceptId = null)
    {
        try
        {
            var request = new
            {
                ConversationId = conversationId,
                Name = name,
                Description = description,
                ParentChannelId = parentChannelId,
                ConceptId = conceptId
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/conversations/fractal-channels/create", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<FractalChannel>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<FractalChannel> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<FractalChannel>
            {
                Success = false,
                Message = $"Failed to create fractal channel: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<FractalChannel>
            {
                Success = false,
                Message = $"Error creating fractal channel: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<FractalChannel>>> GetFractalChannelsAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/fractal-channels");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<FractalChannel>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<FractalChannel>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<FractalChannel>>
            {
                Success = false,
                Message = $"Failed to get fractal channels: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<FractalChannel>>
            {
                Success = false,
                Message = $"Error getting fractal channels: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<FractalChannel>> UpdateFractalChannelAsync(string channelId, string? name, string? description, double? resonanceThreshold = null)
    {
        try
        {
            var request = new
            {
                Name = name,
                Description = description,
                ResonanceThreshold = resonanceThreshold
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/conversations/fractal-channels/{channelId}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<FractalChannel>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<FractalChannel> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<FractalChannel>
            {
                Success = false,
                Message = $"Failed to update fractal channel: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<FractalChannel>
            {
                Success = false,
                Message = $"Error updating fractal channel: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteFractalChannelAsync(string channelId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/conversations/fractal-channels/{channelId}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete fractal channel: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Error deleting fractal channel: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<ChannelResonance>> CalculateResonanceAsync(string messageId, string? channelId = null)
    {
        try
        {
            var request = new { MessageId = messageId, ChannelId = channelId };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/conversations/resonance/calculate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ChannelResonance>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<ChannelResonance> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<ChannelResonance>
            {
                Success = false,
                Message = $"Failed to calculate resonance: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ChannelResonance>
            {
                Success = false,
                Message = $"Error calculating resonance: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<MessageCorrelation>>> FindCorrelationsAsync(string messageId, int maxResults = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/messages/{messageId}/correlations?maxResults={maxResults}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<MessageCorrelation>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<MessageCorrelation>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<MessageCorrelation>>
            {
                Success = false,
                Message = $"Failed to find correlations: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<MessageCorrelation>>
            {
                Success = false,
                Message = $"Error finding correlations: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ConversationMessage>>> GetResonantMessagesAsync(string conversationId, double minResonanceScore = 0.5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/messages/resonant?minScore={minResonanceScore}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<ConversationMessage>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<ConversationMessage>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Failed to get resonant messages: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Error getting resonant messages: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<ConversationMessage>>> GetCorrelatedMessagesAsync(string messageId, int maxResults = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/conversations/messages/{messageId}/correlated?maxResults={maxResults}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<List<ConversationMessage>>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<List<ConversationMessage>> { Success = false, Message = "Failed to parse response" };
            }

            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Failed to get correlated messages: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ConversationMessage>>
            {
                Success = false,
                Message = $"Error getting correlated messages: {ex.Message}"
            };
        }
    }

    private void OnNodeEventReceived(object? sender, RealtimeEvent nodeEvent)
    {
        // Handle water node events
        if (nodeEvent.NodeType == "water" && nodeEvent.Data is JsonElement data)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ConversationMessage>(data.GetRawText(), _jsonOptions);
                if (message != null)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing water node event: {ex.Message}");
            }
        }
    }

    private void OnSystemEventReceived(object? sender, SystemEvent systemEvent)
    {
        // Handle conversation-related system events
        if (systemEvent.EventType.StartsWith("conversation.") && systemEvent.Data is JsonElement data)
        {
            try
            {
                switch (systemEvent.EventType)
                {
                    case "conversation.message.received":
                        var message = JsonSerializer.Deserialize<ConversationMessage>(data.GetRawText(), _jsonOptions);
                        if (message != null)
                        {
                            MessageReceived?.Invoke(this, message);
                        }
                        break;
                    case "conversation.resonance.detected":
                        var resonance = JsonSerializer.Deserialize<ChannelResonance>(data.GetRawText(), _jsonOptions);
                        if (resonance != null)
                        {
                            ResonanceDetected?.Invoke(this, resonance);
                        }
                        break;
                    case "conversation.correlation.found":
                        var correlation = JsonSerializer.Deserialize<MessageCorrelation>(data.GetRawText(), _jsonOptions);
                        if (correlation != null)
                        {
                            CorrelationFound?.Invoke(this, correlation);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing system event: {ex.Message}");
            }
        }
    }
}
