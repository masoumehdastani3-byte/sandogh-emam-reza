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
    public sealed class MemberPanelService : IMemberPanelService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;

        public MemberPanelService(IRepositoryManager repositoryManager, IMapper mapper)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
        }

        public async Task<MemberPanelDto> GetPanelAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null)
                return null;

            var maxLoanStr = await _repositoryManager.SettingRepository.GetValueAsync("MaxLoanAmount", "50000000", cancellationToken);
            long.TryParse(maxLoanStr, out var maxLoan);

            var loans = (await _repositoryManager.LoanRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
            foreach (var loan in loans)
            {
                if (loan.RequestDate != default && string.IsNullOrWhiteSpace(loan.RequestDate_Shamsi))
                    loan.RequestDate_Shamsi = PersianDateHelper.ToShamsi(loan.RequestDate);
                if (loan.PaymentDate.HasValue && string.IsNullOrWhiteSpace(loan.PaymentDate_Shamsi))
                    loan.PaymentDate_Shamsi = PersianDateHelper.ToShamsi(loan.PaymentDate.Value);
            }

            var installments = (await _repositoryManager.InstallmentRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
            var payments = (await _repositoryManager.PaymentRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();

            var paid = installments.Where(a => a.InstallmentStatus == (byte)InstallmentStatus.Cleared).ToList();
            var remain = installments.Where(a => a.InstallmentStatus != (byte)InstallmentStatus.Cleared).ToList();

            var waitingLoan = loans.FirstOrDefault(a => a.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue);
            var myPosition = waitingLoan != null
                ? await _repositoryManager.LoanRepository.GetQueuePositionAsync(waitingLoan.LoanId, cancellationToken)
                : 0;

            var loanDtos = _mapper.Map<List<LoanDto>>(loans);
            foreach (var dto in loanDtos.Where(d => d.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue))
                dto.QueuePosition = await _repositoryManager.LoanRepository.GetQueuePositionAsync(dto.LoanId, cancellationToken);

            return new MemberPanelDto
            {
                User = _mapper.Map<UserDto>(user),
                MaxLoanAmount = maxLoan,
                FundBalance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken),
                WaitingQueueCount = await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken),
                MyQueuePosition = myPosition,
                TotalDebt = remain.Sum(a => a.InstallmentAmount),
                PaidInstallmentsAmount = paid.Sum(a => a.InstallmentAmount),
                RemainInstallmentsAmount = remain.Sum(a => a.InstallmentAmount),
                PaidInstallmentsCount = paid.Count,
                RemainInstallmentsCount = remain.Count,
                TotalDonations = await _repositoryManager.PaymentRepository.GetTotalDonationsByUserIdAsync(userId, cancellationToken),
                Loans = loanDtos,
                Installments = _mapper.Map<List<InstallmentDto>>(installments),
                Payments = _mapper.Map<List<PaymentDto>>(payments)
            };
        }
    }

    public sealed class NationalIdVerifyService : INationalIdVerifyService
    {
        public Task<(bool IsValid, string Message)> VerifyAsync(string nationalCode, string imagePath, CancellationToken cancellationToken = default)
        {
            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return Task.FromResult((false, "کد ملی از نظر الگوریتمی معتبر نیست"));

            if (string.IsNullOrWhiteSpace(imagePath))
                return Task.FromResult((false, "فایل کارت ملی بارگذاری نشده است"));

            // هوک اعتبارسنجی هوشمند: در نسخه فعلی با بررسی فرمت کد ملی و وجود فایل تایید می‌شود.
            // می‌توان سرویس OCR/AI خارجی را اینجا متصل کرد.
            var extension = System.IO.Path.GetExtension(imagePath)?.ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (allowed.All(a => a != extension))
                return Task.FromResult((false, "فرمت تصویر کارت ملی مجاز نیست"));

            return Task.FromResult((true, "کارت ملی از نظر اولیه معتبر تشخیص داده شد (آماده اتصال به سرویس هوش مصنوعی)"));
        }
    }

    public sealed class NotificationSchedulerService : INotificationSchedulerService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IBaleService _baleService;
        private readonly ILoanService _loanService;
        private readonly IMapper _mapper;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public NotificationSchedulerService(
            IRepositoryManager repositoryManager,
            IBaleService baleService,
            ILoanService loanService,
            IMapper mapper,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _repositoryManager = repositoryManager;
            _baleService = baleService;
            _loanService = loanService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<SendRemindersResultDto> SendMonthlyRemindersAsync(CancellationToken cancellationToken = default)
        {
            const int reminderDaysBeforeDue = 3;
            var baseUrl = await AppUrlHelper.GetBaseUrlAsync(_repositoryManager, _configuration, cancellationToken);
            var today = System.DateTime.Today;
            var dueInThreeDays = today.AddDays(reminderDaysBeforeDue);
            var logs = new List<NotificationLog>();
            var userNames = new Dictionary<int, string>();

            var unpaid = (await _repositoryManager.InstallmentRepository.GetAllAsync(cancellationToken))
                .Where(i => i.InstallmentStatus != (byte)InstallmentStatus.Cleared && i.Active != false)
                .Where(i => i.InstallmentDate.Date < today || i.InstallmentDate.Date == dueInThreeDays)
                .ToList();

            foreach (var installment in unpaid)
            {
                var user = installment.Loan?.User;
                if (user == null) continue;

                userNames[user.UserId] = user.FullName;
                var isOverdue = installment.InstallmentDate.Date < today;
                var signingKey = _configuration["App:LinkSigningKey"] ?? _configuration["ZarinPal:MerchantId"];
                var payToken = SecureLinkHelper.CreateInstallmentPayToken(
                    installment.InstallmentId,
                    user.UserId,
                    TimeSpan.FromDays(14),
                    signingKey);
                var link = $"{baseUrl}/pay-installment.html?token={Uri.EscapeDataString(payToken)}";
                var log = await _baleService.NotifyInstallmentReminderAsync(user, installment, link, isOverdue, cancellationToken);
                logs.Add(log);
            }

            var donatorRole = await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken);
            var donatorRoleId = donatorRole.FirstOrDefault(r => r.RoleName == "خیر")?.RoleId ?? (int)UserRoleType.Donator;
            var donators = (await _repositoryManager.UserRepository.GetByRoleIdAsync(donatorRoleId, cancellationToken)).ToList();
            foreach (var donator in donators)
            {
                if (donator.MonthlyDonationAmount == null) continue;
                userNames[donator.UserId] = donator.FullName;
                var link = $"{baseUrl}/panel.html?userId={donator.UserId}&pay=donation";
                var log = await _baleService.NotifyDonationLinkAsync(donator, link, cancellationToken);
                logs.Add(log);
            }

            var sent = logs.Count(l => l.NotificationStatus == (byte)NotificationStatus.Sent);
            var failed = logs.Count(l => l.NotificationStatus == (byte)NotificationStatus.Failed);
            var skipped = logs.Count(l => l.NotificationStatus == (byte)NotificationStatus.Failed &&
                                          !string.IsNullOrWhiteSpace(l.ErrorMessage) &&
                                          l.ErrorMessage.Contains("بله متصل"));

            var dtos = _mapper.Map<List<NotificationLogDto>>(logs);
            foreach (var dto in dtos)
            {
                if (dto.UserId.HasValue && userNames.TryGetValue(dto.UserId.Value, out var name))
                    dto.UserFullName = name;
            }

            return new SendRemindersResultDto
            {
                SentCount = sent,
                FailedCount = failed,
                SkippedCount = skipped,
                Logs = dtos
            };
        }

        public async Task ProcessLoanQueueAsync(CancellationToken cancellationToken = default)
        {
            var queue = await _loanService.GetQueueAsync(cancellationToken);
            var balance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken);
            foreach (var loan in queue)
            {
                if (balance < loan.LoanAmount)
                    break;

                var result = await _loanService.DisburseLoanAsync(new DisburseLoanDto
                {
                    LoanId = loan.LoanId,
                    ManagerUserId = 0,
                    Description = "پرداخت خودکار بر اساس اولویت صف و موجودی صندوق"
                }, cancellationToken);

                if (result.Success)
                    balance -= loan.LoanAmount;
                else
                    break;
            }
        }
    }
}
