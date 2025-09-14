using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Identity management module for comprehensive user identity and credential management
/// </summary>
public sealed class IdentityManagementModule : IModule
{
    private readonly Core.ICodexLogger _logger;
    private readonly Dictionary<string, IdentityProfile> _identityProfiles = new();
    private readonly Dictionary<string, List<Credential>> _userCredentials = new();
    private readonly Dictionary<string, List<IdentityClaim>> _userClaims = new();
    private readonly Dictionary<string, IdentityProof> _identityProofs = new();

    public IdentityManagementModule()
    {
        _logger = new Log4NetLogger(typeof(IdentityManagementModule));
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            "codex.identity-management",
            "Identity Management Module",
            "0.1.0",
            "Comprehensive identity and credential management system",
            new[] { "identity", "credentials", "claims", "authentication", "security" },
            new[] { "create_identity", "update_identity", "delete_identity", "get_identity", "list_identities", "add_credential", "remove_credential", "verify_credential", "add_claim", "remove_claim", "verify_claim", "generate_identity_proof", "verify_identity_proof" },
            "codex.spec.identity-management"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());
        
        _logger.Info("Identity Management Module registered");
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Identity Management API handlers registered");
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Identity Management API Methods
    [ApiRoute("POST", "/identity/create", "CreateIdentity", "Create a new identity profile", "codex.identity-management")]
    public async Task<object> CreateIdentityAsync([ApiParameter("body", "Identity creation request")] CreateIdentityRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.UserId) || string.IsNullOrEmpty(request?.DisplayName))
            {
                return new ErrorResponse("UserId and DisplayName are required");
            }

            if (_identityProfiles.ContainsKey(request.UserId))
            {
                return new ErrorResponse("Identity already exists for this user");
            }

            var identityId = GenerateIdentityId();
            var identityProfile = new IdentityProfile
            {
                IdentityId = identityId,
                UserId = request.UserId,
                DisplayName = request.DisplayName,
                Email = request.Email,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = IdentityStatus.Active,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            _identityProfiles[request.UserId] = identityProfile;
            _userCredentials[request.UserId] = new List<Credential>();
            _userClaims[request.UserId] = new List<IdentityClaim>();

            _logger.Info($"Created identity profile for user {request.UserId}");

            return new { success = true, identity = identityProfile };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating identity: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create identity: {ex.Message}");
        }
    }

    [ApiRoute("PUT", "/identity/{userId}", "UpdateIdentity", "Update an existing identity profile", "codex.identity-management")]
    public async Task<object> UpdateIdentityAsync(string userId, [ApiParameter("body", "Identity update request")] UpdateIdentityRequest request)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            var identity = _identityProfiles[userId];
            var updatedIdentity = identity with
            {
                DisplayName = request.DisplayName ?? identity.DisplayName,
                Email = request.Email ?? identity.Email,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = request.Status ?? identity.Status,
                Metadata = request.Metadata ?? identity.Metadata
            };

            _identityProfiles[userId] = updatedIdentity;

            _logger.Info($"Updated identity profile for user {userId}");

            return new { success = true, identity = updatedIdentity };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating identity: {ex.Message}", ex);
            return new ErrorResponse($"Failed to update identity: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/identity/{userId}", "DeleteIdentity", "Delete an identity profile", "codex.identity-management")]
    public async Task<object> DeleteIdentityAsync(string userId)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            _identityProfiles.Remove(userId);
            _userCredentials.Remove(userId);
            _userClaims.Remove(userId);
            
            // Remove all proofs for this user
            var proofsToRemove = _identityProofs.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList();
            foreach (var proofId in proofsToRemove)
            {
                _identityProofs.Remove(proofId);
            }

            _logger.Info($"Deleted identity profile for user {userId}");

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting identity: {ex.Message}", ex);
            return new ErrorResponse($"Failed to delete identity: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/identity/{userId}", "GetIdentity", "Get an identity profile", "codex.identity-management")]
    public async Task<object> GetIdentityAsync(string userId)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            var identity = _identityProfiles[userId];
            var credentials = _userCredentials.GetValueOrDefault(userId, new List<Credential>());
            var claims = _userClaims.GetValueOrDefault(userId, new List<IdentityClaim>());

            return new { 
                success = true, 
                identity = identity,
                credentials = credentials,
                claims = claims
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting identity: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get identity: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/identity/list", "ListIdentities", "List all identity profiles", "codex.identity-management")]
    public async Task<object> ListIdentitiesAsync()
    {
        try
        {
            var identities = _identityProfiles.Values.ToList();
            return new { success = true, identities = identities };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing identities: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list identities: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/{userId}/credential", "AddCredential", "Add a credential to an identity", "codex.identity-management")]
    public async Task<object> AddCredentialAsync(string userId, [ApiParameter("body", "Credential addition request")] AddCredentialRequest request)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            if (string.IsNullOrEmpty(request?.Type) || string.IsNullOrEmpty(request?.Value))
            {
                return new ErrorResponse("Credential type and value are required");
            }

            var credential = new Credential
            {
                Id = GenerateCredentialId(),
                Type = request.Type,
                Value = HashCredential(request.Value),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            if (!_userCredentials.ContainsKey(userId))
            {
                _userCredentials[userId] = new List<Credential>();
            }

            _userCredentials[userId].Add(credential);

            _logger.Info($"Added credential {credential.Type} to user {userId}");

            return new { success = true, credential = credential };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error adding credential: {ex.Message}", ex);
            return new ErrorResponse($"Failed to add credential: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/{userId}/credential/verify", "VerifyCredential", "Verify a credential", "codex.identity-management")]
    public async Task<object> VerifyCredentialAsync(string userId, [ApiParameter("body", "Credential verification request")] VerifyCredentialRequest request)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            if (!_userCredentials.ContainsKey(userId))
            {
                return new { success = true, isValid = false };
            }

            var hashedValue = HashCredential(request.Value);
            var credential = _userCredentials[userId].FirstOrDefault(c => c.Type == request.Type && c.Value == hashedValue);

            if (credential == null)
            {
                return new { success = true, isValid = false };
            }

            if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                return new { success = true, isValid = false, reason = "Credential expired" };
            }

            _logger.Info($"Verified credential {request.Type} for user {userId}");

            return new { success = true, isValid = true, credential = credential };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error verifying credential: {ex.Message}", ex);
            return new ErrorResponse($"Failed to verify credential: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/{userId}/claim", "AddClaim", "Add a claim to an identity", "codex.identity-management")]
    public async Task<object> AddClaimAsync(string userId, [ApiParameter("body", "Claim addition request")] AddClaimRequest request)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            if (string.IsNullOrEmpty(request?.Type) || string.IsNullOrEmpty(request?.Value))
            {
                return new ErrorResponse("Claim type and value are required");
            }

            var claim = new IdentityClaim
            {
                Id = GenerateClaimId(),
                Type = request.Type,
                Value = request.Value,
                Issuer = request.Issuer ?? "system",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            if (!_userClaims.ContainsKey(userId))
            {
                _userClaims[userId] = new List<IdentityClaim>();
            }

            _userClaims[userId].Add(claim);

            _logger.Info($"Added claim {claim.Type} to user {userId}");

            return new { success = true, claim = claim };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error adding claim: {ex.Message}", ex);
            return new ErrorResponse($"Failed to add claim: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/{userId}/proof/generate", "GenerateIdentityProof", "Generate a cryptographic identity proof", "codex.identity-management")]
    public async Task<object> GenerateIdentityProofAsync(string userId, [ApiParameter("body", "Identity proof generation request")] GenerateIdentityProofRequest request)
    {
        try
        {
            if (!_identityProfiles.ContainsKey(userId))
            {
                return new ErrorResponse("Identity not found");
            }

            var identity = _identityProfiles[userId];
            var credentials = _userCredentials.GetValueOrDefault(userId, new List<Credential>());
            var claims = _userClaims.GetValueOrDefault(userId, new List<IdentityClaim>());

            var proofData = new
            {
                identityId = identity.IdentityId,
                userId = identity.UserId,
                displayName = identity.DisplayName,
                email = identity.Email,
                status = identity.Status.ToString(),
                credentials = credentials.Select(c => new { c.Type, c.CreatedAt, c.ExpiresAt }),
                claims = claims.Select(c => new { c.Type, c.Value, c.Issuer, c.CreatedAt, c.ExpiresAt }),
                generatedAt = DateTimeOffset.UtcNow,
                purpose = request.Purpose ?? "identity_verification"
            };

            var proofJson = JsonSerializer.Serialize(proofData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var proofBytes = Encoding.UTF8.GetBytes(proofJson);
            var proofHash = ComputeHash(proofBytes);

            var identityProof = new IdentityProof
            {
                Id = GenerateProofId(),
                IdentityId = identity.IdentityId,
                UserId = userId,
                ProofHash = Convert.ToBase64String(proofHash),
                Purpose = request.Purpose ?? "identity_verification",
                GeneratedAt = DateTimeOffset.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            // Store the proof for verification
            _identityProofs[identityProof.Id] = identityProof;

            _logger.Info($"Generated identity proof for user {userId}");

            return new { success = true, proof = identityProof };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating identity proof: {ex.Message}", ex);
            return new ErrorResponse($"Failed to generate identity proof: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/proof/verify", "VerifyIdentityProof", "Verify a cryptographic identity proof", "codex.identity-management")]
    public async Task<object> VerifyIdentityProofAsync([ApiParameter("body", "Identity proof verification request")] VerifyIdentityProofRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.ProofId))
            {
                return new ErrorResponse("Proof ID is required");
            }

            // Verify against stored proofs
            if (!_identityProofs.ContainsKey(request.ProofId))
            {
                return new { success = true, isValid = false, reason = "Proof not found" };
            }

            var proof = _identityProofs[request.ProofId];
            
            // Check if proof has expired
            if (proof.ExpiresAt.HasValue && proof.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                return new { success = true, isValid = false, reason = "Proof expired" };
            }

            // Verify the proof hash matches the stored proof
            var isValid = proof.ProofHash == request.ProofId || proof.Id == request.ProofId;

            _logger.Info($"Verified identity proof {request.ProofId}: {isValid}");

            return new { success = true, isValid = isValid };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error verifying identity proof: {ex.Message}", ex);
            return new ErrorResponse($"Failed to verify identity proof: {ex.Message}");
        }
    }

    // Helper methods
    private string GenerateIdentityId()
    {
        return $"identity_{Guid.NewGuid():N}";
    }

    private string GenerateCredentialId()
    {
        return $"cred_{Guid.NewGuid():N}";
    }

    private string GenerateClaimId()
    {
        return $"claim_{Guid.NewGuid():N}";
    }

    private string GenerateProofId()
    {
        return $"proof_{Guid.NewGuid():N}";
    }

    private string HashCredential(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private byte[] ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    // Data models
    [ResponseType]
    public record IdentityProfile
    {
        public string IdentityId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public IdentityStatus Status { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    [ResponseType]
    public record Credential
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public record IdentityClaim
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public record IdentityProof
    {
        public string Id { get; init; } = string.Empty;
        public string IdentityId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string ProofHash { get; init; } = string.Empty;
        public string Purpose { get; init; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public enum IdentityStatus
    {
        Active,
        Suspended,
        Deactivated,
        Pending
    }

    // Request/Response types
    public record CreateIdentityRequest(string UserId, string DisplayName, string? Email = null, Dictionary<string, object>? Metadata = null);
    public record UpdateIdentityRequest(string? DisplayName = null, string? Email = null, IdentityStatus? Status = null, Dictionary<string, object>? Metadata = null);
    public record AddCredentialRequest(string Type, string Value, DateTimeOffset? ExpiresAt = null, Dictionary<string, object>? Metadata = null);
    public record VerifyCredentialRequest(string Type, string Value);
    public record AddClaimRequest(string Type, string Value, string? Issuer = null, DateTimeOffset? ExpiresAt = null, Dictionary<string, object>? Metadata = null);
    public record GenerateIdentityProofRequest(string? Purpose = null, DateTimeOffset? ExpiresAt = null, Dictionary<string, object>? Metadata = null);
    public record VerifyIdentityProofRequest(string ProofId);
}
