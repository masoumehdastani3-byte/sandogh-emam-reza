using Contracts.Enums;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    internal sealed class LoanRepository : ILoanRepository
    {
        private readonly RepositoryDbContext _dbContext;

        public LoanRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Delete(Loan loan) => _dbContext.Loans.Remove(loan);
        public void Insert(Loan loan) => _dbContext.Loans.Add(loan);
        public void Update(Loan loan) => _dbContext.Loans.Update(loan);

        public async Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Include(a => a.User).Include(a => a.Installments).ToListAsync(cancellationToken);

        public async Task<Loan> GetByLoanIdAsync(int loanId, CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Include(a => a.User).Include(a => a.Installments)
                .FirstOrDefaultAsync(a => a.LoanId == loanId, cancellationToken);

        public async Task<Loan> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.FirstOrDefaultAsync(a => a.PaymentId == paymentId, cancellationToken);

        public async Task<IEnumerable<Loan>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Include(a => a.Installments)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.RequestDate)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Loan>> GetBySponsorIdAsync(int sponsorId, CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Where(a => a.UserSponsorId == sponsorId).ToListAsync(cancellationToken);

        public async Task<IEnumerable<Loan>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Loans.Where(a =>
                (fromDate == null || a.RequestDate >= fromDate) &&
                (toDate == null || a.RequestDate <= toDate) &&
                (userId == null || a.UserId == userId)
            ).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Loan>> GetWaitingQueueAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Include(a => a.User)
                .Where(a => a.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue && a.Active == true)
                .OrderBy(a => a.QueuePriority)
                .ThenBy(a => a.RequestDate)
                .ToListAsync(cancellationToken);

        public async Task<int> GetWaitingQueueCountAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.CountAsync(a =>
                a.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue && a.Active == true, cancellationToken);

        public async Task<int> GetDisbursedCountAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.CountAsync(a =>
                a.LoanRequestStatus == (byte)LoanRequestStatus.Disbursed, cancellationToken);

        public async Task<int> GetQueuePositionAsync(int loanId, CancellationToken cancellationToken = default)
        {
            var loan = await GetByLoanIdAsync(loanId, cancellationToken);
            if (loan == null || loan.LoanRequestStatus != (byte)LoanRequestStatus.WaitingInQueue)
                return 0;

            return await _dbContext.Loans.CountAsync(a =>
                a.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue &&
                a.Active == true &&
                (a.QueuePriority < loan.QueuePriority ||
                 (a.QueuePriority == loan.QueuePriority && a.RequestDate <= loan.RequestDate)),
                cancellationToken);
        }

        public async Task<Loan> GetActiveLoanByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Loans.Include(a => a.Installments)
                .Where(a => a.UserId == userId &&
                            (a.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue ||
                             (a.LoanRequestStatus == (byte)LoanRequestStatus.Disbursed &&
                              a.LoanStatus != (byte)LoanStatus.Cleared)))
                .OrderByDescending(a => a.RequestDate)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
