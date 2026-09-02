using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    internal sealed class UserSubMenuRepository : IUserSubMenuRepository
    {
        private readonly RepositoryDbContext _dbContext;
        public UserSubMenuRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public async Task<IEnumerable<UserSubMenu>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.UserSubMenu.OrderBy(a => a.MenuTitle).ToListAsync(cancellationToken);

        public async Task<UserSubMenu> GetByIdAsync(int menuId, CancellationToken cancellationToken = default) =>
            await _dbContext.UserSubMenu.FirstOrDefaultAsync(a => a.UserSubMenuId == menuId, cancellationToken);

        public void Insert(UserSubMenu menu) => _dbContext.UserSubMenu.Add(menu);
        public void Update(UserSubMenu menu) => _dbContext.UserSubMenu.Update(menu);
        public void Delete(UserSubMenu menu) => _dbContext.UserSubMenu.Remove(menu);
    }
}
