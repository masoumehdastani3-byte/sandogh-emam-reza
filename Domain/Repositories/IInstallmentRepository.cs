using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IInstallmentRepository
    {
        void InsertAddRangeAsync(List<Installment> installments);
        void Insert(Installment installment);
        void Update(Installment installment);
        void Delete(Installment installment);
        void DeleteAddRangeAsync(List<Installment> installments);
        Task<Installment> GetByInstallmentIdAsync(int installmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Installment>> GetByLoanIdAsync(int loanId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Installment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<Installment> GetByPaymentIdAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Installment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Installment>> GetByDateAsync(DateTime? fromDate, DateTime? toDate, int? userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Installment>> GetUpcomingUnpaidAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    }
}
