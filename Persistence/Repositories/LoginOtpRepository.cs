using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    internal sealed class LoginOtpRepository : ILoginOtpRepository
    {
        private readonly RepositoryDbContext _dbContext;

        public LoginOtpRepository(RepositoryDbContext dbContext) => _dbContext = dbContext;

        public void Insert(LoginOtp otp) => _dbContext.LoginOtps.Add(otp);

        public void Update(LoginOtp otp) => _dbContext.LoginOtps.Update(otp);

        public async Task<LoginOtp> GetActiveByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default) =>
            await _dbContext.LoginOtps
                .Where(a => a.NationalCode == nationalCode && !a.IsUsed && a.ExpiresAt > DateTime.Now)
                .OrderByDescending(a => a.LoginOtpId)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<LoginOtp> GetActiveByLoginTokenHashAsync(string loginTokenHash, CancellationToken cancellationToken = default) =>
            await _dbContext.LoginOtps
                .Where(a => a.LoginTokenHash == loginTokenHash && !a.IsUsed && a.ExpiresAt > DateTime.Now)
                .OrderByDescending(a => a.LoginOtpId)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<int> CountRecentRequestsAsync(string nationalCode, int withinMinutes, CancellationToken cancellationToken = default)
        {
            var since = DateTime.Now.AddMinutes(-withinMinutes);
            return await _dbContext.LoginOtps
                .CountAsync(a => a.NationalCode == nationalCode && a.CreateDate >= since, cancellationToken);
        }

        public async Task InvalidateActiveForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var items = await _dbContext.LoginOtps
                .Where(a => a.UserId == userId && !a.IsUsed && a.ExpiresAt > DateTime.Now)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
                item.IsUsed = true;
        }
    }
}
