using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodexBootstrap.Core.Security
{
    /// <summary>
    /// In-memory implementation of IUserRepository for development purposes
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users = new();
        private readonly Dictionary<string, User> _usersByEmail = new();

        public Task<User?> GetByIdAsync(string userId)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email?.ToLowerInvariant() ?? "", out var user);
            return Task.FromResult(user);
        }

        public Task CreateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _users[user.Id] = user;
            _usersByEmail[user.Email.ToLowerInvariant()] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (_users.ContainsKey(user.Id))
            {
                var oldUser = _users[user.Id];
                _users[user.Id] = user;
                
                // Update email index if email changed
                if (oldUser.Email.ToLowerInvariant() != user.Email.ToLowerInvariant())
                {
                    _usersByEmail.Remove(oldUser.Email.ToLowerInvariant());
                    _usersByEmail[user.Email.ToLowerInvariant()] = user;
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                _users.Remove(userId);
                _usersByEmail.Remove(user.Email.ToLowerInvariant());
            }
            return Task.CompletedTask;
        }
    }
}