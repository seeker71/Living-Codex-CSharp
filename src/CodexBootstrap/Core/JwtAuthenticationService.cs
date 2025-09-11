using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace CodexBootstrap.Core;

/// <summary>
/// JWT-based authentication service
/// </summary>
public class JwtAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly Dictionary<string, string> _refreshTokens = new();

    public JwtAuthenticationService(IUserRepository userRepository, JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings;
        _logger = new Log4NetLogger(typeof(JwtAuthenticationService));
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                return new AuthenticationResult(false, Error: "Invalid username or password");
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return new AuthenticationResult(false, Error: "Invalid username or password");
            }

            var token = await GenerateTokenAsync(user);
            var refreshToken = GenerateRefreshToken();
            _refreshTokens[refreshToken] = user.Id;

            // Update last login
            var updatedUser = user with { LastLoginAt = DateTime.UtcNow };
            await _userRepository.UpdateAsync(updatedUser);

            _logger.Info($"User {username} authenticated successfully");
            return new AuthenticationResult(
                true, 
                User: user, 
                Token: token, 
                RefreshToken: refreshToken,
                ExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Authentication failed for user {username}: {ex.Message}", ex);
            return new AuthenticationResult(false, Error: "Authentication failed");
        }
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string token)
    {
        try
        {
            var validationResult = await ValidateTokenAsync(token);
            if (!validationResult.IsValid || validationResult.User == null)
            {
                return new AuthenticationResult(false, Error: "Invalid token");
            }

            return new AuthenticationResult(
                true,
                User: validationResult.User,
                Token: token,
                ExpiresAt: validationResult.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Token authentication failed: {ex.Message}", ex);
            return new AuthenticationResult(false, Error: "Token authentication failed");
        }
    }

    public async Task<string> GenerateTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles as claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add custom claims from metadata
        if (user.Metadata != null)
        {
            foreach (var kvp in user.Metadata)
            {
                claims.Add(new Claim($"custom:{kvp.Key}", kvp.Value.ToString() ?? ""));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> RefreshTokenAsync(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        // Remove old refresh token
        _refreshTokens.Remove(refreshToken);

        // Generate new tokens
        var newToken = await GenerateTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();
        _refreshTokens[newRefreshToken] = user.Id;

        return newToken;
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var jti = jwtToken.Claims.FirstOrDefault(x => x.Type == "jti")?.Value;
            
            if (jti != null)
            {
                // In a real implementation, you'd store revoked tokens in a database
                // For now, we'll just log it
                _logger.Info($"Token revoked: {jti}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to revoke token: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return new TokenValidationResult(false, Error: "Invalid token claims");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return new TokenValidationResult(false, Error: "User not found or inactive");
            }

            return new TokenValidationResult(
                true,
                User: user,
                Principal: principal,
                ExpiresAt: jwtToken.ValidTo
            );
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult(false, Error: "Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult(false, Error: "Invalid token signature");
        }
        catch (Exception ex)
        {
            _logger.Error($"Token validation failed: {ex.Message}", ex);
            return new TokenValidationResult(false, Error: "Token validation failed");
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token)
    {
        var validationResult = await ValidateTokenAsync(token);
        return validationResult.User;
    }

    public async Task<RegistrationResult> RegisterUserAsync(string username, string email, string password, string[]? roles = null)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                return new RegistrationResult(false, Error: "Username already exists");
            }

            var existingEmail = await _userRepository.GetByEmailAsync(email);
            if (existingEmail != null)
            {
                return new RegistrationResult(false, Error: "Email already exists");
            }

            // Create new user
            var user = new User(
                Id: Guid.NewGuid().ToString(),
                Username: username,
                Email: email,
                PasswordHash: HashPassword(password),
                Roles: roles ?? new[] { "user" },
                IsActive: true,
                CreatedAt: DateTime.UtcNow,
                LastLoginAt: null
            );

            await _userRepository.CreateAsync(user);
            _logger.Info($"User {username} registered successfully");

            return new RegistrationResult(true, User: user);
        }
        catch (Exception ex)
        {
            _logger.Error($"User registration failed for {username}: {ex.Message}", ex);
            return new RegistrationResult(false, Error: "Registration failed");
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                return false;
            }

            var updatedUser = user with { PasswordHash = HashPassword(newPassword) };
            await _userRepository.UpdateAsync(updatedUser);

            _logger.Info($"Password changed for user {user.Username}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Password change failed for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if email exists or not
                return true;
            }

            // In a real implementation, you'd send a password reset email
            // For now, we'll just log it
            _logger.Info($"Password reset requested for user {user.Username}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Password reset failed for email {email}: {ex.Message}", ex);
            return false;
        }
    }

    private string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[32];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[64];
        Array.Copy(salt, 0, hashBytes, 0, 32);
        Array.Copy(hash, 0, hashBytes, 32, 32);

        return Convert.ToBase64String(hashBytes);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            var salt = new byte[32];
            Array.Copy(hashBytes, 0, salt, 0, 32);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            for (int i = 0; i < 32; i++)
            {
                if (hashBytes[i + 32] != hash[i])
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

/// <summary>
/// JWT settings configuration
/// </summary>
public record JwtSettings(
    string SecretKey,
    string Issuer,
    string Audience,
    int ExpirationMinutes = 60,
    int RefreshTokenExpirationDays = 7
);
