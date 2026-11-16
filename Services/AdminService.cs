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
        private readonly IBaleService _baleService;

        public AdminService(IRepositoryManager repositoryManager, IMapper mapper, IBaleService baleService)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _baleService = baleService;
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
            var installmentStats = await GetInstallmentCollectionCountsAsync(today, cancellationToken);

            return new AdminDashboardDto
            {
                FundBalance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken),
                WaitingQueueCount = await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken),
                TodayPaymentsCount = todayPayments.Count,
                TodayPaymentsAmount = todayPayments.Sum(p => p.Amount),
                OverdueInstallmentsCount = installmentStats.overdue,
                DueInThreeDaysInstallmentsCount = installmentStats.dueInThreeDays
            };
        }

        public async Task<InstallmentCollectionReportDto> GetInstallmentCollectionReportAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var dueInThreeDays = today.AddDays(3);
            var unpaid = (await _repositoryManager.InstallmentRepository.GetAllAsync(cancellationToken))
                .Where(i => i.InstallmentStatus != (byte)InstallmentStatus.Cleared && i.Active != false)
                .ToList();

            var overdue = unpaid.Where(i => i.InstallmentDate.Date < today).ToList();
            var dueSoon = unpaid.Where(i => i.InstallmentDate.Date == dueInThreeDays).ToList();

            return new InstallmentCollectionReportDto
            {
                OverdueCount = overdue.Count,
                DueInThreeDaysCount = dueSoon.Count,
                OverdueInstallments = MapInstallmentCollectionItems(overdue, today),
                DueInThreeDaysInstallments = MapInstallmentCollectionItems(dueSoon, today)
            };
        }

        public async Task<List<NotificationLogDto>> GetNotificationLogsAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _repositoryManager.NotificationLogRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<List<NotificationLogDto>>(logs);
        }

        private async Task<(int overdue, int dueInThreeDays)> GetInstallmentCollectionCountsAsync(DateTime today, CancellationToken cancellationToken)
        {
            var dueInThreeDays = today.AddDays(3);
            var unpaid = (await _repositoryManager.InstallmentRepository.GetAllAsync(cancellationToken))
                .Where(i => i.InstallmentStatus != (byte)InstallmentStatus.Cleared && i.Active != false);

            var overdue = unpaid.Count(i => i.InstallmentDate.Date < today);
            var dueSoon = unpaid.Count(i => i.InstallmentDate.Date == dueInThreeDays);
            return (overdue, dueSoon);
        }

        private static List<InstallmentCollectionItemDto> MapInstallmentCollectionItems(IEnumerable<Installment> items, DateTime today)
        {
            return items.Select(i =>
            {
                var due = i.InstallmentDate.Date;
                var days = (due - today).Days;
                return new InstallmentCollectionItemDto
                {
                    InstallmentId = i.InstallmentId,
                    LoanId = i.LoanId,
                    UserId = i.Loan?.UserId ?? 0,
                    MemberName = i.Loan?.User?.FullName ?? "-",
                    Mobile = i.Loan?.User?.Mobile ?? "",
                    InstallmentAmount = i.InstallmentAmount,
                    InstallmentDate_Shamsi = i.InstallmentDate_shansi ?? PersianDateHelper.ToShamsi(i.InstallmentDate),
                    DaysUntilDue = days,
                    StatusLabel = days < 0 ? $"عقب‌افتاده ({Math.Abs(days)} روز)" : days == 0 ? "سررسید امروز" : days == 3 ? "۳ روز مانده" : $"{days} روز مانده"
                };
            }).OrderBy(x => x.DaysUntilDue).ToList();
        }

        public async Task<ManagementReportDto> GetManagementReportAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var pc = new System.Globalization.PersianCalendar();
            var currentYear = pc.GetYear(now);
            var currentMonth = pc.GetMonth(now);

            var loans = (await _repositoryManager.LoanRepository.GetAllAsync(cancellationToken)).ToList();
            var disbursedLoans = loans
                .Where(l => l.LoanRequestStatus == (byte)LoanRequestStatus.Disbursed)
                .ToList();
            var debtorLoans = disbursedLoans
                .Where(l => l.LoanStatus != (byte)LoanStatus.Cleared && (l.RemainAmount ?? 0) > 0)
                .ToList();

            var transactions = (await _repositoryManager.FundTransactionRepository.GetAllAsync(cancellationToken))
                .Where(t => t.Active != false && t.Amount > 0)
                .ToList();

            var monthlyInflows = transactions
                .GroupBy(t =>
                {
                    var date = t.TransactionDate;
                    return new { Year = pc.GetYear(date), Month = pc.GetMonth(date) };
                })
                .Select(g => new MonthlyFundInflowDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthLabel = $"{g.Key.Year:0000}/{g.Key.Month:00}",
                    TotalAmount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            var currentMonthInflow = transactions
                .Where(t => pc.GetYear(t.TransactionDate) == currentYear && pc.GetMonth(t.TransactionDate) == currentMonth)
                .Sum(t => t.Amount);

            return new ManagementReportDto
            {
                FundBalance = await _repositoryManager.FundTransactionRepository.GetCurrentBalanceAsync(cancellationToken),
                DebtorLoansCount = debtorLoans.Count,
                TotalDebtAmount = debtorLoans.Sum(l => l.RemainAmount ?? 0),
                DisbursedLoansCount = disbursedLoans.Count,
                TotalDisbursedAmount = disbursedLoans.Sum(l => l.LoanAmount),
                CurrentMonthInflow = currentMonthInflow,
                ReportDate_Shamsi = PersianDateHelper.ToShamsi(now),
                MonthlyInflows = monthlyInflows
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

        public async Task<List<SettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var list = (await _repositoryManager.SettingRepository.GetAllAsync(cancellationToken))
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SettingName);
            return _mapper.Map<List<SettingDto>>(list);
        }

        public async Task<ApiResultDto> UpdateSettingAsync(UpdateSettingDto dto, CancellationToken cancellationToken = default)
        {
            if (!await IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            if (string.IsNullOrWhiteSpace(dto.SettingName))
                return ApiResultDto.Fail("نام تنظیم الزامی است");

            var setting = await _repositoryManager.SettingRepository.GetByNameAsync(dto.SettingName.Trim(), cancellationToken);
            if (setting == null)
                return ApiResultDto.Fail("تنظیم یافت نشد");

            var value = dto.SettingValue?.Trim();
            if (dto.SettingName == AppUrlHelper.AppBaseUrlSettingName)
            {
                value = value?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(value) ||
                    !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return ApiResultDto.Fail("آدرس سایت معتبر نیست (مثال: https://yourdomain.com)");
            }

            setting.SettingValue = value;
            setting.UpdateDate = DateTime.Now;
            _repositoryManager.SettingRepository.Update(setting);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("تنظیم ذخیره شد", _mapper.Map<SettingDto>(setting));
        }

        public async Task<List<UserDto>> GetMembersAsync(CancellationToken cancellationToken = default)
        {
            var users = (await _repositoryManager.UserRepository.GetAllAsync(cancellationToken))
                .OrderByDescending(u => u.CreateDate)
                .ThenBy(u => u.UserId)
                .ToList();

            var dtos = _mapper.Map<List<UserDto>>(users);
            var openUserIds = await GetOpenLoanUserIdsAsync(cancellationToken);
            foreach (var dto in dtos)
            {
                dto.Password = null;
                dto.HasOpenLoan = openUserIds.Contains(dto.UserId);
            }
            return dtos;
        }

        public async Task<ApiResultDto> CreateMemberAsync(AdminCreateMemberDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                return ApiResultDto.Fail("اطلاعات عضو نامعتبر است");

            if (!await IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var firstName = dto.FirstName?.Trim() ?? "";
            var lastName = dto.LastName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return ApiResultDto.Fail("نام و نام خانوادگی را بنویسید");
            if (firstName.Length > 50 || lastName.Length > 50)
                return ApiResultDto.Fail("نام یا نام خانوادگی بلندتر از حد مجاز است");

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode ?? "").Trim();
            var mobile = PersianDateHelper.ToEnglishDigits(dto.Mobile ?? "").Trim();
            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return ApiResultDto.Fail("کد ملی نامعتبر است");
            if (!PersianDateHelper.IsValidMobile(mobile))
                return ApiResultDto.Fail("شماره موبایل نامعتبر است. مثال: 09121234567");

            var existingNational = await _repositoryManager.UserRepository.GetByNationalCodeAsync(nationalCode, cancellationToken);
            if (existingNational != null)
                return ApiResultDto.Fail("این کد ملی قبلاً ثبت شده است");

            var existingMobile = await _repositoryManager.UserRepository.GetByMobileAsync(mobile, cancellationToken);
            if (existingMobile != null)
                return ApiResultDto.Fail("این شماره موبایل قبلاً ثبت شده است");

            var roleType = (UserRoleType)dto.RoleType;
            string roleName;
            byte userStatus;
            if (roleType == UserRoleType.Manager)
            {
                roleName = "مدیر صندوق";
                userStatus = (byte)UserStatus.Manager;
            }
            else if (roleType == UserRoleType.Donator)
            {
                if (dto.MonthlyDonationAmount == null || dto.MonthlyDonationAmount <= 0)
                    return ApiResultDto.Fail("مبلغ کمک ماهانه برای خیرین الزامی است");
                roleName = "خیر";
                userStatus = (byte)UserStatus.Donator;
            }
            else if (roleType == UserRoleType.Member)
            {
                roleName = "عضو / وام‌گیرنده";
                userStatus = (byte)UserStatus.LoanApplicant;
            }
            else
                return ApiResultDto.Fail("نوع حساب نامعتبر است");

            var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
            var role = roles.FirstOrDefault(r => r.RoleName == roleName)
                ?? roles.FirstOrDefault(r => r.RoleId == (int)roleType);
            if (role == null)
                return ApiResultDto.Fail("نقش کاربری در سیستم تعریف نشده است. یک‌بار برنامه را اجرا کنید تا نقش‌ها ساخته شوند.");

            var password = PersianDateHelper.GeneratePassword();
            var note = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (note != null && note.Length > 500)
                note = note.Substring(0, 500);

            var user = new User
            {
                NationalCode = nationalCode,
                Mobile = mobile,
                FirstName = firstName,
                LastName = lastName,
                FullName = (firstName + " " + lastName).Trim(),
                UserName = nationalCode,
                Password = password,
                RoleId = role.RoleId,
                UserStatus = userStatus,
                UserActiveType = (byte)UserActiveType.Active,
                UserStatusLife = (byte)UserStatusLife.living,
                UserGender = (byte)UserGender.Man,
                HasBaleApp = true,
                MonthlyDonationAmount = roleType == UserRoleType.Donator ? dto.MonthlyDonationAmount : null,
                Description = note,
                CreateUserId = dto.ManagerUserId,
                CreateDate = DateTime.Now,
                Active = true
            };

            _repositoryManager.UserRepository.Insert(user);
            var save = await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            if (save <= 0)
                return ApiResultDto.Fail("ثبت عضو با مشکل مواجه شد");

            var botLink = _baleService.GetBotStartLink(user.UserId);
            string queueError = null;
            if (roleType == UserRoleType.Member)
                queueError = await EnqueueBorrowerAsync(user, dto.ManagerUserId, cancellationToken);
            var queued = roleType == UserRoleType.Member && queueError == null;

            var message = roleType == UserRoleType.Manager
                ? "مدیر صندوق ثبت شد. با کد ملی و این رمز وارد پنل مدیران می‌شود و همان دسترسی شما را دارد."
                : queued
                    ? "عضو ثبت شد و به صف وام رفت. مبلغ را از بخش صف وام تعیین کنید. رمز ورود را هم به او بدهید."
                    : queueError != null
                        ? "عضو ثبت شد، ولی وارد صف وام نشد: " + queueError
                        : "عضو ثبت شد. رمز ورود را به او بدهید. با استارت ربات بله، پیام‌های صندوق برایش می‌رود.";
            return ApiResultDto.Ok(
                message,
                new
                {
                    user.UserId,
                    user.NationalCode,
                    user.FullName,
                    user.Mobile,
                    user.RoleId,
                    roleName = role.RoleName,
                    password,
                    botLink,
                    queued
                });
        }

        public async Task<ApiResultDto> EnqueueMemberAsync(AdminEnqueueMemberDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || dto.UserId <= 0)
                return ApiResultDto.Fail("شناسه عضو نامعتبر است");
            if (!await IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("عضو یافت نشد");
            if (user.Active == false)
                return ApiResultDto.Fail("این حساب غیرفعال است");

            var queueError = await EnqueueBorrowerAsync(user, dto.ManagerUserId, cancellationToken);
            if (queueError != null)
                return ApiResultDto.Fail(queueError);

            return ApiResultDto.Ok(user.FullName + " به صف وام اضافه شد. مبلغ را از همان‌جا تعیین کنید.");
        }

        public async Task<ApiResultDto> UpdateMemberAsync(AdminUpdateMemberDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || dto.UserId <= 0)
                return ApiResultDto.Fail("شناسه عضو نامعتبر است");

            if (!await IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("عضو یافت نشد");

            if (dto.Active.HasValue)
            {
                if (dto.UserId == dto.ManagerUserId && dto.Active.Value == false)
                    return ApiResultDto.Fail("نمی‌توانید حساب خودتان را غیرفعال کنید");

                user.Active = dto.Active.Value;
                user.UserActiveType = dto.Active.Value
                    ? (byte)UserActiveType.Active
                    : (byte)UserActiveType.Blocked;
            }

            if (dto.UpdateDescription)
                user.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : TrimNote(dto.Description);

            if (dto.UpdateProfile)
            {
                var profileError = await ApplyMemberProfileAsync(user, dto, cancellationToken);
                if (profileError != null)
                    return ApiResultDto.Fail(profileError);
            }

            user.UpdateDate = DateTime.Now;
            user.ModifyUserId = dto.ManagerUserId;
            _repositoryManager.UserRepository.Update(user);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var result = _mapper.Map<UserDto>(user);
            result.Password = null;
            result.HasOpenLoan = (await GetOpenLoanUserIdsAsync(cancellationToken)).Contains(user.UserId);
            return ApiResultDto.Ok("اطلاعات عضو به‌روزرسانی شد", result);
        }

        private async Task<string> ApplyMemberProfileAsync(User user, AdminUpdateMemberDto dto, CancellationToken cancellationToken)
        {
            var firstName = dto.FirstName?.Trim() ?? "";
            var lastName = dto.LastName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return "نام و نام خانوادگی را بنویسید";
            if (firstName.Length > 50 || lastName.Length > 50)
                return "نام یا نام خانوادگی بلندتر از حد مجاز است";

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode ?? "").Trim();
            var mobile = PersianDateHelper.ToEnglishDigits(dto.Mobile ?? "").Trim();
            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return "کد ملی نامعتبر است";
            if (!PersianDateHelper.IsValidMobile(mobile))
                return "شماره موبایل نامعتبر است. مثال: 09121234567";

            var existingNational = await _repositoryManager.UserRepository.GetByNationalCodeAsync(nationalCode, cancellationToken);
            if (existingNational != null && existingNational.UserId != user.UserId)
                return "این کد ملی برای عضو دیگری ثبت شده است";

            var existingMobile = await _repositoryManager.UserRepository.GetByMobileAsync(mobile, cancellationToken);
            if (existingMobile != null && existingMobile.UserId != user.UserId)
                return "این شماره موبایل برای عضو دیگری ثبت شده است";

            var roleType = (UserRoleType)(dto.RoleType ?? (byte)UserRoleType.Member);
            if (dto.UserId == dto.ManagerUserId && roleType != UserRoleType.Manager)
                return "نقش حساب خودتان را عوض نکنید";

            string roleName;
            if (roleType == UserRoleType.Manager)
                roleName = "مدیر صندوق";
            else if (roleType == UserRoleType.Donator)
            {
                if (dto.MonthlyDonationAmount == null || dto.MonthlyDonationAmount <= 0)
                    return "مبلغ کمک ماهانه برای خیرین الزامی است";
                roleName = "خیر";
            }
            else if (roleType == UserRoleType.Member)
                roleName = "عضو / وام‌گیرنده";
            else
                return "نوع حساب نامعتبر است";

            var currentIsMember = !IsManagerUser(user) && user.RoleId != (int)UserRoleType.Donator
                && (user.UserRole?.RoleName ?? "") != "خیر";
            if (roleType != UserRoleType.Member && currentIsMember)
            {
                var open = await _repositoryManager.LoanRepository.GetActiveLoanByUserIdAsync(user.UserId, cancellationToken);
                if (open != null)
                    return "این عضو در صف وام یا وام باز است. تا تمام نشود نقش او عوض نمی‌شود";
            }

            var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
            var role = roles.FirstOrDefault(r => r.RoleName == roleName)
                ?? roles.FirstOrDefault(r => r.RoleId == (int)roleType);
            if (role == null)
                return "نقش کاربری در سیستم تعریف نشده است";

            user.FirstName = firstName;
            user.LastName = lastName;
            user.FullName = (firstName + " " + lastName).Trim();
            user.NationalCode = nationalCode;
            user.UserName = nationalCode;
            user.Mobile = mobile;
            user.RoleId = role.RoleId;
            user.UserRole = role;
            user.MonthlyDonationAmount = roleType == UserRoleType.Donator ? dto.MonthlyDonationAmount : null;
            user.Description = TrimNote(dto.Description);

            if (roleType == UserRoleType.Manager)
                user.UserStatus = (byte)UserStatus.Manager;
            else if (roleType == UserRoleType.Donator)
                user.UserStatus = (byte)UserStatus.Donator;
            else if (user.UserStatus == (byte)UserStatus.Manager || user.UserStatus == (byte)UserStatus.Donator)
                user.UserStatus = (byte)UserStatus.LoanApplicant;

            return null;
        }

        private async Task<string> EnqueueBorrowerAsync(User user, int managerUserId, CancellationToken cancellationToken)
        {
            if (user == null)
                return "عضو یافت نشد";
            if (IsManagerUser(user))
                return "مدیر صندوق وارد صف وام نمی‌شود";

            var roleName = user.UserRole?.RoleName ?? "";
            if (user.RoleId == (int)UserRoleType.Donator || user.UserStatus == (byte)UserStatus.Donator || roleName == "خیر")
                return "خیر وارد صف وام نمی‌شود";

            var active = await _repositoryManager.LoanRepository.GetActiveLoanByUserIdAsync(user.UserId, cancellationToken);
            if (active != null)
                return "این عضو همین حالا در صف است یا وام باز دارد";

            var installmentCountStr = await _repositoryManager.SettingRepository.GetValueAsync("DefaultInstallmentCount", "10", cancellationToken);
            int.TryParse(installmentCountStr, out var installmentCount);
            if (installmentCount <= 0) installmentCount = 10;

            var now = DateTime.Now;
            var queueCount = await _repositoryManager.LoanRepository.GetWaitingQueueCountAsync(cancellationToken);
            var loan = new Loan
            {
                UserId = user.UserId,
                LoanAmount = 0,
                RemainAmount = 0,
                InstallmentCount = installmentCount,
                InstallmentAmount = 0,
                RequestDate = now,
                RequestDate_Shamsi = PersianDateHelper.ToShamsi(now),
                QueuePriority = queueCount + 1,
                LoanRequestStatus = (byte)LoanRequestStatus.WaitingInQueue,
                LoanStatus = (byte)LoanStatus.Normal,
                Description = "ورود به صف توسط مدیر صندوق",
                CreateUserId = managerUserId,
                CreateDate = now,
                Active = true
            };
            _repositoryManager.LoanRepository.Insert(loan);

            if (!IsManagerUser(user))
                user.UserStatus = (byte)UserStatus.LoanApplicant;
            user.UpdateDate = now;
            user.ModifyUserId = managerUserId;
            _repositoryManager.UserRepository.Update(user);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        private async Task<HashSet<int>> GetOpenLoanUserIdsAsync(CancellationToken cancellationToken)
        {
            var loans = await _repositoryManager.LoanRepository.GetAllAsync(cancellationToken);
            return new HashSet<int>(loans.Where(l =>
                l.Active != false &&
                (l.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue ||
                 (l.LoanRequestStatus == (byte)LoanRequestStatus.Disbursed &&
                  l.LoanStatus != (byte)LoanStatus.Cleared)))
                .Select(l => l.UserId));
        }

        private static bool IsManagerUser(User user)
        {
            if (user == null) return false;
            if (user.UserStatus == (byte)UserStatus.Manager) return true;
            if (user.RoleId == (int)UserRoleType.Manager) return true;
            var roleName = user.UserRole?.RoleName ?? string.Empty;
            return roleName == "مدیر صندوق" || roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return null;
            note = note.Trim();
            return note.Length > 500 ? note.Substring(0, 500) : note;
        }
    }
}
