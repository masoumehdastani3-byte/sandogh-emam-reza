using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IUserRoleRepository
    {
        Task<UserRole> GetByUserRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserRole>> GetAllAsync(CancellationToken cancellationToken = default);
        void Insert(UserRole role);
        void Update(UserRole role);
        void Delete(UserRole role);
    }
}
