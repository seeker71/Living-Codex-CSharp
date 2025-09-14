using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Digital signature module for cryptographic signing and verification of nodes and edges
/// </summary>
public sealed class DigitalSignatureModule : IModule
{
    private readonly Core.ICodexLogger _logger;
    private const string SignatureKey = "digitalSignature";
    private const string PublicKeyKey = "publicKey";
    private const string AlgorithmKey = "signatureAlgorithm";
    private const string TimestampKey = "signatureTimestamp";
    private const string Algorithm = "ECDSA-P256";

    public DigitalSignatureModule()
    {
        _logger = new Log4NetLogger(typeof(DigitalSignatureModule));
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            "codex.digital-signature",
            "Digital Signature Module",
            "0.1.0",
            "Cryptographic signing and verification for nodes and edges",
            new[] { "cryptography", "digital-signature", "security", "verification" },
            new[] { "sign_node", "verify_node", "sign_edge", "verify_edge", "generate_keypair", "extract_public_key" },
            "codex.spec.digital-signature"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());
        
        _logger.Info("Digital Signature Module registered");
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Digital Signature API handlers registered");
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Digital signature implementation methods
    [ApiRoute("POST", "/signature/sign-node", "SignNode", "Sign a node with digital signature", "codex.digital-signature")]
    public async Task<object> SignNodeAsync([ApiParameter("body", "Request containing node and private key")] SignNodeRequest request)
    {
        try
        {
            if (request?.Node == null || request.PrivateKey == null)
            {
                return new ErrorResponse("Node and private key are required");
            }

            var signedNode = SignNode(request.Node, request.PrivateKey);
            return new { success = true, signedNode };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error signing node: {ex.Message}", ex);
            return new ErrorResponse($"Failed to sign node: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/signature/verify-node", "VerifyNode", "Verify a node's digital signature", "codex.digital-signature")]
    public async Task<object> VerifyNodeAsync([ApiParameter("body", "Request containing node and public key")] VerifyNodeRequest request)
    {
        try
        {
            if (request?.Node == null || request.PublicKey == null)
            {
                return new ErrorResponse("Node and public key are required");
            }

            var isValid = VerifyNodeSignature(request.Node, request.PublicKey);
            return new { success = true, isValid };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error verifying node: {ex.Message}", ex);
            return new ErrorResponse($"Failed to verify node: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/signature/sign-edge", "SignEdge", "Sign an edge with digital signature", "codex.digital-signature")]
    public async Task<object> SignEdgeAsync([ApiParameter("body", "Request containing edge and private key")] SignEdgeRequest request)
    {
        try
        {
            if (request?.Edge == null || request.PrivateKey == null)
            {
                return new ErrorResponse("Edge and private key are required");
            }

            var signedEdge = SignEdge(request.Edge, request.PrivateKey);
            return new { success = true, signedEdge };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error signing edge: {ex.Message}", ex);
            return new ErrorResponse($"Failed to sign edge: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/signature/verify-edge", "VerifyEdge", "Verify an edge's digital signature", "codex.digital-signature")]
    public async Task<object> VerifyEdgeAsync([ApiParameter("body", "Request containing edge and public key")] VerifyEdgeRequest request)
    {
        try
        {
            if (request?.Edge == null || request.PublicKey == null)
            {
                return new ErrorResponse("Edge and public key are required");
            }

            var isValid = VerifyEdgeSignature(request.Edge, request.PublicKey);
            return new { success = true, isValid };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error verifying edge: {ex.Message}", ex);
            return new ErrorResponse($"Failed to verify edge: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/signature/generate-keypair", "GenerateKeyPair", "Generate a new cryptographic key pair", "codex.digital-signature")]
    public async Task<object> GenerateKeyPairAsync()
    {
        try
        {
            var (privateKey, publicKey) = GenerateKeyPair();
            return new { 
                success = true, 
                privateKey = Convert.ToBase64String(privateKey),
                publicKey = Convert.ToBase64String(publicKey),
                algorithm = Algorithm
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating key pair: {ex.Message}", ex);
            return new ErrorResponse($"Failed to generate key pair: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/signature/extract-public-key", "ExtractPublicKey", "Extract public key from a signed item", "codex.digital-signature")]
    public async Task<object> ExtractPublicKeyAsync([ApiParameter("body", "Request containing signed item")] ExtractPublicKeyRequest request)
    {
        try
        {
            if (request?.SignedItem == null)
            {
                return new ErrorResponse("Signed item is required");
            }

            var publicKey = ExtractPublicKey(request.SignedItem);
            return new { 
                success = true, 
                publicKey = publicKey != null ? Convert.ToBase64String(publicKey) : null
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error extracting public key: {ex.Message}", ex);
            return new ErrorResponse($"Failed to extract public key: {ex.Message}");
        }
    }

    private byte[] SignData(byte[] data, byte[] privateKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(privateKey, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to sign data: {ex.Message}", ex);
            throw;
        }
    }

    private bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to verify signature: {ex.Message}", ex);
            return false;
        }
    }

    private (byte[] privateKey, byte[] publicKey) GenerateKeyPair()
    {
        try
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = ecdsa.ExportPkcs8PrivateKey();
            var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            return (privateKey, publicKey);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to generate key pair: {ex.Message}", ex);
            throw;
        }
    }

    private Node SignNode(Node node, byte[] privateKey)
    {
        try
        {
            // Create a copy of the node without signature metadata for signing
            var nodeForSigning = CloneNode(node);
            var cleanMeta = new Dictionary<string, object>();
            
            if (nodeForSigning.Meta != null)
            {
                foreach (var kvp in nodeForSigning.Meta)
                {
                    if (kvp.Key != SignatureKey && kvp.Key != PublicKeyKey && 
                        kvp.Key != AlgorithmKey && kvp.Key != TimestampKey)
                    {
                        cleanMeta[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Serialize the node for signing (excluding signature metadata)
            var nodeJson = JsonSerializer.Serialize(nodeForSigning with { Meta = cleanMeta }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var nodeBytes = Encoding.UTF8.GetBytes(nodeJson);

            // Sign the data
            var signature = SignData(nodeBytes, privateKey);
            var publicKey = ExtractPublicKeyFromPrivateKey(privateKey);

            // Create new node with signature metadata
            var signedMeta = new Dictionary<string, object>();
            if (node.Meta != null)
            {
                foreach (var kvp in node.Meta)
                {
                    signedMeta[kvp.Key] = kvp.Value;
                }
            }
            signedMeta[SignatureKey] = Convert.ToBase64String(signature);
            signedMeta[PublicKeyKey] = Convert.ToBase64String(publicKey);
            signedMeta[AlgorithmKey] = Algorithm;
            signedMeta[TimestampKey] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var signedNode = node with { Meta = signedMeta };

            _logger.Debug($"Signed node {signedNode.Id} with {Algorithm}");
            return signedNode;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to sign node {node.Id}: {ex.Message}", ex);
            throw;
        }
    }

    private bool VerifyNodeSignature(Node node, byte[] publicKey)
    {
        try
        {
            if (!node.Meta?.ContainsKey(SignatureKey) == true)
            {
                _logger.Warn($"Node {node.Id} has no digital signature");
                return false;
            }

            // Extract signature metadata
            var signatureB64 = node.Meta[SignatureKey]?.ToString();
            if (string.IsNullOrEmpty(signatureB64))
            {
                _logger.Warn($"Node {node.Id} has empty signature");
                return false;
            }

            var signature = Convert.FromBase64String(signatureB64);

            // Create a copy without signature metadata for verification
            var cleanMeta = new Dictionary<string, object>();
            if (node.Meta != null)
            {
                foreach (var kvp in node.Meta)
                {
                    if (kvp.Key != SignatureKey && kvp.Key != PublicKeyKey && 
                        kvp.Key != AlgorithmKey && kvp.Key != TimestampKey)
                    {
                        cleanMeta[kvp.Key] = kvp.Value;
                    }
                }
            }

            var nodeForVerification = node with { Meta = cleanMeta };

            // Serialize the node for verification
            var nodeJson = JsonSerializer.Serialize(nodeForVerification, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var nodeBytes = Encoding.UTF8.GetBytes(nodeJson);

            // Verify the signature
            var isValid = VerifySignature(nodeBytes, signature, publicKey);
            
            if (isValid)
            {
                _logger.Debug($"Node {node.Id} signature verified successfully");
            }
            else
            {
                _logger.Warn($"Node {node.Id} signature verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to verify node {node.Id} signature: {ex.Message}", ex);
            return false;
        }
    }

    private Edge SignEdge(Edge edge, byte[] privateKey)
    {
        try
        {
            // Create a copy of the edge without signature metadata for signing
            var cleanMeta = new Dictionary<string, object>();
            
            if (edge.Meta != null)
            {
                foreach (var kvp in edge.Meta)
                {
                    if (kvp.Key != SignatureKey && kvp.Key != PublicKeyKey && 
                        kvp.Key != AlgorithmKey && kvp.Key != TimestampKey)
                    {
                        cleanMeta[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Serialize the edge for signing (excluding signature metadata)
            var edgeJson = JsonSerializer.Serialize(edge with { Meta = cleanMeta }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var edgeBytes = Encoding.UTF8.GetBytes(edgeJson);

            // Sign the data
            var signature = SignData(edgeBytes, privateKey);
            var publicKey = ExtractPublicKeyFromPrivateKey(privateKey);

            // Create new edge with signature metadata
            var signedMeta = new Dictionary<string, object>();
            if (edge.Meta != null)
            {
                foreach (var kvp in edge.Meta)
                {
                    signedMeta[kvp.Key] = kvp.Value;
                }
            }
            signedMeta[SignatureKey] = Convert.ToBase64String(signature);
            signedMeta[PublicKeyKey] = Convert.ToBase64String(publicKey);
            signedMeta[AlgorithmKey] = Algorithm;
            signedMeta[TimestampKey] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var signedEdge = edge with { Meta = signedMeta };

            _logger.Debug($"Signed edge {edge.FromId}->{edge.ToId} with {Algorithm}");
            return signedEdge;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to sign edge {edge.FromId}->{edge.ToId}: {ex.Message}", ex);
            throw;
        }
    }

    private bool VerifyEdgeSignature(Edge edge, byte[] publicKey)
    {
        try
        {
            if (!edge.Meta?.ContainsKey(SignatureKey) == true)
            {
                _logger.Warn($"Edge {edge.FromId}->{edge.ToId} has no digital signature");
                return false;
            }

            // Extract signature metadata
            var signatureB64 = edge.Meta[SignatureKey]?.ToString();
            if (string.IsNullOrEmpty(signatureB64))
            {
                _logger.Warn($"Edge {edge.FromId}->{edge.ToId} has empty signature");
                return false;
            }

            var signature = Convert.FromBase64String(signatureB64);

            // Create a copy without signature metadata for verification
            var cleanMeta = new Dictionary<string, object>();
            if (edge.Meta != null)
            {
                foreach (var kvp in edge.Meta)
                {
                    if (kvp.Key != SignatureKey && kvp.Key != PublicKeyKey && 
                        kvp.Key != AlgorithmKey && kvp.Key != TimestampKey)
                    {
                        cleanMeta[kvp.Key] = kvp.Value;
                    }
                }
            }

            var edgeForVerification = edge with { Meta = cleanMeta };

            // Serialize the edge for verification
            var edgeJson = JsonSerializer.Serialize(edgeForVerification, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var edgeBytes = Encoding.UTF8.GetBytes(edgeJson);

            // Verify the signature
            var isValid = VerifySignature(edgeBytes, signature, publicKey);
            
            if (isValid)
            {
                _logger.Debug($"Edge {edge.FromId}->{edge.ToId} signature verified successfully");
            }
            else
            {
                _logger.Warn($"Edge {edge.FromId}->{edge.ToId} signature verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to verify edge {edge.FromId}->{edge.ToId} signature: {ex.Message}", ex);
            return false;
        }
    }

    private byte[]? ExtractPublicKey(object signedItem)
    {
        try
        {
            if (signedItem is Node node && node.Meta?.ContainsKey(PublicKeyKey) == true)
            {
                var publicKeyB64 = node.Meta[PublicKeyKey]?.ToString();
                return string.IsNullOrEmpty(publicKeyB64) ? null : Convert.FromBase64String(publicKeyB64);
            }
            
            if (signedItem is Edge edge && edge.Meta?.ContainsKey(PublicKeyKey) == true)
            {
                var publicKeyB64 = edge.Meta[PublicKeyKey]?.ToString();
                return string.IsNullOrEmpty(publicKeyB64) ? null : Convert.FromBase64String(publicKeyB64);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to extract public key: {ex.Message}", ex);
            return null;
        }
    }

    private byte[] ExtractPublicKeyFromPrivateKey(byte[] privateKey)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKey, out _);
        return ecdsa.ExportSubjectPublicKeyInfo();
    }

    private Node CloneNode(Node node)
    {
        return node.DeepClone();
    }

    private Edge CloneEdge(Edge edge)
    {
        return edge.DeepClone();
    }

    // Request/Response types
    [ResponseType]
    public record SignNodeRequest(Node Node, byte[] PrivateKey);
    [ResponseType]
    public record VerifyNodeRequest(Node Node, byte[] PublicKey);
    [ResponseType]
    public record SignEdgeRequest(Edge Edge, byte[] PrivateKey);
    [ResponseType]
    public record VerifyEdgeRequest(Edge Edge, byte[] PublicKey);
    [ResponseType]
    public record ExtractPublicKeyRequest(object SignedItem);
}
