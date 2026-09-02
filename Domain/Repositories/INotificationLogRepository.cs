using Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface INotificationLogRepository
    {
        void Insert(NotificationLog notification);
        Task<IEnumerable<NotificationLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<NotificationLog>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    public interface IFundTransactionRepository
    {
        void Insert(FundTransaction transaction);
        Task<FundTransaction> GetLastAsync(CancellationToken cancellationToken = default);
        Task<long> GetCurrentBalanceAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<FundTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
