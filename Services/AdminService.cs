using AutoMapper;
using Contracts.Enums;
using Contracts.Models;
using Domain.Entities;
using Domain.Repositories;
using Services.Abstractions;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;

        public AdminService(IRepositoryManager repositoryManager, IMapper mapper)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
        }

        public async Task<bool> IsManagerAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) return false;
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null) return false;
            if (user.UserStatus == (byte)UserStatus.Manager) return true;
            if (user.RoleId == (int)UserRoleType.Manager) return true;
            var roleName = user.UserRole?.RoleName ?? string.Empty;
            return roleName == "مدیر صندوق" || roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var payments = (await _repositoryManager.PaymentRepository.GetAllAsync(cancellationToken)).ToList();
            var todayPayments = payments.Where(p => p.PaymentDate.Date == today).ToList();

            return new AdminDashboardDto
            {
                FundBalance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken),
                WaitingQueueCount = await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken),
                TodayPaymentsCount = todayPayments.Count,
                TodayPaymentsAmount = todayPayments.Sum(p => p.Amount)
            };
        }

        public async Task<ApiResultDto> DepositFundAsync(FundDepositDto dto, CancellationToken cancellationToken = default)
        {
            if (!await IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            if (dto.Amount <= 0)
                return ApiResultDto.Fail("مبلغ شارژ باید بزرگ‌تر از صفر باشد");

            var balance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken);
            var newBalance = balance + dto.Amount;
            var now = DateTime.Now;

            _repositoryManager.FundTransactionRepository.Insert(new FundTransaction
            {
                TransactionType = (byte)FundTransactionType.ManualAdjust,
                Amount = dto.Amount,
                BalanceAfter = newBalance,
                UserId = dto.ManagerUserId,
                TransactionDate = now,
                TransactionDate_Shamsi = PersianDateHelper.ToShamsi(now),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "شارژ دستی صندوق" : dto.Description.Trim(),
                CreateDate = now,
                Active = true
            });

            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("صندوق با موفقیت شارژ شد", new { fundBalance = newBalance });
        }

        public async Task<List<FundTransactionDto>> GetFundTransactionsAsync(CancellationToken cancellationToken = default)
        {
            var list = await _repositoryManager.FundTransactionRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<List<FundTransactionDto>>(list);
        }
    }
}
