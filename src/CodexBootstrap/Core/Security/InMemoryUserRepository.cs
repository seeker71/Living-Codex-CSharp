using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core.Security
{
    /// <summary>
    /// In-memory implementation of user repository for development and testing
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users;
        private readonly Dictionary<string, User> _usersByEmail;
        private readonly ICodexLogger _logger;

        public InMemoryUserRepository(ICodexLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _users = new Dictionary<string, User>();
            _usersByEmail = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<User?> GetByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult<User?>(null);
            }

            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Task.FromResult<User?>(null);
            }

            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task CreateAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(user));
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                throw new ArgumentException("User email cannot be null or empty", nameof(user));
            }

            // Check if user already exists
            if (_users.ContainsKey(user.Id))
            {
                throw new InvalidOperationException($"User with ID {user.Id} already exists");
            }

            if (_usersByEmail.ContainsKey(user.Email))
            {
                throw new InvalidOperationException($"User with email {user.Email} already exists");
            }

            // Set timestamps
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Add to both dictionaries
            _users[user.Id] = user;
            _usersByEmail[user.Email] = user;

            _logger.Info($"User created: {user.Email} (ID: {user.Id})");
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(user));
            }

            if (!_users.ContainsKey(user.Id))
            {
                throw new InvalidOperationException($"User with ID {user.Id} not found");
            }

            // Update timestamp
            user.UpdatedAt = DateTime.UtcNow;

            // Update in both dictionaries
            _users[user.Id] = user;
            if (!string.IsNullOrEmpty(user.Email))
            {
                _usersByEmail[user.Email] = user;
            }

            _logger.Info($"User updated: {user.Email} (ID: {user.Id})");
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (!_users.TryGetValue(userId, out var user))
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Remove from both dictionaries
            _users.Remove(userId);
            if (!string.IsNullOrEmpty(user.Email))
            {
                _usersByEmail.Remove(user.Email);
            }

            _logger.Info($"User deleted: {user.Email} (ID: {user.Id})");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets all users (for administrative purposes)
        /// </summary>
        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return Task.FromResult(_users.Values.AsEnumerable());
        }

        /// <summary>
        /// Gets the total number of users
        /// </summary>
        public Task<int> GetUserCountAsync()
        {
            return Task.FromResult(_users.Count);
        }

        /// <summary>
        /// Searches users by display name
        /// </summary>
        public Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return Task.FromResult(Enumerable.Empty<User>());
            }

            var results = _users.Values
                .Where(u => u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .AsEnumerable();

            return Task.FromResult(results);
        }

        /// <summary>
        /// Gets users created within a date range
        /// </summary>
        public Task<IEnumerable<User>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var results = _users.Values
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .AsEnumerable();

            return Task.FromResult(results);
        }

        /// <summary>
        /// Gets active users only
        /// </summary>
        public Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            var results = _users.Values
                .Where(u => u.IsActive)
                .AsEnumerable();

            return Task.FromResult(results);
        }

        /// <summary>
        /// Clears all users (for testing purposes)
        /// </summary>
        public Task ClearAllAsync()
        {
            _users.Clear();
            _usersByEmail.Clear();
            _logger.Info("All users cleared from repository");
            return Task.CompletedTask;
        }
    }
}
