using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly RepositoryDbContext _dbContext;

        public UserRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Delete(User user) => _dbContext.Users.Remove(user);

        public void Insert(User user) => _dbContext.Users.Add(user);

        public void Update(User user) => _dbContext.Users.Update(user);

        public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole).ToListAsync(cancellationToken);

        public async Task<User> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole)
                .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        public async Task<User> GetByBaleChatIdAsync(string baleChatId, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole)
                .FirstOrDefaultAsync(a => a.BaleChatId == baleChatId, cancellationToken);

        public async Task<User> GetByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole)
                .FirstOrDefaultAsync(a => a.NationalCode == nationalCode, cancellationToken);

        public async Task<User> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.FirstOrDefaultAsync(a => a.Mobile == mobile, cancellationToken);

        public async Task<User> LoginAsync(string nationalCode, string password, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole)
                .FirstOrDefaultAsync(a => a.NationalCode == nationalCode && a.Password == password && a.Active == true, cancellationToken);

        public async Task<List<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(a => a.UserRole)
                .Where(a => a.RoleId == roleId && a.Active == true)
                .ToListAsync(cancellationToken);

        public async Task<List<User>> GetManagersAsync(CancellationToken cancellationToken = default)
        {
            var managerRole = await _dbContext.UserRole.FirstOrDefaultAsync(a => a.RoleName == "مدیر صندوق", cancellationToken);
            if (managerRole == null)
                return await GetByRoleIdAsync(1, cancellationToken);
            return await GetByRoleIdAsync(managerRole.RoleId, cancellationToken);
        }

        public async Task<List<User>> GetFilterAsync(string userName, int? userId, string fullName, string cardNumber, string mobile, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(a => a.UserRole).Where(a =>
                (string.IsNullOrEmpty(userName) || a.UserName.Contains(userName)) &&
                (string.IsNullOrEmpty(fullName) || a.FullName.Contains(fullName)) &&
                (string.IsNullOrEmpty(cardNumber) || a.CardNumber.Contains(cardNumber)) &&
                (string.IsNullOrEmpty(mobile) || a.Mobile.Contains(mobile)) &&
                (userId == null || a.UserId == userId)
            ).ToListAsync(cancellationToken);
        }
    }
}
