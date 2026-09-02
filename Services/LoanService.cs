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
    public sealed class LoanService : ILoanService
    {
        private const int FirstInstallmentDelayDays = 30;

        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly IBaleService _baleService;

        public LoanService(IRepositoryManager repositoryManager, IMapper mapper, IBaleService baleService)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _baleService = baleService;
        }

        public async Task<ApiResultDto> RequestLoanAsync(LoanRequestDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            if (user.RoleId == (int)UserRoleType.Donator)
                return ApiResultDto.Fail("خیرین امکان درخواست وام ندارند");

            var maxLoanStr = await _repositoryManager.SettingRepository.GetValueAsync("MaxLoanAmount", "50000000", cancellationToken);
            long.TryParse(maxLoanStr, out var maxLoan);
            if (maxLoan <= 0) maxLoan = 50000000;

            if (dto.LoanAmount <= 0 || dto.LoanAmount > maxLoan)
                return ApiResultDto.Fail($"مبلغ وام باید بین ۱ و {maxLoan:N0} ریال باشد");

            var active = await _repositoryManager.LoanRepository.GetActiveLoanByUserIdAsync(dto.UserId, cancellationToken);
            if (active != null)
                return ApiResultDto.Fail("شما یک درخواست یا وام فعال دارید");

            var installmentCountStr = await _repositoryManager.SettingRepository.GetValueAsync("DefaultInstallmentCount", "10", cancellationToken);
            int.TryParse(installmentCountStr, out var installmentCount);
            if (installmentCount <= 0) installmentCount = 10;

            var now = DateTime.Now;
            var queueCount = await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken);

            var loan = new Loan
            {
                UserId = dto.UserId,
                LoanAmount = dto.LoanAmount,
                RemainAmount = dto.LoanAmount,
                InstallmentCount = installmentCount,
                InstallmentAmount = dto.LoanAmount / installmentCount,
                RequestDate = now,
                RequestDate_Shamsi = PersianDateHelper.ToShamsi(now),
                QueuePriority = queueCount + 1,
                LoanRequestStatus = (byte)LoanRequestStatus.WaitingInQueue,
                LoanStatus = (byte)LoanStatus.Normal,
                Description = dto.Description,
                CreateDate = now,
                Active = true
            };

            _repositoryManager.LoanRepository.Insert(loan);

            user.UserStatus = (byte)UserStatus.LoanApplicant;
            _repositoryManager.UserRepository.Update(user);

            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var position = await _repositoryManager.LoanRepository.GetQueuePositionAsync(loan.LoanId, cancellationToken);
            var loanDto = _mapper.Map<LoanDto>(loan);
            loanDto.QueuePosition = position;

            return ApiResultDto.Ok("درخواست وام در صف قرار گرفت", loanDto);
        }

        public async Task<ApiResultDto> DisburseLoanAsync(DisburseLoanDto dto, CancellationToken cancellationToken = default)
        {
            var loan = await _repositoryManager.LoanRepository.GetByLoanIdAsync(dto.LoanId, cancellationToken);
            if (loan == null)
                return ApiResultDto.Fail("وام یافت نشد");

            if (loan.LoanRequestStatus != (byte)LoanRequestStatus.WaitingInQueue)
                return ApiResultDto.Fail("این وام در صف انتظار نیست");

            var balance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken);
            if (balance < loan.LoanAmount)
                return ApiResultDto.Fail($"موجودی صندوق کافی نیست. موجودی فعلی: {balance:N0} ریال");

            var now = DateTime.Now;
            var installmentCount = loan.InstallmentCount ?? 10;
            var installmentAmount = loan.LoanAmount / installmentCount;

            var payment = new Payment
            {
                UserId = loan.UserId,
                Amount = loan.LoanAmount,
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentStatus = (byte)PaymentStatus.Pay,
                PaymentType = (byte)PaymentType.LoanDisbursement,
                PaymentMethod = (byte)PaymentMethod.Manual,
                GatewayStatus = (byte)GatewayStatus.Success,
                Confirm = true,
                ConfirmUserId = dto.ManagerUserId,
                ConfirmUserDescription = dto.Description ?? "پرداخت وام از موجودی صندوق",
                CreateDate = now,
                Active = true
            };
            _repositoryManager.PaymentRepository.Insert(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            loan.PaymentId = payment.PaymentId;
            loan.PaymentDate = now;
            loan.PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now);
            loan.LoanRequestStatus = (byte)LoanRequestStatus.Disbursed;
            loan.LoanStatus = (byte)LoanStatus.Normal;
            loan.RemainAmount = loan.LoanAmount;
            loan.InstallmentAmount = installmentAmount;
            loan.InstallmentCount = installmentCount;
            loan.UpdateDate = now;
            loan.ModifyUserId = dto.ManagerUserId;

            var installments = new List<Installment>();
            var firstDueDate = now.AddDays(FirstInstallmentDelayDays);
            for (var i = 1; i <= installmentCount; i++)
            {
                var due = firstDueDate.AddMonths(i - 1);
                installments.Add(new Installment
                {
                    LoanId = loan.LoanId,
                    InstallmentDate = due,
                    InstallmentDate_shansi = PersianDateHelper.ToShamsi(due),
                    InstallmentAmount = installmentAmount,
                    InstallmentStatus = (byte)InstallmentStatus.Normal,
                    CreateDate = now,
                    Active = true
                });
            }
            _repositoryManager.InstallmentRepository.InsertAddRangeAsync(installments);

            var newBalance = balance - loan.LoanAmount;
            _repositoryManager.FundTransactionRepository.Insert(new FundTransaction
            {
                TransactionType = (byte)FundTransactionType.LoanOut,
                Amount = -loan.LoanAmount,
                BalanceAfter = newBalance,
                UserId = loan.UserId,
                PaymentId = payment.PaymentId,
                LoanId = loan.LoanId,
                TransactionDate = now,
                TransactionDate_Shamsi = PersianDateHelper.ToShamsi(now),
                Description = $"پرداخت وام شماره {loan.LoanId}",
                CreateDate = now,
                Active = true
            });

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(loan.UserId, cancellationToken);
            if (user != null)
            {
                user.UserStatus = (byte)UserStatus.Borrower;
                _repositoryManager.UserRepository.Update(user);
            }

            _repositoryManager.LoanRepository.Update(loan);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            if (user != null && !string.IsNullOrEmpty(user.BaleChatId))
            {
                await _baleService.SendMessageAsync(user.BaleChatId,
                    $"وام شما به مبلغ {loan.LoanAmount:N0} ریال پرداخت شد. اولین قسط ۳۰ روز بعد و سپس {installmentCount} قسط ماهانه.",
                    cancellationToken);
            }

            return ApiResultDto.Ok("وام با موفقیت پرداخت شد", _mapper.Map<LoanDto>(loan));
        }

        public async Task<ApiResultDto> RejectLoanAsync(int loanId, string reason, int managerUserId, CancellationToken cancellationToken = default)
        {
            var loan = await _repositoryManager.LoanRepository.GetByLoanIdAsync(loanId, cancellationToken);
            if (loan == null)
                return ApiResultDto.Fail("وام یافت نشد");

            loan.LoanRequestStatus = (byte)LoanRequestStatus.Rejected;
            loan.RejectReason = reason;
            loan.UpdateDate = DateTime.Now;
            loan.ModifyUserId = managerUserId;
            _repositoryManager.LoanRepository.Update(loan);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("درخواست وام رد شد");
        }

        public async Task<List<LoanDto>> GetQueueAsync(CancellationToken cancellationToken = default)
        {
            var queue = (await _repositoryManager.LoanRepository.GetWaitingQueueAsync(cancellationToken)).ToList();
            return await MapLoansToDtosAsync(queue, setQueuePositions: true, cancellationToken);
        }

        public async Task<List<LoanDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var loans = (await _repositoryManager.LoanRepository.GetAllAsync(cancellationToken))
                .OrderByDescending(a => a.RequestDate)
                .ToList();
            return await MapLoansToDtosAsync(loans, setQueuePositions: false, cancellationToken);
        }

        public async Task<List<LoanDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var loans = (await _repositoryManager.LoanRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
            return await MapLoansToDtosAsync(loans, setQueuePositions: false, cancellationToken);
        }

        public async Task<LoanDto> GetByIdAsync(int loanId, CancellationToken cancellationToken = default)
        {
            var loan = await _repositoryManager.LoanRepository.GetByLoanIdAsync(loanId, cancellationToken);
            if (loan == null) return null;
            NormalizeLoanDates(loan);
            var dto = _mapper.Map<LoanDto>(loan);
            if (dto.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue)
                dto.QueuePosition = await _repositoryManager.LoanRepository.GetQueuePositionAsync(dto.LoanId, cancellationToken);
            return dto;
        }

        private async Task<List<LoanDto>> MapLoansToDtosAsync(List<Loan> loans, bool setQueuePositions, CancellationToken cancellationToken)
        {
            foreach (var loan in loans)
                NormalizeLoanDates(loan);

            var dtos = _mapper.Map<List<LoanDto>>(loans);
            if (setQueuePositions)
            {
                for (var i = 0; i < dtos.Count; i++)
                    dtos[i].QueuePosition = i + 1;
            }
            else
            {
                foreach (var dto in dtos.Where(d => d.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue))
                    dto.QueuePosition = await _repositoryManager.LoanRepository.GetQueuePositionAsync(dto.LoanId, cancellationToken);
            }
            return dtos;
        }

        private static void NormalizeLoanDates(Loan loan)
        {
            if (loan == null) return;
            if (loan.RequestDate != default && string.IsNullOrWhiteSpace(loan.RequestDate_Shamsi))
                loan.RequestDate_Shamsi = PersianDateHelper.ToShamsi(loan.RequestDate);
            if (loan.PaymentDate.HasValue && string.IsNullOrWhiteSpace(loan.PaymentDate_Shamsi))
                loan.PaymentDate_Shamsi = PersianDateHelper.ToShamsi(loan.PaymentDate.Value);
        }

        public async Task<int> GetWaitingCountAsync(CancellationToken cancellationToken = default) =>
            await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken);

        public async Task<int> GetDisbursedCountAsync(CancellationToken cancellationToken = default) =>
            await _repositoryManager.LoanRepository.GetDisbursedCountAsync(cancellationToken);
    }
}
