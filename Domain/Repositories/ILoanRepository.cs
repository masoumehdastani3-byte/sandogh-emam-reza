using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface ILoanRepository
    {
        void Insert(Loan loan);
        void Update(Loan loan);
        void Delete(Loan loan);
        Task<Loan> GetByLoanIdAsync(int loanId, CancellationToken cancellationToken = default);
        Task<Loan> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Loan>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Loan>> GetBySponsorIdAsync(int sponsorId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Loan>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Loan>> GetWaitingQueueAsync(CancellationToken cancellationToken = default);
        Task<int> GetWaitingQueueCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetDisbursedCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetQueuePositionAsync(int loanId, CancellationToken cancellationToken = default);
        Task<Loan> GetActiveLoanByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
