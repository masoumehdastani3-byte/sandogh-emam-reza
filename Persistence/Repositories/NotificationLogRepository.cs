using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    internal sealed class NotificationLogRepository : INotificationLogRepository
    {
        private readonly RepositoryDbContext _dbContext;
        public NotificationLogRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Insert(NotificationLog notification) => _dbContext.NotificationLogs.Add(notification);

        public async Task<IEnumerable<NotificationLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.NotificationLogs.OrderByDescending(a => a.CreateDate).ToListAsync(cancellationToken);

        public async Task<IEnumerable<NotificationLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.NotificationLogs.Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreateDate)
                .ToListAsync(cancellationToken);
    }

    internal sealed class FundTransactionRepository : IFundTransactionRepository
    {
        private readonly RepositoryDbContext _dbContext;
        public FundTransactionRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Insert(FundTransaction transaction) => _dbContext.FundTransactions.Add(transaction);

        public async Task<FundTransaction> GetLastAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.FundTransactions.OrderByDescending(a => a.FundTransactionId)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<long> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
        {
            var last = await GetLastAsync(cancellationToken);
            return last?.BalanceAfter ?? 0;
        }

        public async Task<IEnumerable<FundTransaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.FundTransactions.OrderByDescending(a => a.TransactionDate).ToListAsync(cancellationToken);
    }
}
