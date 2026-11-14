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

            var roleName = user.UserRole?.RoleName ?? string.Empty;
            var keepManager = user.UserStatus == (byte)UserStatus.Manager
                || user.RoleId == (int)UserRoleType.Manager
                || roleName == "مدیر صندوق"
                || roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            if (!keepManager)
                user.UserStatus = (byte)UserStatus.LoanApplicant;
            _repositoryManager.UserRepository.Update(user);

            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var loanDto = _mapper.Map<LoanDto>(loan);
            loanDto.QueuePosition = 0;

            return ApiResultDto.Ok("درخواست شما ثبت شد و در نوبت است. هر وقت نوبت شما شود، در بله خبر می‌دهیم.", loanDto);
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
                await _baleService.SendWithKeyboardAsync(user.BaleChatId,
                    $"وام شما پرداخت شد.\nمبلغ: {MoneyText.Toman(loan.LoanAmount)}\nتعداد قسط: {installmentCount}\nاولین قسط حدود ۳۰ روز دیگر است.\nمانده و پرداخت قسط را با دکمه‌های پایین ببینید.",
                    BaleKeyboards.For(false, false),
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

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(loan.UserId, cancellationToken);
            if (user != null && !string.IsNullOrWhiteSpace(user.BaleChatId))
            {
                var reasonText = string.IsNullOrWhiteSpace(reason) ? "" : "\nدلیل: " + reason.Trim();
                await _baleService.SendWithKeyboardAsync(user.BaleChatId,
                    "درخواست وام شما پذیرفته نشد." + reasonText + "\nاگر پرسشی دارید با مسئول صندوق صحبت کنید.",
                    BaleKeyboards.For(false, user.RoleId == (int)UserRoleType.Donator),
                    cancellationToken);
            }

            return ApiResultDto.Ok("درخواست وام رد شد");
        }

        public async Task<ApiResultDto> MoveInQueueAsync(int loanId, int direction, int managerUserId, CancellationToken cancellationToken = default)
        {
            if (direction != -1 && direction != 1)
                return ApiResultDto.Fail("جابه‌جایی نامعتبر است");

            var queue = (await _repositoryManager.LoanRepository.GetWaitingQueueAsync(cancellationToken)).ToList();
            var index = queue.FindIndex(l => l.LoanId == loanId);
            if (index < 0)
                return ApiResultDto.Fail("این درخواست در صف نیست");

            var target = index + direction;
            if (target < 0)
                return ApiResultDto.Fail("این نفر همین حالا اول صف است");
            if (target >= queue.Count)
                return ApiResultDto.Fail("این نفر همین حالا آخر صف است");

            for (var i = 0; i < queue.Count; i++)
                queue[i].QueuePriority = (i + 1) * 10;

            var swap = queue[index].QueuePriority;
            queue[index].QueuePriority = queue[target].QueuePriority;
            queue[target].QueuePriority = swap;

            var now = DateTime.Now;
            foreach (var item in queue)
            {
                item.UpdateDate = now;
                item.ModifyUserId = managerUserId;
                _repositoryManager.LoanRepository.Update(item);
            }

            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok(direction < 0 ? "این نفر یک نوبت جلوتر آمد" : "این نفر یک نوبت عقب‌تر رفت");
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
            return await MapLoansToDtosAsync(loans, setQueuePositions: false, cancellationToken, exposeQueuePosition: false);
        }

        public async Task<LoanDto> GetByIdAsync(int loanId, CancellationToken cancellationToken = default)
        {
            var loan = await _repositoryManager.LoanRepository.GetByLoanIdAsync(loanId, cancellationToken);
            if (loan == null) return null;
            NormalizeLoanDates(loan);
            var dto = _mapper.Map<LoanDto>(loan);
            dto.QueuePosition = 0;
            return dto;
        }

        private async Task<List<LoanDto>> MapLoansToDtosAsync(List<Loan> loans, bool setQueuePositions, CancellationToken cancellationToken, bool exposeQueuePosition = true)
        {
            foreach (var loan in loans)
                NormalizeLoanDates(loan);

            var dtos = _mapper.Map<List<LoanDto>>(loans);
            if (!exposeQueuePosition)
            {
                foreach (var dto in dtos)
                    dto.QueuePosition = 0;
                return dtos;
            }

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

        public async Task<ApiResultDto> ImportPastLoanAsync(ImportPastLoanDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                return ApiResultDto.Fail("اطلاعات وام قبلی ناقص است");

            var firstName = (dto.FirstName ?? "").Trim();
            var lastName = (dto.LastName ?? "").Trim();
            if (firstName.Length < 2 || lastName.Length < 2)
                return ApiResultDto.Fail("نام و نام خانوادگی را بنویسید");

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode ?? "").Trim();
            var mobile = PersianDateHelper.ToEnglishDigits(dto.Mobile ?? "").Trim();
            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return ApiResultDto.Fail("کد ملی معتبر نیست");
            if (!PersianDateHelper.IsValidMobile(mobile))
                return ApiResultDto.Fail("شماره موبایل معتبر نیست. مثال: 09121234567");

            if (dto.LoanAmountToman <= 0)
                return ApiResultDto.Fail("مبلغ وام را به تومان بنویسید");
            if (dto.InstallmentCount < 1 || dto.InstallmentCount > 60)
                return ApiResultDto.Fail("تعداد قسط باید بین ۱ و ۶۰ باشد");
            if (dto.PaidInstallmentCount < 0 || dto.PaidInstallmentCount > dto.InstallmentCount)
                return ApiResultDto.Fail("تعداد قسط پرداخت‌شده بیشتر از کل قسط‌هاست");

            var loanAmount = dto.LoanAmountToman * 10;
            var unpaidCount = dto.InstallmentCount - dto.PaidInstallmentCount;
            DateTime firstUnpaidDate = DateTime.Today.AddDays(FirstInstallmentDelayDays);
            if (unpaidCount > 0 && !string.IsNullOrWhiteSpace(dto.NextDueDateShamsi))
            {
                var parsed = PersianDateHelper.FromShamsi(PersianDateHelper.ToEnglishDigits(dto.NextDueDateShamsi).Trim());
                if (!parsed.HasValue)
                    return ApiResultDto.Fail("تاریخ قسط بعدی درست نیست. مثال: 1404/12/01");
                firstUnpaidDate = parsed.Value.Date;
            }

            var existing = await _repositoryManager.UserRepository.GetByNationalCodeAsync(nationalCode, cancellationToken);
            var mobileOwner = await _repositoryManager.UserRepository.GetByMobileAsync(mobile, cancellationToken);
            User user;
            string createdPassword = null;

            if (existing == null)
            {
                if (mobileOwner != null)
                    return ApiResultDto.Fail("این شماره موبایل برای شخص دیگری ثبت شده است");

                var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
                var role = roles.FirstOrDefault(r => r.RoleName == "عضو / وام‌گیرنده")
                    ?? roles.FirstOrDefault(r => r.RoleId == (int)UserRoleType.Member);
                if (role == null)
                    return ApiResultDto.Fail("نقش عضو در سیستم نیست");

                createdPassword = PersianDateHelper.GeneratePassword();
                var nowUser = DateTime.Now;
                user = new User
                {
                    NationalCode = nationalCode,
                    Mobile = mobile,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = (firstName + " " + lastName).Trim(),
                    UserName = nationalCode,
                    Password = createdPassword,
                    RoleId = role.RoleId,
                    UserStatus = unpaidCount > 0 ? (byte)UserStatus.Borrower : (byte)UserStatus.LoanApplicant,
                    UserActiveType = (byte)UserActiveType.Active,
                    UserStatusLife = (byte)UserStatusLife.living,
                    UserGender = (byte)UserGender.Man,
                    HasBaleApp = false,
                    CreateDate = nowUser,
                    Active = true,
                    Description = "ثبت‌شده از سوابق قبل از سامانه"
                };
                _repositoryManager.UserRepository.Insert(user);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                if (existing.RoleId == (int)UserRoleType.Donator || existing.UserStatus == (byte)UserStatus.Donator)
                    return ApiResultDto.Fail("این کد ملی برای یک خیر ثبت شده و وام به او وصل نمی‌شود");
                if (mobileOwner != null && mobileOwner.UserId != existing.UserId)
                    return ApiResultDto.Fail("این شماره موبایل برای شخص دیگری است. موبایل ثبت‌شده این کد ملی: " + existing.Mobile);

                var active = await _repositoryManager.LoanRepository.GetActiveLoanByUserIdAsync(existing.UserId, cancellationToken);
                if (active != null)
                    return ApiResultDto.Fail("این شخص همین حالا یک وام باز دارد. تا تمام نشود وام قبلی دیگری ثبت نمی‌شود");

                user = existing;
                if (string.IsNullOrWhiteSpace(user.FirstName)) user.FirstName = firstName;
                if (string.IsNullOrWhiteSpace(user.LastName)) user.LastName = lastName;
                if (string.IsNullOrWhiteSpace(user.FullName))
                    user.FullName = (user.FirstName + " " + user.LastName).Trim();
                if (string.IsNullOrWhiteSpace(user.Mobile))
                    user.Mobile = mobile;
                user.UserStatus = unpaidCount > 0 ? (byte)UserStatus.Borrower : user.UserStatus;
                user.UpdateDate = DateTime.Now;
                _repositoryManager.UserRepository.Update(user);
            }

            var now = DateTime.Now;
            var note = string.IsNullOrWhiteSpace(dto.Note) ? "ثبت سابقه قبل از سامانه" : dto.Note.Trim();
            var payment = new Payment
            {
                UserId = user.UserId,
                Amount = loanAmount,
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentStatus = (byte)PaymentStatus.Pay,
                PaymentType = (byte)PaymentType.LoanDisbursement,
                PaymentMethod = (byte)PaymentMethod.Manual,
                GatewayStatus = (byte)GatewayStatus.Success,
                Confirm = true,
                ConfirmUserId = dto.ManagerUserId,
                ConfirmUserDescription = note + " — این رکورد موجودی امروز صندوق را تغییر نمی‌دهد",
                Description = note,
                CreateDate = now,
                Active = true
            };
            _repositoryManager.PaymentRepository.Insert(payment);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var baseInstallment = loanAmount / dto.InstallmentCount;
            var remainder = loanAmount % dto.InstallmentCount;
            long remain = 0;
            var installments = new List<Installment>();
            for (var i = 0; i < dto.InstallmentCount; i++)
            {
                var amount = baseInstallment + (i == dto.InstallmentCount - 1 ? remainder : 0);
                var isPaid = i < dto.PaidInstallmentCount;
                var due = firstUnpaidDate.AddMonths(i - dto.PaidInstallmentCount);
                if (!isPaid)
                    remain += amount;
                installments.Add(new Installment
                {
                    InstallmentDate = due,
                    InstallmentDate_shansi = PersianDateHelper.ToShamsi(due),
                    InstallmentAmount = amount,
                    InstallmentStatus = isPaid ? (byte)InstallmentStatus.Cleared : (byte)InstallmentStatus.Normal,
                    PaymentDate = isPaid ? due : (DateTime?)null,
                    PaymentDate_shansi = isPaid ? PersianDateHelper.ToShamsi(due) : null,
                    CreateDate = now,
                    Active = true
                });
            }

            var loan = new Loan
            {
                UserId = user.UserId,
                PaymentId = payment.PaymentId,
                LoanAmount = loanAmount,
                RemainAmount = remain,
                InstallmentCount = dto.InstallmentCount,
                InstallmentAmount = baseInstallment,
                RequestDate = now,
                RequestDate_Shamsi = PersianDateHelper.ToShamsi(now),
                PaymentDate = now,
                PaymentDate_Shamsi = PersianDateHelper.ToShamsi(now),
                QueuePriority = 0,
                LoanRequestStatus = (byte)LoanRequestStatus.Disbursed,
                LoanStatus = remain == 0 ? (byte)LoanStatus.Cleared : (byte)LoanStatus.Normal,
                Description = note,
                CreateDate = now,
                Active = true,
                ModifyUserId = dto.ManagerUserId
            };
            _repositoryManager.LoanRepository.Insert(loan);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var installment in installments)
                installment.LoanId = loan.LoanId;
            _repositoryManager.InstallmentRepository.InsertAddRangeAsync(installments);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var summary = user.FullName + " ثبت شد. "
                + "مبلغ وام: " + MoneyText.Toman(loanAmount) + ". "
                + "قسط پرداخت‌شده: " + dto.PaidInstallmentCount + " از " + dto.InstallmentCount + ". "
                + "مانده: " + MoneyText.Toman(remain) + ".";
            if (createdPassword != null)
                summary += " عضو جدید است. رمز ورود به سایت: " + createdPassword + ".";
            else
                summary += " این شخص از قبل عضو بود.";
            summary += " موجودی صندوق تغییر نکرد.";

            return ApiResultDto.Ok(summary, new
            {
                user.UserId,
                user.FullName,
                loan.LoanId,
                RemainAmount = remain,
                dto.PaidInstallmentCount,
                dto.InstallmentCount
            });
        }

        public async Task<int> GetWaitingCountAsync(CancellationToken cancellationToken = default) =>
            await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken);

        public async Task<int> GetDisbursedCountAsync(CancellationToken cancellationToken = default) =>
            await _repositoryManager.LoanRepository.GetDisbursedCountAsync(cancellationToken);
    }
}
