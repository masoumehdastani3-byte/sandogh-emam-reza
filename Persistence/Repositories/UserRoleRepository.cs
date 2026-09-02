using Domain.Entities;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    internal sealed class UserRoleRepository : IUserRoleRepository
    {
        private readonly RepositoryDbContext _dbContext;
        public UserRoleRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public async Task<IEnumerable<UserRole>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.UserRole.ToListAsync(cancellationToken);
        }

        public async Task<UserRole> GetByUserRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.UserRole.FirstOrDefaultAsync(a => a.RoleId == roleId, cancellationToken);
        }

        public void Insert(UserRole role) => _dbContext.UserRole.Add(role);
        public void Update(UserRole role) => _dbContext.UserRole.Update(role);
        public void Delete(UserRole role) => _dbContext.UserRole.Remove(role);
    }
}
