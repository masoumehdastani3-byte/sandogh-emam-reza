using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IPaymentRepository
    {
        void Insert(Payment payment);
        void Update(Payment payment);
        void Delete(Payment payment);
        Task<Payment> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<Payment> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Payment>> GetDonationsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<long> GetTotalDonationsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<long> GetTotalConfirmedDonationsAsync(CancellationToken cancellationToken = default);
    }
}
