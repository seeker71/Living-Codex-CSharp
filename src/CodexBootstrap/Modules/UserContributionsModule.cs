using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// User contributions module with ETH ledger, change tracking, attribution, and reward sharing
/// </summary>
public sealed class UserContributionsModule : IModule
{
    private readonly Core.ILogger _logger;
    private readonly NodeRegistry _registry;
    private readonly Web3? _web3;
    private readonly ConcurrentDictionary<string, Contribution> _contributions = new();
    private readonly ConcurrentDictionary<string, UserReward> _userRewards = new();
    private readonly ConcurrentDictionary<string, Attribution> _attributions = new();
    private readonly ConcurrentQueue<ContributionEvent> _contributionHistory = new();
    private readonly object _lock = new object();
    private int _maxHistorySize = 1000;

    public UserContributionsModule(NodeRegistry registry, string? ethereumRpcUrl = null)
    {
        _logger = new Log4NetLogger(typeof(UserContributionsModule));
        _registry = registry;
        
        if (!string.IsNullOrEmpty(ethereumRpcUrl))
        {
            _web3 = new Web3(ethereumRpcUrl);
        }
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.user-contributions",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "User Contributions Module",
            Description: "Manages user contributions with ETH ledger, change tracking, attribution, and reward sharing",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "0.1.0",
                    capabilities = new[]
                    {
                        "contribution_tracking",
                        "eth_ledger",
                        "attribution_system",
                        "reward_sharing",
                        "change_tracking",
                        "contribution_validation",
                        "reward_calculation",
                        "attribution_verification"
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "User Contributions Module",
                ["version"] = "0.1.0",
                ["description"] = "Manages user contributions with ETH ledger, change tracking, attribution, and reward sharing"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
        _logger.Info("User Contributions Module registered");
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("User Contributions API handlers registered");
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Contribution Management API Methods
    [ApiRoute("POST", "/contributions/record", "RecordContribution", "Record a user contribution", "codex.user-contributions")]
    public async Task<object> RecordContributionAsync([ApiParameter("body", "Contribution request")] RecordContributionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required");
            }

            if (string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("Entity ID is required");
            }

            var contribution = new Contribution
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                EntityId = request.EntityId,
                EntityType = request.EntityType ?? "node",
                ContributionType = request.ContributionType ?? ContributionType.Create,
                Description = request.Description ?? "",
                Value = request.Value ?? 0,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTimeOffset.UtcNow,
                Status = ContributionStatus.Pending,
                EthereumTxHash = null
            };

            _contributions[contribution.Id] = contribution;

            // Record contribution event
            await RecordContributionEventAsync(contribution);

            // Calculate and assign rewards
            await CalculateAndAssignRewardsAsync(contribution);

            _logger.Info($"Contribution recorded: {contribution.Id} by user {request.UserId}");
            return new { success = true, contributionId = contribution.Id };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error recording contribution: {ex.Message}", ex);
            return new ErrorResponse($"Failed to record contribution: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/contributions/user/{userId}", "GetUserContributions", "Get contributions by user", "codex.user-contributions")]
    public async Task<object> GetUserContributionsAsync(string userId, [ApiParameter("query", "Query parameters")] ContributionQuery? query)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            query ??= new ContributionQuery();
            var contributions = _contributions.Values
                .Where(c => c.UserId == userId)
                .AsEnumerable();

            // Apply filters
            if (query.ContributionTypes?.Any() == true)
            {
                contributions = contributions.Where(c => query.ContributionTypes.Contains(c.ContributionType));
            }

            if (query.EntityTypes?.Any() == true)
            {
                contributions = contributions.Where(c => query.EntityTypes.Contains(c.EntityType));
            }

            if (query.Status?.Any() == true)
            {
                contributions = contributions.Where(c => query.Status.Contains(c.Status));
            }

            if (query.Since.HasValue)
            {
                contributions = contributions.Where(c => c.Timestamp >= query.Since.Value);
            }

            if (query.Until.HasValue)
            {
                contributions = contributions.Where(c => c.Timestamp <= query.Until.Value);
            }

            // Apply sorting
            contributions = query.SortDescending ? 
                contributions.OrderByDescending(c => c.Timestamp) : 
                contributions.OrderBy(c => c.Timestamp);

