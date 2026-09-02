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
    internal sealed class InstallmentRepository : IInstallmentRepository
    {
        private readonly RepositoryDbContext _dbContext;

        public InstallmentRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Delete(Installment installment) => _dbContext.Installments.Remove(installment);
        public void DeleteAddRangeAsync(List<Installment> installments) => _dbContext.Installments.RemoveRange(installments);
        public void Insert(Installment installment) => _dbContext.Installments.Add(installment);
        public void InsertAddRangeAsync(List<Installment> installments) => _dbContext.Installments.AddRange(installments);
        public void Update(Installment installment) => _dbContext.Installments.Update(installment);

        public async Task<IEnumerable<Installment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.Include(a => a.Loan).ThenInclude(l => l.User).ToListAsync(cancellationToken);

        public async Task<Installment> GetByInstallmentIdAsync(int installmentId, CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.Include(a => a.Loan)
                .FirstOrDefaultAsync(a => a.InstallmentId == installmentId, cancellationToken);

        public async Task<IEnumerable<Installment>> GetByLoanIdAsync(int loanId, CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.Where(a => a.LoanId == loanId)
                .OrderBy(a => a.InstallmentDate)
                .ToListAsync(cancellationToken);

        public async Task<Installment> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.FirstOrDefaultAsync(a => a.PaymentId == paymentId, cancellationToken);

        public async Task<IEnumerable<Installment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.Include(a => a.Loan)
                .Where(a => a.Loan.UserId == userId)
                .OrderBy(a => a.InstallmentDate)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Installment>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Installments.Include(a => a.Loan).Where(a =>
                (fromDate == null || a.InstallmentDate >= fromDate) &&
                (toDate == null || a.InstallmentDate <= toDate) &&
                (userId == null || a.Loan.UserId == userId)
            ).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Installment>> GetUpcomingUnpaidAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default) =>
            await _dbContext.Installments.Include(a => a.Loan).ThenInclude(l => l.User)
                .Where(a => a.InstallmentStatus != (byte)InstallmentStatus.Cleared &&
                            a.InstallmentDate >= fromDate &&
                            a.InstallmentDate <= toDate)
                .ToListAsync(cancellationToken);
    }
}
