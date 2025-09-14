using System.Collections.ObjectModel;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class ConversationViewModel : BaseViewModel
{
    private readonly IConversationService _conversationService;
    private readonly IAuthenticationService _authService;
    private readonly ISignalRService _signalRService;

    private Conversation? _currentConversation;
    private ObservableCollection<ConversationMessage> _messages = new();
    private ObservableCollection<FractalChannel> _fractalChannels = new();
    private ObservableCollection<ConversationParticipant> _participants = new();
    private string _newMessageText = string.Empty;
    private string _newChannelName = string.Empty;
    private string _newChannelDescription = string.Empty;
    private FractalChannel? _selectedChannel;
    private ConversationMessage? _selectedMessage;
    private bool _isWaterNodeMode = true;

    public ConversationViewModel(IConversationService conversationService, IAuthenticationService authService, ISignalRService signalRService)
    {
        _conversationService = conversationService;
        _authService = authService;
        _signalRService = signalRService;
        Title = "Conversation";

        // Commands
        SendMessageCommand = new Command(async () => await SendMessageAsync());
        CreateFractalChannelCommand = new Command(async () => await CreateFractalChannelAsync());
        ConvertToWaterNodeCommand = new Command<ConversationMessage>(async (message) => await ConvertToWaterNodeAsync(message));
        LinkToConceptCommand = new Command<ConversationMessage>(async (message) => await LinkToConceptAsync(message));
        CalculateResonanceCommand = new Command<ConversationMessage>(async (message) => await CalculateResonanceAsync(message));
        FindCorrelationsCommand = new Command<ConversationMessage>(async (message) => await FindCorrelationsAsync(message));
        RefreshMessagesCommand = new Command(async () => await RefreshMessagesAsync());

        // Subscribe to events
        _conversationService.MessageReceived += OnMessageReceived;
        _conversationService.ResonanceDetected += OnResonanceDetected;
        _conversationService.CorrelationFound += OnCorrelationFound;
        _conversationService.ConversationUpdated += OnConversationUpdated;
        _conversationService.FractalChannelCreated += OnFractalChannelCreated;
    }

    public Conversation? CurrentConversation
    {
        get => _currentConversation;
        set => SetProperty(ref _currentConversation, value);
    }

    public ObservableCollection<ConversationMessage> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    public ObservableCollection<FractalChannel> FractalChannels
    {
        get => _fractalChannels;
        set => SetProperty(ref _fractalChannels, value);
    }

    public ObservableCollection<ConversationParticipant> Participants
    {
        get => _participants;
        set => SetProperty(ref _participants, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }

    public string NewChannelName
    {
        get => _newChannelName;
        set => SetProperty(ref _newChannelName, value);
    }

    public string NewChannelDescription
    {
        get => _newChannelDescription;
        set => SetProperty(ref _newChannelDescription, value);
    }

    public FractalChannel? SelectedChannel
    {
        get => _selectedChannel;
        set => SetProperty(ref _selectedChannel, value);
    }

    public ConversationMessage? SelectedMessage
    {
        get => _selectedMessage;
        set => SetProperty(ref _selectedMessage, value);
    }

    public bool IsWaterNodeMode
    {
        get => _isWaterNodeMode;
        set => SetProperty(ref _isWaterNodeMode, value);
    }

    public ICommand SendMessageCommand { get; }
    public ICommand CreateFractalChannelCommand { get; }
    public ICommand ConvertToWaterNodeCommand { get; }
    public ICommand LinkToConceptCommand { get; }
    public ICommand CalculateResonanceCommand { get; }
    public ICommand FindCorrelationsCommand { get; }
    public ICommand RefreshMessagesCommand { get; }

    public async Task InitializeAsync(string conversationId)
    {
        IsBusy = true;
        try
        {
            // Load conversation details
            var conversationResponse = await _conversationService.GetConversationAsync(conversationId);
            if (conversationResponse.Success && conversationResponse.Data != null)
            {
                CurrentConversation = conversationResponse.Data;
                Title = CurrentConversation.Name;
            }

            // Load participants
            var participantsResponse = await _conversationService.GetParticipantsAsync(conversationId);
            if (participantsResponse.Success && participantsResponse.Data != null)
            {
                Participants.Clear();
                foreach (var participant in participantsResponse.Data)
                {
                    Participants.Add(participant);
                }
            }

            // Load fractal channels
            var channelsResponse = await _conversationService.GetFractalChannelsAsync(conversationId);
            if (channelsResponse.Success && channelsResponse.Data != null)
            {
                FractalChannels.Clear();
                foreach (var channel in channelsResponse.Data)
                {
                    FractalChannels.Add(channel);
                }
            }

            // Load messages
            await RefreshMessagesAsync();

            // Subscribe to real-time events for this conversation
            await _signalRService.JoinGroupAsync($"conversation:{conversationId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing conversation: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMessageText) || CurrentConversation == null)
            return;

        IsBusy = true;
        try
        {
            var messageType = IsWaterNodeMode ? MessageType.WaterNode : MessageType.Text;
            var response = await _conversationService.SendMessageAsync(
                CurrentConversation.Id, 
                NewMessageText, 
                messageType);

            if (response.Success && response.Data != null)
            {
                Messages.Add(response.Data);
                NewMessageText = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateFractalChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(NewChannelName) || CurrentConversation == null)
            return;

        IsBusy = true;
        try
        {
            var response = await _conversationService.CreateFractalChannelAsync(
                CurrentConversation.Id,
                NewChannelName,
                NewChannelDescription,
                SelectedChannel?.Id);

            if (response.Success && response.Data != null)
            {
                FractalChannels.Add(response.Data);
                NewChannelName = string.Empty;
                NewChannelDescription = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating fractal channel: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConvertToWaterNodeAsync(ConversationMessage message)
    {
        if (message == null) return;

        IsBusy = true;
        try
        {
            var response = await _conversationService.ConvertToWaterNodeAsync(message.Id);
            if (response.Success && response.Data != null)
            {
                var index = Messages.IndexOf(message);
                if (index >= 0)
                {
                    Messages[index] = response.Data;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting to water node: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LinkToConceptAsync(ConversationMessage message)
    {
        if (message == null) return;

        // This would typically open a concept selection dialog
        // For now, we'll use a placeholder concept ID
        var conceptId = "placeholder-concept-id";

        IsBusy = true;
        try
        {
            var response = await _conversationService.LinkToConceptAsync(message.Id, conceptId);
            if (response.Success && response.Data != null)
            {
                var index = Messages.IndexOf(message);
                if (index >= 0)
                {
                    Messages[index] = response.Data;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error linking to concept: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CalculateResonanceAsync(ConversationMessage message)
    {
        if (message == null) return;

        IsBusy = true;
        try
        {
            var response = await _conversationService.CalculateResonanceAsync(message.Id, SelectedChannel?.Id);
            if (response.Success && response.Data != null)
            {
                // Update message with resonance score
                var index = Messages.IndexOf(message);
                if (index >= 0)
                {
                    var updatedMessage = message with { ResonanceScore = response.Data.ResonanceScore };
                    Messages[index] = updatedMessage;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculating resonance: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FindCorrelationsAsync(ConversationMessage message)
    {
        if (message == null) return;

        IsBusy = true;
        try
        {
            var response = await _conversationService.FindCorrelationsAsync(message.Id);
            if (response.Success && response.Data != null)
            {
                // Update message with correlations
                var index = Messages.IndexOf(message);
                if (index >= 0)
                {
                    var updatedMessage = message with { Correlations = response.Data };
                    Messages[index] = updatedMessage;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding correlations: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshMessagesAsync()
    {
        if (CurrentConversation == null) return;

        IsBusy = true;
        try
        {
            var response = await _conversationService.GetMessagesAsync(CurrentConversation.Id);
            if (response.Success && response.Data != null)
            {
                Messages.Clear();
                foreach (var message in response.Data)
                {
                    Messages.Add(message);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing messages: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnMessageReceived(object? sender, ConversationMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CurrentConversation?.Id == message.ConversationId)
            {
                Messages.Add(message);
            }
        });
    }

    private void OnResonanceDetected(object? sender, ChannelResonance resonance)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Resonance detected: {resonance.ResonanceScore} for channel {resonance.ChannelId}");
        });
    }

    private void OnCorrelationFound(object? sender, MessageCorrelation correlation)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Correlation found: {correlation.CorrelationType} with strength {correlation.Strength}");
        });
    }

    private void OnConversationUpdated(object? sender, Conversation conversation)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CurrentConversation?.Id == conversation.Id)
            {
                CurrentConversation = conversation;
                Title = conversation.Name;
            }
        });
    }

    private void OnFractalChannelCreated(object? sender, FractalChannel channel)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (CurrentConversation?.Id == channel.ConversationId)
            {
                FractalChannels.Add(channel);
            }
        });
    }
}