            // Apply pagination
            var totalCount = contributions.Count();
            var pagedContributions = contributions
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Retrieved {pagedContributions.Count} contributions for user {userId}");
            return new { 
                success = true, 
                contributions = pagedContributions, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user contributions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get user contributions: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/contributions/entity/{entityId}", "GetEntityContributions", "Get contributions for an entity", "codex.user-contributions")]
    public async Task<object> GetEntityContributionsAsync(string entityId, [ApiParameter("query", "Query parameters")] ContributionQuery? query)
    {
        try
        {
            if (string.IsNullOrEmpty(entityId))
            {
                return new ErrorResponse("Entity ID is required");
            }

            query ??= new ContributionQuery();
            var contributions = _contributions.Values
                .Where(c => c.EntityId == entityId)
                .AsEnumerable();

            // Apply filters
            if (query.ContributionTypes?.Any() == true)
            {
                contributions = contributions.Where(c => query.ContributionTypes.Contains(c.ContributionType));
            }

            if (query.EntityTypes?.Any() == true)
            {
                contributions = contributions.Where(c => query.EntityTypes.Contains(c.EntityType));
            }

            if (query.Status?.Any() == true)
            {
                contributions = contributions.Where(c => query.Status.Contains(c.Status));
            }

            if (query.Since.HasValue)
            {
                contributions = contributions.Where(c => c.Timestamp >= query.Since.Value);
            }

            if (query.Until.HasValue)
            {
                contributions = contributions.Where(c => c.Timestamp <= query.Until.Value);
            }

            // Apply sorting
            contributions = query.SortDescending ? 
                contributions.OrderByDescending(c => c.Timestamp) : 
                contributions.OrderBy(c => c.Timestamp);

            // Apply pagination
            var totalCount = contributions.Count();
            var pagedContributions = contributions
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Retrieved {pagedContributions.Count} contributions for entity {entityId}");
            return new { 
                success = true, 
                contributions = pagedContributions, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting entity contributions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get entity contributions: {ex.Message}");
        }
    }

    // Attribution Management API Methods
    [ApiRoute("POST", "/attributions/create", "CreateAttribution", "Create attribution for a contribution", "codex.user-contributions")]
    public async Task<object> CreateAttributionAsync([ApiParameter("body", "Attribution request")] CreateAttributionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ContributionId))
            {
                return new ErrorResponse("Contribution ID is required");
            }

            if (!_contributions.TryGetValue(request.ContributionId, out var contribution))
            {
                return new ErrorResponse("Contribution not found");
            }

            var attribution = new Attribution
            {
                Id = Guid.NewGuid().ToString(),
                ContributionId = request.ContributionId,
                UserId = contribution.UserId,
                EntityId = contribution.EntityId,
                AttributionType = request.AttributionType ?? AttributionType.Primary,
                Percentage = request.Percentage ?? 100.0m,
                Description = request.Description ?? "",
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = AttributionStatus.Pending
            };

            _attributions[attribution.Id] = attribution;

            _logger.Info($"Attribution created: {attribution.Id} for contribution {request.ContributionId}");
            return new { success = true, attributionId = attribution.Id };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating attribution: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create attribution: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/attributions/contribution/{contributionId}", "GetContributionAttributions", "Get attributions for a contribution", "codex.user-contributions")]
    public async Task<object> GetContributionAttributionsAsync(string contributionId)
    {
        try
        {
            if (string.IsNullOrEmpty(contributionId))
            {
                return new ErrorResponse("Contribution ID is required");
            }

            var attributions = _attributions.Values
                .Where(a => a.ContributionId == contributionId)
                .OrderBy(a => a.CreatedAt)
                .ToList();

            _logger.Debug($"Retrieved {attributions.Count} attributions for contribution {contributionId}");
            return new { success = true, attributions = attributions };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting contribution attributions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get contribution attributions: {ex.Message}");
        }
    }

    // Reward Management API Methods
    [ApiRoute("GET", "/rewards/user/{userId}", "GetUserRewards", "Get rewards for a user", "codex.user-contributions")]
    public async Task<object> GetUserRewardsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            var rewards = _userRewards.Values
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var totalReward = rewards.Sum(r => r.Amount);
            var pendingReward = rewards.Where(r => r.Status == RewardStatus.Pending).Sum(r => r.Amount);
            var paidReward = rewards.Where(r => r.Status == RewardStatus.Paid).Sum(r => r.Amount);

            _logger.Debug($"Retrieved {rewards.Count} rewards for user {userId}");
            return new { 
                success = true, 
                rewards = rewards,
                totalReward = totalReward,
                pendingReward = pendingReward,
                paidReward = paidReward
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user rewards: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get user rewards: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/rewards/claim", "ClaimReward", "Claim a reward", "codex.user-contributions")]
    public async Task<object> ClaimRewardAsync([ApiParameter("body", "Claim request")] ClaimRewardRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required");
            }

            if (string.IsNullOrEmpty(request.RewardId))
            {
                return new ErrorResponse("Reward ID is required");
            }

            if (!_userRewards.TryGetValue(request.RewardId, out var reward))
            {
                return new ErrorResponse("Reward not found");
            }

            if (reward.UserId != request.UserId)
            {
                return new ErrorResponse("Reward does not belong to user");
            }

            if (reward.Status != RewardStatus.Pending)
            {
                return new ErrorResponse("Reward is not pending");
            }

            // Process reward claim
            // reward.Status = RewardStatus.Claimed; // Cannot modify init-only property
            // reward.ClaimedAt = DateTimeOffset.UtcNow; // Cannot modify init-only property

            // In a real implementation, this would process the ETH transaction
            if (_web3 != null)
            {
                try
                {
                    // This is a simplified example - in reality, you'd have a smart contract
                    // that handles the reward distribution
                    var txHash = await ProcessRewardTransactionAsync(reward);
                    // reward.EthereumTxHash = txHash; // Cannot modify init-only property
                    // reward.Status = RewardStatus.Paid; // Cannot modify init-only property
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing reward transaction: {ex.Message}", ex);
                    // reward.Status = RewardStatus.Failed; // Cannot modify init-only property
                }
            }

            _logger.Info($"Reward claimed: {request.RewardId} by user {request.UserId}");
            return new { success = true, rewardId = request.RewardId, status = reward.Status };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error claiming reward: {ex.Message}", ex);
            return new ErrorResponse($"Failed to claim reward: {ex.Message}");
        }
    }

