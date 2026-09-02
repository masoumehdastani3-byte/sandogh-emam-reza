using Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        void Insert(User user);
        void Update(User user);
        void Delete(User user);
        Task<User> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<User> GetByBaleChatIdAsync(string baleChatId, CancellationToken cancellationToken = default);
        Task<User> GetByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);
        Task<User> GetByMobileAsync(string mobile, CancellationToken cancellationToken = default);
        Task<User> LoginAsync(string nationalCode, string password, CancellationToken cancellationToken = default);
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
        Task<List<User>> GetManagersAsync(CancellationToken cancellationToken = default);
        Task<List<User>> GetFilterAsync(string userName, int? userId, string fullName, string cardNumber, string mobile, CancellationToken cancellationToken = default);
    }
}
