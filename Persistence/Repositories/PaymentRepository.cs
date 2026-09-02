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
    internal sealed class PaymentRepository : IPaymentRepository
    {
        private readonly RepositoryDbContext _dbContext;

        public PaymentRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Delete(Payment payment) => _dbContext.Payments.Remove(payment);
        public void Insert(Payment payment) => _dbContext.Payments.Add(payment);
        public void Update(Payment payment) => _dbContext.Payments.Update(payment);

        public async Task<IEnumerable<Payment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.Include(a => a.User).OrderByDescending(a => a.PaymentDate).ToListAsync(cancellationToken);

        public async Task<Payment> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.FirstOrDefaultAsync(a => a.PaymentId == paymentId, cancellationToken);

        public async Task<Payment> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.FirstOrDefaultAsync(a => a.ZarinPalAuthority == authority, cancellationToken);

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.Where(a => a.UserId == userId)
                .OrderByDescending(a => a.PaymentDate)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<Payment>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Payments.Where(a =>
                (fromDate == null || a.PaymentDate >= fromDate) &&
                (toDate == null || a.PaymentDate <= toDate) &&
                (userId == null || a.UserId == userId)
            ).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Payment>> GetDonationsByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.Where(a =>
                a.UserId == userId &&
                a.PaymentType == (byte)PaymentType.Donation &&
                a.Confirm &&
                a.GatewayStatus == (byte)GatewayStatus.Success
            ).ToListAsync(cancellationToken);

        public async Task<long> GetTotalDonationsByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.Where(a =>
                a.UserId == userId &&
                a.PaymentType == (byte)PaymentType.Donation &&
                a.Confirm &&
                a.GatewayStatus == (byte)GatewayStatus.Success
            ).SumAsync(a => a.Amount, cancellationToken);

        public async Task<long> GetTotalConfirmedDonationsAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Payments.Where(a =>
                a.PaymentType == (byte)PaymentType.Donation &&
                a.Confirm &&
                a.GatewayStatus == (byte)GatewayStatus.Success
            ).SumAsync(a => a.Amount, cancellationToken);
    }
}
