using AutoMapper;
using Contracts.Enums;
using Contracts.Models;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Services.Abstractions;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly IZarinPalService _zarinPalService;
        private readonly IBaleService _baleService;
        private readonly IConfiguration _configuration;

        public PaymentService(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IZarinPalService zarinPalService,
            IBaleService baleService,
            IConfiguration configuration)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _zarinPalService = zarinPalService;
            _baleService = baleService;
            _configuration = configuration;
        }

        public async Task<ApiResultDto> StartZarinPalPaymentAsync(ZarinPalRequestDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            if (dto.Amount <= 0)
                return ApiResultDto.Fail("مبلغ نامعتبر است");

            if (dto.PaymentType == (byte)PaymentType.Installment && dto.InstallmentId.HasValue)
            {
                var installment = await _repositoryManager.InstallmentRepository.GetByInstallmentIdAsync(dto.InstallmentId.Value, cancellationToken);
                if (installment == null)
                    return ApiResultDto.Fail("قسط یافت نشد");
                if (installment.InstallmentStatus == (byte)InstallmentStatus.Cleared)
                    return ApiResultDto.Fail("این قسط قبلاً پرداخت شده است");
                if (installment.Loan.UserId != dto.UserId)
                    return ApiResultDto.Fail("قسط متعلق به این کاربر نیست");
            }

            var callback = string.IsNullOrWhiteSpace(dto.CallbackUrl)
                ? _configuration["ZarinPal:CallbackUrl"]
                : dto.CallbackUrl;

            var now = DateTime.Now;
            var payment = new Payment
            {
                UserId = dto.UserId,
                InstallmentId = dto.InstallmentId,
                Amount = dto.Amount,
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentStatus = (byte)PaymentStatus.Receipt,
                PaymentType = dto.PaymentType,
                PaymentMethod = (byte)PaymentMethod.ZarinPal,
                GatewayStatus = (byte)GatewayStatus.Pending,
                Confirm = false,
                Description = dto.Description,
                CreateDate = now,
                Active = true
            };

            _repositoryManager.PaymentRepository.Insert(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var request = await _zarinPalService.RequestPaymentAsync(
                dto.Amount,
                dto.Description ?? $"پرداخت صندوق قرض‌الحسنه - کاربر {user.UserId}",
                callback,
                user.Mobile,
                cancellationToken);

            if (!request.Success)
            {
                payment.GatewayStatus = (byte)GatewayStatus.Failed;
                payment.Description = request.Message;
                _repositoryManager.PaymentRepository.Update(payment);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResultDto.Fail(request.Message);
            }

            payment.ZarinPalAuthority = request.Authority;
            _repositoryManager.PaymentRepository.Update(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResultDto.Ok("هدایت به درگاه", new
            {
                payment.PaymentId,
                Authority = request.Authority,
                GatewayUrl = request.GatewayUrl
            });
        }

        public async Task<ApiResultDto> VerifyZarinPalPaymentAsync(string authority, string status, CancellationToken cancellationToken = default)
        {
            var payment = await _repositoryManager.PaymentRepository.GetByAuthorityAsync(authority, cancellationToken);
            if (payment == null)
                return ApiResultDto.Fail("تراکنش یافت نشد");

            if (payment.GatewayStatus == (byte)GatewayStatus.Success)
                return ApiResultDto.Ok("این تراکنش قبلاً تایید شده است", _mapper.Map<PaymentDto>(payment));

            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                payment.GatewayStatus = (byte)GatewayStatus.Failed;
                _repositoryManager.PaymentRepository.Update(payment);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResultDto.Fail("پرداخت توسط کاربر لغو شد");
            }

            var verify = await _zarinPalService.VerifyPaymentAsync(authority, payment.Amount, cancellationToken);
            if (!verify.Success)
            {
                payment.GatewayStatus = (byte)GatewayStatus.Failed;
                payment.Description = verify.Message;
                _repositoryManager.PaymentRepository.Update(payment);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResultDto.Fail(verify.Message);
            }

            payment.GatewayStatus = (byte)GatewayStatus.Success;
            payment.Confirm = true;
            payment.ZarinPalRefId = verify.RefId;
            payment.RefrenceNumber = verify.RefId;
            payment.UpdateDate = DateTime.Now;
            _repositoryManager.PaymentRepository.Update(payment);

            await ApplySuccessfulPaymentAsync(payment, cancellationToken);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var payer = payment.UserId.HasValue
                ? await _repositoryManager.UserRepository.GetByUserIdAsync(payment.UserId.Value, cancellationToken)
                : null;
            await _baleService.NotifyManagersPaymentAsync(payment, payer, cancellationToken);

            return ApiResultDto.Ok("پرداخت با موفقیت تایید شد", _mapper.Map<PaymentDto>(payment));
        }

        public async Task<ApiResultDto> SubmitManualPaymentAsync(ManualPaymentDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            if (dto.Amount <= 0)
                return ApiResultDto.Fail("مبلغ نامعتبر است");

            var now = DateTime.Now;
            var payment = new Payment
            {
                UserId = dto.UserId,
                InstallmentId = dto.InstallmentId,
                Amount = dto.Amount,
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentStatus = (byte)PaymentStatus.Receipt,
                PaymentType = dto.PaymentType == 0 ? (byte)PaymentType.Manual : dto.PaymentType,
                PaymentMethod = (byte)PaymentMethod.Manual,
                GatewayStatus = (byte)GatewayStatus.Success,
                Confirm = true,
                ConfirmUserId = dto.ConfirmUserId,
                ConfirmUserDescription = dto.Description,
                RefrenceNumber = dto.RefrenceNumber,
                CardNumber = dto.CardNumber,
                FileUload = dto.FileUpload,
                CreateDate = now,
                Active = true
            };

            _repositoryManager.PaymentRepository.Insert(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            await ApplySuccessfulPaymentAsync(payment, cancellationToken);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            await _baleService.NotifyManagersPaymentAsync(payment, user, cancellationToken);

            return ApiResultDto.Ok("پرداخت دستی ثبت شد", _mapper.Map<PaymentDto>(payment));
        }

        private async Task ApplySuccessfulPaymentAsync(Payment payment, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var balance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken);

            if (payment.PaymentType == (byte)PaymentType.Installment && payment.InstallmentId.HasValue)
            {
                var installment = await _repositoryManager.InstallmentRepository.GetByInstallmentIdAsync(payment.InstallmentId.Value, cancellationToken);
                if (installment != null)
                {
                    installment.PaymentId = payment.PaymentId;
                    installment.PaymentDate = now;
                    installment.PaymentDate_shansi = PersianDateHelper.ToShamsi(now);
                    installment.InstallmentStatus = (byte)InstallmentStatus.Cleared;
                    installment.UpdateDate = now;
                    _repositoryManager.InstallmentRepository.Update(installment);

                    var loan = await _repositoryManager.LoanRepository.GetByLoanIdAsync(installment.LoanId, cancellationToken);
                    if (loan != null)
                    {
                        loan.RemainAmount = (loan.RemainAmount ?? loan.LoanAmount) - payment.Amount;
                        if (loan.RemainAmount < 0) loan.RemainAmount = 0;

                        var all = (await _repositoryManager.InstallmentRepository.GetByLoanIdAsync(loan.LoanId, cancellationToken)).ToList();
                        if (all.All(a => a.InstallmentId == installment.InstallmentId || a.InstallmentStatus == (byte)InstallmentStatus.Cleared))
                            loan.LoanStatus = (byte)LoanStatus.Cleared;

                        loan.UpdateDate = now;
                        _repositoryManager.LoanRepository.Update(loan);
                    }
                }

                _repositoryManager.FundTransactionRepository.Insert(new FundTransaction
                {
                    TransactionType = (byte)FundTransactionType.InstallmentIn,
                    Amount = payment.Amount,
                    BalanceAfter = balance + payment.Amount,
                    UserId = payment.UserId,
                    PaymentId = payment.PaymentId,
                    TransactionDate = now,
                    TransactionDate_Shamsi = PersianDateHelper.ToShamsi(now),
                    Description = "دریافت قسط وام",
                    CreateDate = now,
                    Active = true
                });
            }
            else if (payment.PaymentType == (byte)PaymentType.Donation || payment.PaymentType == (byte)PaymentType.Manual)
            {
                var txType = payment.PaymentType == (byte)PaymentType.Donation
                    ? FundTransactionType.DonationIn
                    : FundTransactionType.ManualAdjust;

                _repositoryManager.FundTransactionRepository.Insert(new FundTransaction
                {
                    TransactionType = (byte)txType,
                    Amount = payment.Amount,
                    BalanceAfter = balance + payment.Amount,
                    UserId = payment.UserId,
                    PaymentId = payment.PaymentId,
                    TransactionDate = now,
                    TransactionDate_Shamsi = PersianDateHelper.ToShamsi(now),
                    Description = payment.PaymentType == (byte)PaymentType.Donation ? "کمک خیرین" : "واریز دستی",
                    CreateDate = now,
                    Active = true
                });
            }
        }

        public async Task<List<PaymentDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var list = (await _repositoryManager.PaymentRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
            return _mapper.Map<List<PaymentDto>>(list);
        }

        public async Task<List<PaymentDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = (await _repositoryManager.PaymentRepository.GetAllAsync(cancellationToken)).ToList();
            return _mapper.Map<List<PaymentDto>>(list);
        }
    }
}
