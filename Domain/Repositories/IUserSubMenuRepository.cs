using Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IUserSubMenuRepository
    {
        Task<IEnumerable<UserSubMenu>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<UserSubMenu> GetByIdAsync(int menuId, CancellationToken cancellationToken = default);
        void Insert(UserSubMenu menu);
        void Update(UserSubMenu menu);
        void Delete(UserSubMenu menu);
    }
}
