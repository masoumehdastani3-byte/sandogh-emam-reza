using AutoMapper;
using Contracts.Enums;
using Contracts.Helpers;
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
        private readonly NikooPaymentHubClient _nikooPaymentHub;

        public PaymentService(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IZarinPalService zarinPalService,
            IBaleService baleService,
            IConfiguration configuration,
            NikooPaymentHubClient nikooPaymentHub)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _zarinPalService = zarinPalService;
            _baleService = baleService;
            _configuration = configuration;
            _nikooPaymentHub = nikooPaymentHub;
        }

        public async Task<ApiResultDto> StartZarinPalPaymentAsync(ZarinPalRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                return ApiResultDto.Fail("درخواست نامعتبر است");

            if (!string.IsNullOrWhiteSpace(dto.PayToken))
            {
                var resolved = await ResolvePayTokenAsync(dto.PayToken, cancellationToken);
                if (!resolved.Success)
                    return resolved;

                var info = (InstallmentPayLinkInfo)resolved.Data;
                dto.UserId = info.UserId;
                dto.InstallmentId = info.InstallmentId;
                dto.Amount = info.InstallmentAmount;
                dto.PaymentType = (byte)PaymentType.Installment;
                if (string.IsNullOrWhiteSpace(dto.Description))
                    dto.Description = $"پرداخت قسط شماره {info.InstallmentId}";
            }

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            if (user.Active != true)
                return ApiResultDto.Fail("حساب کاربری غیرفعال است");

            if (dto.Amount <= 0)
                return ApiResultDto.Fail("مبلغ نامعتبر است");

            long gatewayAmount = dto.Amount;
            long gatewayFee = 0;
            string description = dto.Description;

            if (dto.PaymentType == (byte)PaymentType.Installment)
            {
                if (!dto.InstallmentId.HasValue)
                    return ApiResultDto.Fail("پرداخت قسط فقط از لیست اقساط یا لینک پرداخت قسط امکان‌پذیر است");

                var installment = await _repositoryManager.InstallmentRepository.GetByInstallmentIdAsync(dto.InstallmentId.Value, cancellationToken);
                if (installment == null)
                    return ApiResultDto.Fail("قسط یافت نشد");
                if (installment.InstallmentStatus == (byte)InstallmentStatus.Cleared)
                    return ApiResultDto.Fail("این قسط قبلاً پرداخت شده است");
                if (installment.Loan?.UserId != dto.UserId)
                    return ApiResultDto.Fail("قسط متعلق به این کاربر نیست");
                if (dto.Amount != installment.InstallmentAmount)
                    return ApiResultDto.Fail("مبلغ پرداخت باید برابر مبلغ قسط باشد");

                var payable = ZarinPalFeeHelper.CalculatePayable(installment.InstallmentAmount);
                gatewayAmount = payable.PayableAmount;
                gatewayFee = payable.FeeAmount;
                description = (dto.Description ?? $"پرداخت قسط شماره {installment.InstallmentId}")
                    + $" | مبلغ قسط: {installment.InstallmentAmount:N0} ریال"
                    + $" | کارمزد درگاه: {gatewayFee:N0} ریال";
            }
            else if (dto.PaymentType != (byte)PaymentType.Donation)
            {
                return ApiResultDto.Fail("پرداخت آنلاین فقط برای کمک خیرین یا پرداخت قسط از لیست اقساط مجاز است");
            }

            var baseUrl = await AppUrlHelper.GetBaseUrlAsync(_repositoryManager, _configuration, cancellationToken);

            var now = DateTime.Now;
            var payment = new Payment
            {
                UserId = dto.UserId,
                InstallmentId = dto.InstallmentId,
                Amount = gatewayAmount,
                GatewayFeeAmount = gatewayFee,
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentStatus = (byte)PaymentStatus.Receipt,
                PaymentType = dto.PaymentType,
                PaymentMethod = (byte)PaymentMethod.ZarinPal,
                GatewayStatus = (byte)GatewayStatus.Pending,
                Confirm = false,
                Description = description,
                CreateDate = now,
                Active = true
            };

            _repositoryManager.PaymentRepository.Insert(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            // پرداخت از طریق دامنه نیکو سیستم آفرین (صفحه Start + Callback زرین‌پال آنجا انجام می‌شود)
            if (_nikooPaymentHub.IsEnabled)
            {
                var amountToman = gatewayAmount / 10;
                if (gatewayAmount % 10 != 0)
                    amountToman = (gatewayAmount + 9) / 10;

                if (amountToman < 1000)
                {
                    payment.GatewayStatus = (byte)GatewayStatus.Failed;
                    payment.Description = "مبلغ کمتر از حداقل درگاه است";
                    _repositoryManager.PaymentRepository.Update(payment);
                    await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                    return ApiResultDto.Fail("مبلغ پرداخت کمتر از حداقل مجاز درگاه است");
                }

                var returnUrl = $"{baseUrl.TrimEnd('/')}/api/payment/nikoo/return";
                var notifyUrl = $"{baseUrl.TrimEnd('/')}/api/payment/nikoo/notify";
                var hub = await _nikooPaymentHub.CreatePaymentAsync(
                    amountToman,
                    returnUrl,
                    description ?? $"پرداخت لبخند فردا - کاربر {user.UserId}",
                    payment.PaymentId.ToString(),
                    notifyUrl,
                    user.FullName,
                    user.Mobile,
                    cancellationToken);

                if (!hub.Success)
                {
                    payment.GatewayStatus = (byte)GatewayStatus.Failed;
                    payment.Description = hub.Message;
                    _repositoryManager.PaymentRepository.Update(payment);
                    await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                    return ApiResultDto.Fail(hub.Message);
                }

                payment.ZarinPalAuthority = hub.Token;
                _repositoryManager.PaymentRepository.Update(payment);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

                return ApiResultDto.Ok("هدایت به درگاه", new
                {
                    payment.PaymentId,
                    Authority = hub.Token,
                    GatewayUrl = hub.PaymentUrl,
                    Amount = gatewayAmount,
                    GatewayFeeAmount = gatewayFee,
                    BaseAmount = dto.Amount,
                    Via = "nikoo"
                });
            }

            var callback = string.IsNullOrWhiteSpace(dto.CallbackUrl)
                ? (_configuration["ZarinPal:CallbackUrl"] ?? $"{baseUrl}/api/payment/zarinpal/callback")
                : dto.CallbackUrl;

            var request = await _zarinPalService.RequestPaymentAsync(
                gatewayAmount,
                description ?? $"پرداخت لبخند فردا - کاربر {user.UserId}",
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
                GatewayUrl = request.GatewayUrl,
                Amount = gatewayAmount,
                GatewayFeeAmount = gatewayFee,
                BaseAmount = dto.Amount,
                Via = "direct"
            });
        }

        public async Task<ApiResultDto> GetInstallmentPayLinkAsync(string payToken, CancellationToken cancellationToken = default)
        {
            return await ResolvePayTokenAsync(payToken, cancellationToken);
        }

        private async Task<ApiResultDto> ResolvePayTokenAsync(string payToken, CancellationToken cancellationToken)
        {
            var signingKey = _configuration["App:LinkSigningKey"] ?? _configuration["ZarinPal:MerchantId"];
            if (!SecureLinkHelper.TryParseInstallmentPayToken(payToken, signingKey, out var installmentId, out var userId, out var error))
                return ApiResultDto.Fail(error);

            var installment = await _repositoryManager.InstallmentRepository.GetByInstallmentIdAsync(installmentId, cancellationToken);
            if (installment == null)
                return ApiResultDto.Fail("قسط یافت نشد");
            if (installment.InstallmentStatus == (byte)InstallmentStatus.Cleared)
                return ApiResultDto.Fail("این قسط قبلاً پرداخت شده است");
            if (installment.Loan?.UserId != userId)
                return ApiResultDto.Fail("لینک پرداخت متعلق به این قسط نیست");

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null || user.Active != true)
                return ApiResultDto.Fail("حساب کاربری یافت نشد یا غیرفعال است");

            var payable = ZarinPalFeeHelper.CalculatePayable(installment.InstallmentAmount);
            return ApiResultDto.Ok("OK", new InstallmentPayLinkInfo
            {
                UserId = userId,
                InstallmentId = installment.InstallmentId,
                InstallmentAmount = installment.InstallmentAmount,
                GatewayFeeAmount = payable.FeeAmount,
                PayableAmount = payable.PayableAmount,
                DueDate_Shamsi = installment.InstallmentDate_shansi,
                MemberName = user.FullName
            });
        }

        private sealed class InstallmentPayLinkInfo
        {
            public int UserId { get; set; }
            public int InstallmentId { get; set; }
            public long InstallmentAmount { get; set; }
            public long GatewayFeeAmount { get; set; }
            public long PayableAmount { get; set; }
            public string DueDate_Shamsi { get; set; }
            public string MemberName { get; set; }
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

        public async Task<ApiResultDto> CompleteNikooPaymentAsync(
            string token,
            string status,
            string amount,
            string externalRef,
            string refId,
            string timestamp,
            string signature,
            CancellationToken cancellationToken = default)
        {
            if (!_nikooPaymentHub.VerifyCallback(token, status, amount, externalRef, refId, timestamp, signature))
                return ApiResultDto.Fail("امضای بازگشت از درگاه نامعتبر است");

            Payment payment = null;
            if (!string.IsNullOrWhiteSpace(token))
                payment = await _repositoryManager.PaymentRepository.GetByAuthorityAsync(token, cancellationToken);

            if (payment == null && int.TryParse(externalRef, out var paymentId))
                payment = await _repositoryManager.PaymentRepository.GetByPaymentIdAsync(paymentId, cancellationToken);

            if (payment == null)
                return ApiResultDto.Fail("تراکنش یافت نشد");

            if (payment.GatewayStatus == (byte)GatewayStatus.Success)
                return ApiResultDto.Ok("این تراکنش قبلاً تایید شده است", _mapper.Map<PaymentDto>(payment));

            if (!string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                payment.GatewayStatus = (byte)GatewayStatus.Failed;
                payment.Description = (payment.Description ?? "") + " | وضعیت نیکو: " + (status ?? "failed");
                payment.UpdateDate = DateTime.Now;
                _repositoryManager.PaymentRepository.Update(payment);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResultDto.Fail("پرداخت ناموفق بود یا توسط کاربر لغو شد");
            }

            payment.GatewayStatus = (byte)GatewayStatus.Success;
            payment.Confirm = true;
            payment.ZarinPalAuthority = token ?? payment.ZarinPalAuthority;
            payment.ZarinPalRefId = refId;
            payment.RefrenceNumber = refId;
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
                GatewayFeeAmount = 0,
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
            // مبلغ خالص صندوق = مبلغ درگاه منهای کارمزد زرین‌پال
            var netAmount = payment.Amount - payment.GatewayFeeAmount;
            if (netAmount < 0) netAmount = 0;

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
                        // کاهش بدهی وام فقط به اندازه مبلغ قسط (نه کارمزد درگاه)
                        var installmentNet = installment.InstallmentAmount;
                        loan.RemainAmount = (loan.RemainAmount ?? loan.LoanAmount) - installmentNet;
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
                    Amount = netAmount,
                    BalanceAfter = balance + netAmount,
                    UserId = payment.UserId,
                    PaymentId = payment.PaymentId,
                    TransactionDate = now,
                    TransactionDate_Shamsi = PersianDateHelper.ToShamsi(now),
                    Description = payment.GatewayFeeAmount > 0
                        ? $"دریافت قسط وام (کارمزد درگاه {payment.GatewayFeeAmount:N0} ریال از مبلغ پرداخت‌شده کسر شده)"
                        : "دریافت قسط وام",
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
                    Amount = netAmount,
                    BalanceAfter = balance + netAmount,
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
