using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using Microsoft.IdentityModel.Tokens;

namespace CodexBootstrap.Core.Security
{
    /// <summary>
    /// Basic authentication service for the Living Codex system
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ICodexLogger _logger;
        private readonly IUserRepository _userRepository;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly int _jwtExpirationMinutes;
        private readonly Dictionary<string, UserSession> _activeSessions;

        public AuthenticationService(ICodexLogger logger, IUserRepository userRepository, string jwtSecret = null, string jwtIssuer = "LivingCodex", int jwtExpirationMinutes = 60)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtSecret = jwtSecret ?? GenerateRandomSecret();
            _jwtIssuer = jwtIssuer;
            _jwtExpirationMinutes = jwtExpirationMinutes;
            _activeSessions = new Dictionary<string, UserSession>();
        }

        /// <summary>
        /// Authenticates a user with email and password
        /// </summary>
        public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return AuthenticationResult.Failure("Email and password are required");
                }

                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.Warn($"Authentication failed: User not found for email {email}");
                    return AuthenticationResult.Failure("Invalid credentials");
                }

                if (!user.IsActive)
                {
                    _logger.Warn($"Authentication failed: User {email} is inactive");
                    return AuthenticationResult.Failure("Account is inactive");
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.Warn($"Authentication failed: Invalid password for user {email}");
                    return AuthenticationResult.Failure("Invalid credentials");
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Create session
                var session = new UserSession
                {
                    UserId = user.Id,
                    Email = user.Email,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                    IsActive = true
                };

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _activeSessions[token] = session;

                _logger.Info($"User {email} authenticated successfully");
                return AuthenticationResult.Success(token, user);
            }
            catch (Exception ex)
            {
                _logger.Error($"Authentication error for user {email}: {ex.Message}", ex);
                return AuthenticationResult.Failure("Authentication failed due to system error");
            }
        }

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        public async Task<AuthenticationResult> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return AuthenticationResult.Failure("Token is required");
                }

                // Check if session exists and is active
                if (!_activeSessions.TryGetValue(token, out var session))
                {
                    return AuthenticationResult.Failure("Invalid or expired token");
                }

                if (!session.IsActive || session.ExpiresAt < DateTime.UtcNow)
                {
                    _activeSessions.Remove(token);
                    return AuthenticationResult.Failure("Token has expired");
                }

                // Get user to ensure they're still active
                var user = await _userRepository.GetByIdAsync(session.UserId);
                if (user == null || !user.IsActive)
                {
                    _activeSessions.Remove(token);
                    return AuthenticationResult.Failure("User account is no longer active");
                }

                return AuthenticationResult.Success(token, user);
            }
            catch (Exception ex)
            {
                _logger.Error($"Token validation error: {ex.Message}", ex);
                return AuthenticationResult.Failure("Token validation failed");
            }
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        public async Task<AuthenticationResult> RegisterAsync(string email, string password, string displayName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return AuthenticationResult.Failure("Email and password are required");
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(email);
                if (existingUser != null)
                {
                    return AuthenticationResult.Failure("User with this email already exists");
                }

                // Validate password strength
                var passwordValidation = ValidatePasswordStrength(password);
                if (!passwordValidation.IsValid)
                {
                    return AuthenticationResult.Failure(passwordValidation.ErrorMessage);
                }

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    DisplayName = displayName ?? email,
                    PasswordHash = HashPassword(password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.CreateAsync(user);

                _logger.Info($"New user registered: {email}");
                return AuthenticationResult.Success(null, user);
            }
            catch (Exception ex)
            {
                _logger.Error($"User registration error for {email}: {ex.Message}", ex);
                return AuthenticationResult.Failure("Registration failed due to system error");
            }
        }

        /// <summary>
        /// Logs out a user by invalidating their token
        /// </summary>
        public Task LogoutAsync(string token)
        {
            if (!string.IsNullOrEmpty(token) && _activeSessions.ContainsKey(token))
            {
                _activeSessions.Remove(token);
                _logger.Info($"User logged out successfully");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Changes a user's password
        /// </summary>
        public async Task<AuthenticationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return AuthenticationResult.Failure("User not found");
                }

                if (!VerifyPassword(currentPassword, user.PasswordHash))
                {
                    return AuthenticationResult.Failure("Current password is incorrect");
                }

                var passwordValidation = ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsValid)
                {
                    return AuthenticationResult.Failure(passwordValidation.ErrorMessage);
                }

                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.Info($"Password changed for user {user.Email}");
                return AuthenticationResult.Success(null, user);
            }
            catch (Exception ex)
            {
                _logger.Error($"Password change error for user {userId}: {ex.Message}", ex);
                return AuthenticationResult.Failure("Password change failed due to system error");
            }
        }

        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.DisplayName),
                new("jti", Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                Issuer = _jwtIssuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Hashes a password using PBKDF2
        /// </summary>
        private string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[32];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var combined = new byte[64];
            Array.Copy(salt, 0, combined, 0, 32);
            Array.Copy(hash, 0, combined, 32, 32);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                var combined = Convert.FromBase64String(passwordHash);
                var salt = new byte[32];
                var hash = new byte[32];

                Array.Copy(combined, 0, salt, 0, 32);
                Array.Copy(combined, 32, hash, 0, 32);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                var computedHash = pbkdf2.GetBytes(32);

                return CryptographicOperations.FixedTimeEquals(hash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        private ValidationResult ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return ValidationResult.Error("Password is required");
            }

            if (password.Length < 8)
            {
                return ValidationResult.Error("Password must be at least 8 characters long");
            }

            if (password.Length > 128)
            {
                return ValidationResult.Error("Password must be no more than 128 characters long");
            }

            // Check for at least one uppercase letter
            if (!password.Any(char.IsUpper))
            {
                return ValidationResult.Error("Password must contain at least one uppercase letter");
            }

            // Check for at least one lowercase letter
            if (!password.Any(char.IsLower))
            {
                return ValidationResult.Error("Password must contain at least one lowercase letter");
            }

            // Check for at least one digit
            if (!password.Any(char.IsDigit))
            {
                return ValidationResult.Error("Password must contain at least one digit");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Generates a random secret for JWT signing
        /// </summary>
        private string GenerateRandomSecret()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    /// <summary>
    /// Interface for authentication service
    /// </summary>
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync(string email, string password);
        Task<AuthenticationResult> ValidateTokenAsync(string token);
        Task<AuthenticationResult> RegisterAsync(string email, string password, string displayName = null);
        Task LogoutAsync(string token);
        Task<AuthenticationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    }

    /// <summary>
    /// Represents the result of an authentication operation
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; }
        public string Token { get; }
        public User User { get; }
        public string ErrorMessage { get; }

        private AuthenticationResult(bool isSuccess, string token = null, User user = null, string errorMessage = null)
        {
            IsSuccess = isSuccess;
            Token = token;
            User = user;
            ErrorMessage = errorMessage;
        }

        public static AuthenticationResult Success(string token, User user) => new(true, token, user);
        public static AuthenticationResult Failure(string errorMessage) => new(false, errorMessage: errorMessage);
    }

    /// <summary>
    /// Represents an active user session
    /// </summary>
    public class UserSession
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents a user in the system
    /// </summary>
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Interface for user repository
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string userId);
        Task<User?> GetByEmailAsync(string email);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(string userId);
    }
}
