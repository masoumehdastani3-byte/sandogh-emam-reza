using Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface ILoginOtpRepository
    {
        void Insert(LoginOtp otp);
        void Update(LoginOtp otp);
        Task<LoginOtp> GetActiveByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);
        Task<int> CountRecentRequestsAsync(string nationalCode, int withinMinutes, CancellationToken cancellationToken = default);
        Task InvalidateActiveForUserAsync(int userId, CancellationToken cancellationToken = default);
    }
}