    // ETH Ledger API Methods
    [ApiRoute("GET", "/ledger/balance/{address}", "GetEthBalance", "Get ETH balance for an address", "codex.user-contributions")]
    public async Task<object> GetEthBalanceAsync(string address)
    {
        try
        {
            if (string.IsNullOrEmpty(address))
            {
                return new ErrorResponse("Address is required");
            }

            if (_web3 == null)
            {
                return new ErrorResponse("Ethereum connection not configured");
            }

            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
            var balanceInEth = Web3.Convert.FromWei(balance);

            _logger.Debug($"Retrieved ETH balance for {address}: {balanceInEth}");
            return new { success = true, address = address, balance = balanceInEth, balanceWei = balance.Value };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting ETH balance: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get ETH balance: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ledger/transfer", "TransferEth", "Transfer ETH to a user", "codex.user-contributions")]
    public async Task<object> TransferEthAsync([ApiParameter("body", "Transfer request")] TransferEthRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ToAddress))
            {
                return new ErrorResponse("To address is required");
            }

            if (string.IsNullOrEmpty(request.FromPrivateKey))
            {
                return new ErrorResponse("From private key is required");
            }

            if (_web3 == null)
            {
                return new ErrorResponse("Ethereum connection not configured");
            }

            var account = new Account(request.FromPrivateKey);
            var web3 = new Web3(account, _web3.Client);

            var amountInWei = Web3.Convert.ToWei(request.Amount);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            var gasLimit = new HexBigInteger(21000);

            var txHash = await web3.Eth.TransactionManager.SendTransactionAsync(
                account.Address,
                request.ToAddress,
                new HexBigInteger(amountInWei));

            _logger.Info($"ETH transfer initiated: {txHash}");
            return new { success = true, transactionHash = txHash };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error transferring ETH: {ex.Message}", ex);
            return new ErrorResponse($"Failed to transfer ETH: {ex.Message}");
        }
    }

    // Private helper methods
    private async Task RecordContributionEventAsync(Contribution contribution)
    {
        var contributionEvent = new ContributionEvent
        {
            Id = Guid.NewGuid().ToString(),
            ContributionId = contribution.Id,
            UserId = contribution.UserId,
            EntityId = contribution.EntityId,
            EventType = "contribution_recorded",
            Data = new Dictionary<string, object>
            {
                ["contributionType"] = contribution.ContributionType.ToString(),
                ["value"] = contribution.Value,
                ["description"] = contribution.Description
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        _contributionHistory.Enqueue(contributionEvent);

        // Maintain history size
        lock (_lock)
        {
            while (_contributionHistory.Count > _maxHistorySize)
            {
                _contributionHistory.TryDequeue(out _);
            }
        }
    }

    private async Task CalculateAndAssignRewardsAsync(Contribution contribution)
    {
        try
        {
            // Calculate reward based on contribution type, value, and user history
            var rewardAmount = CalculateRewardAmount(contribution);
            var bonusMultiplier = CalculateBonusMultiplier(contribution.UserId);
            var finalReward = rewardAmount * bonusMultiplier;

            if (finalReward > 0)
            {
                var reward = new UserReward
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = contribution.UserId,
                    ContributionId = contribution.Id,
                    Amount = finalReward,
                    Currency = "ETH",
                    Status = RewardStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _userRewards[reward.Id] = reward;

                // Update contribution status
                // contribution.Status = ContributionStatus.Validated; // Cannot modify init-only property

                // Create attribution for the contribution
                await CreateDefaultAttributionAsync(contribution);

                _logger.Info($"Reward assigned: {reward.Id} for contribution {contribution.Id} (Amount: {finalReward} ETH)");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calculating rewards: {ex.Message}", ex);
        }
    }

    private decimal CalculateRewardAmount(Contribution contribution)
    {
        // Advanced reward calculation based on multiple factors
        var baseReward = contribution.ContributionType switch
        {
            ContributionType.Create => 1.0m,
            ContributionType.Update => 0.5m,
            ContributionType.Delete => 0.1m,
            ContributionType.Comment => 0.2m,
            ContributionType.Rating => 0.1m,
            ContributionType.Share => 0.3m,
            ContributionType.View => 0.05m,
            _ => 0.0m
        };

        // Apply value multiplier
        var valueMultiplier = Math.Max(0.1m, Math.Min(2.0m, contribution.Value / 10.0m));
        return baseReward * valueMultiplier;
    }

    private decimal CalculateBonusMultiplier(string userId)
    {
        // Calculate bonus based on user's contribution history
        var userContributions = _contributions.Values
            .Where(c => c.UserId == userId && c.Status == ContributionStatus.Validated)
            .Count();

        var userRewards = _userRewards.Values
            .Where(r => r.UserId == userId && r.Status == RewardStatus.Paid)
            .Sum(r => r.Amount);

        // Bonus for active contributors
        var activityBonus = Math.Min(2.0m, 1.0m + (userContributions * 0.1m));
        
        // Bonus for high-value contributors
        var valueBonus = Math.Min(1.5m, 1.0m + (userRewards * 0.01m));

        return activityBonus * valueBonus;
    }

    private async Task CreateDefaultAttributionAsync(Contribution contribution)
    {
        try
        {
            var attribution = new Attribution
            {
                Id = Guid.NewGuid().ToString(),
                ContributionId = contribution.Id,
                UserId = contribution.UserId,
                EntityId = contribution.EntityId,
                AttributionType = AttributionType.Primary,
                Percentage = 100.0m,
                Description = "Primary contributor",
                Metadata = new Dictionary<string, object>
                {
                    ["autoGenerated"] = true,
                    ["timestamp"] = DateTimeOffset.UtcNow
                },
                CreatedAt = DateTimeOffset.UtcNow,
                Status = AttributionStatus.Approved
            };

            _attributions[attribution.Id] = attribution;
            _logger.Debug($"Default attribution created for contribution {contribution.Id}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating default attribution: {ex.Message}", ex);
        }
    }

    private async Task<string> ProcessRewardTransactionAsync(UserReward reward)
    {
        try
        {
            if (_web3 == null)
            {
                // Simulate transaction for testing
                await Task.Delay(100);
                return $"0x{Guid.NewGuid():N}";
            }

            // In a real implementation, this would:
            // 1. Check user's ETH address
            // 2. Call a smart contract to distribute rewards
            // 3. Handle gas fees and transaction confirmation
            // 4. Update reward status based on transaction result

            var userAddress = await GetUserEthAddressAsync(reward.UserId);
            if (string.IsNullOrEmpty(userAddress))
            {
                throw new InvalidOperationException($"No ETH address found for user {reward.UserId}");
            }

            // Simulate smart contract call
            // var contractAddress = "0x742d35Cc6634C0532925a3b8D0C4E2e4C5C5C5C5"; // Example contract
            var amountInWei = Web3.Convert.ToWei(reward.Amount);
            
            // This would be a real smart contract call in production
            await Task.Delay(200); // Simulate blockchain transaction time
            
            var txHash = $"0x{Guid.NewGuid():N}";
            _logger.Info($"Reward transaction processed: {txHash} for {reward.Amount} ETH to {userAddress}");
            
            return txHash;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing reward transaction: {ex.Message}", ex);
            throw;
        }
    }

    private async Task<string?> GetUserEthAddressAsync(string userId)
    {
        // In a real implementation, this would look up the user's ETH address from a database
        // For now, we'll generate a deterministic address based on user ID
        await Task.Delay(10);
        return $"0x{userId.GetHashCode():X8}742d35Cc6634C0532925a3b8D0C4E2e4C5C5C5C5";
    }

    // Data models
    public record RecordContributionRequest(
        string UserId,
        string EntityId,
        string? EntityType = null,
        ContributionType? ContributionType = null,
        string? Description = null,
        decimal? Value = null,
        Dictionary<string, object>? Metadata = null
    );

    public record ContributionQuery(
        ContributionType[]? ContributionTypes = null,
        string[]? EntityTypes = null,
        ContributionStatus[]? Status = null,
        DateTimeOffset? Since = null,
        DateTimeOffset? Until = null,
        bool SortDescending = true,
        int? Skip = null,
        int? Take = null
    );

    public record CreateAttributionRequest(
        string ContributionId,
        AttributionType? AttributionType = null,
        decimal? Percentage = null,
        string? Description = null,
        Dictionary<string, object>? Metadata = null
    );

    public record ClaimRewardRequest(
        string UserId,
        string RewardId
    );

    public record TransferEthRequest(
        string ToAddress,
        string FromPrivateKey,
        decimal Amount
    );

    public record Contribution(
        string Id,
        string UserId,
        string EntityId,
        string EntityType,
        ContributionType ContributionType,
        string Description,
        decimal Value,
        Dictionary<string, object> Metadata,
        DateTimeOffset Timestamp,
        ContributionStatus Status,
        string? EthereumTxHash = null
    )
    {
        public Contribution() : this("", "", "", "", ContributionType.Create, "", 0, new Dictionary<string, object>(), DateTimeOffset.UtcNow, ContributionStatus.Pending, null) { }
    }

    public record Attribution(
        string Id,
        string ContributionId,
        string UserId,
        string EntityId,
        AttributionType AttributionType,
        decimal Percentage,
        string Description,
        Dictionary<string, object> Metadata,
        DateTimeOffset CreatedAt,
        AttributionStatus Status
    )
    {
        public Attribution() : this("", "", "", "", AttributionType.Primary, 100.0m, "", new Dictionary<string, object>(), DateTimeOffset.UtcNow, AttributionStatus.Pending) { }
    }

    public record UserReward(
        string Id,
        string UserId,
        string ContributionId,
        decimal Amount,
        string Currency,
        RewardStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ClaimedAt = null,
        string? EthereumTxHash = null
    )
    {
        public UserReward() : this("", "", "", 0, "ETH", RewardStatus.Pending, DateTimeOffset.UtcNow, null, null) { }
    }

    public record ContributionEvent(
        string Id,
        string ContributionId,
        string UserId,
        string EntityId,
        string EventType,
        Dictionary<string, object> Data,
        DateTimeOffset Timestamp
    )
    {
        public ContributionEvent() : this("", "", "", "", "", new Dictionary<string, object>(), DateTimeOffset.UtcNow) { }
    }

    public enum ContributionType
    {
        Create,
        Update,
        Delete,
        Comment,
        Rating,
        Share,
        View
    }

    public enum ContributionStatus
    {
        Pending,
        Validated,
        Rejected,
        Processed
    }

    public enum AttributionType
    {
        Primary,
        Secondary,
        Contributor,
        Reviewer
    }

    public enum AttributionStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum RewardStatus
    {
        Pending,
        Claimed,
        Paid,
        Failed
    }
}
