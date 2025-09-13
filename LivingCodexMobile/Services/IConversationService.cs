using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IConversationService
{
    // Conversation Management
    Task<ApiResponse<Conversation>> CreateConversationAsync(string name, string? description, ConversationType type, List<string> participantIds);
    Task<ApiResponse<Conversation>> GetConversationAsync(string conversationId);
    Task<ApiResponse<List<Conversation>>> GetUserConversationsAsync(string userId);
    Task<ApiResponse<Conversation>> UpdateConversationAsync(string conversationId, string? name, string? description);
    Task<ApiResponse<bool>> DeleteConversationAsync(string conversationId);
    
    // Participant Management
    Task<ApiResponse<bool>> AddParticipantAsync(string conversationId, string userId, ParticipantRole role = ParticipantRole.Contributor);
    Task<ApiResponse<bool>> RemoveParticipantAsync(string conversationId, string userId);
    Task<ApiResponse<bool>> UpdateParticipantRoleAsync(string conversationId, string userId, ParticipantRole role);
    Task<ApiResponse<List<ConversationParticipant>>> GetParticipantsAsync(string conversationId);
    
    // Message Management
    Task<ApiResponse<ConversationMessage>> SendMessageAsync(string conversationId, string content, MessageType messageType = MessageType.Text, string? parentMessageId = null);
    Task<ApiResponse<List<ConversationMessage>>> GetMessagesAsync(string conversationId, int page = 1, int pageSize = 50);
    Task<ApiResponse<ConversationMessage>> UpdateMessageAsync(string messageId, string content);
    Task<ApiResponse<bool>> DeleteMessageAsync(string messageId);
    
    // Water Node Integration
    Task<ApiResponse<ConversationMessage>> ConvertToWaterNodeAsync(string messageId);
    Task<ApiResponse<List<ConversationMessage>>> GetWaterNodeMessagesAsync(string conversationId);
    Task<ApiResponse<ConversationMessage>> LinkToConceptAsync(string messageId, string conceptId);
    
    // Fractal Channel Management
    Task<ApiResponse<FractalChannel>> CreateFractalChannelAsync(string conversationId, string name, string? description, string? parentChannelId = null, string? conceptId = null);
    Task<ApiResponse<List<FractalChannel>>> GetFractalChannelsAsync(string conversationId);
    Task<ApiResponse<FractalChannel>> UpdateFractalChannelAsync(string channelId, string? name, string? description, double? resonanceThreshold = null);
    Task<ApiResponse<bool>> DeleteFractalChannelAsync(string channelId);
    
    // Resonance and Correlation
    Task<ApiResponse<ChannelResonance>> CalculateResonanceAsync(string messageId, string? channelId = null);
    Task<ApiResponse<List<MessageCorrelation>>> FindCorrelationsAsync(string messageId, int maxResults = 10);
    Task<ApiResponse<List<ConversationMessage>>> GetResonantMessagesAsync(string conversationId, double minResonanceScore = 0.5);
    Task<ApiResponse<List<ConversationMessage>>> GetCorrelatedMessagesAsync(string messageId, int maxResults = 10);
    
    // Real-time Events
    event EventHandler<ConversationMessage>? MessageReceived;
    event EventHandler<ChannelResonance>? ResonanceDetected;
    event EventHandler<MessageCorrelation>? CorrelationFound;
    event EventHandler<Conversation>? ConversationUpdated;
    event EventHandler<FractalChannel>? FractalChannelCreated;
}

