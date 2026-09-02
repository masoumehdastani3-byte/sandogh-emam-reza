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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class UserService : IUserService
    {
        private const int OtpExpiryMinutes = 5;
        private const int OtpMaxAttempts = 5;
        private const int OtpMaxRequestsPerWindow = 3;
        private const int OtpRequestWindowMinutes = 15;

        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly IBaleService _baleService;
        private readonly INationalIdVerifyService _nationalIdVerifyService;

        public UserService(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IBaleService baleService,
            INationalIdVerifyService nationalIdVerifyService)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _baleService = baleService;
            _nationalIdVerifyService = nationalIdVerifyService;
        }

        public async Task<string> Delete(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null)
                return "کاربر مورد نظر یافت نشد";

            _repositoryManager.UserRepository.Delete(user);
            var result = await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0 ? "0" : "عمل حذف با مشکل مواجه شده است";
        }

        public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = await _repositoryManager.UserRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<List<UserDto>>(result);
        }

        public async Task<UserDto> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            var result = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<UserDto>(result);
        }

        public async Task<List<UserDto>> GetFilterAsync(string userName, int? userId, string fullName, string cardNumber, string mobile, CancellationToken cancellationToken = default)
        {
            var result = await _repositoryManager.UserRepository.GetFilterAsync(userName, userId, fullName, cardNumber, mobile, cancellationToken);
            return _mapper.Map<List<UserDto>>(result);
        }

        public async Task<ApiResultDto> RegisterAsync(RegisterMemberDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                return ApiResultDto.Fail("اطلاعات عضویت نامعتبر است");

            if (!dto.HasBaleApp)
                return ApiResultDto.Fail("عضویت بدون نصب اپلیکیشن بله امکان‌پذیر نیست");

            if (!PersianDateHelper.IsValidNationalCode(dto.NationalCode))
                return ApiResultDto.Fail("کد ملی نامعتبر است");

            if (!PersianDateHelper.IsValidMobile(dto.Mobile))
                return ApiResultDto.Fail("شماره موبایل نامعتبر است");

            var existingNational = await _repositoryManager.UserRepository.GetByNationalCodeAsync(dto.NationalCode, cancellationToken);
            if (existingNational != null)
                return ApiResultDto.Fail("این کد ملی قبلاً ثبت شده است");

            var existingMobile = await _repositoryManager.UserRepository.GetByMobileAsync(dto.Mobile, cancellationToken);
            if (existingMobile != null)
                return ApiResultDto.Fail("این شماره موبایل قبلاً ثبت شده است");

            var roleType = (UserRoleType)dto.RoleType;
            if (roleType != UserRoleType.Member && roleType != UserRoleType.Donator)
                return ApiResultDto.Fail("نوع عضویت نامعتبر است");

            if (roleType == UserRoleType.Donator && (dto.MonthlyDonationAmount == null || dto.MonthlyDonationAmount <= 0))
                return ApiResultDto.Fail("مبلغ کمک ماهانه برای خیرین الزامی است");

            var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
            var roleName = roleType == UserRoleType.Donator ? "خیر" : "عضو / وام‌گیرنده";
            var role = roles.FirstOrDefault(r => r.RoleName == roleName) ?? roles.FirstOrDefault(r => r.RoleId == (int)roleType);
            if (role == null)
                return ApiResultDto.Fail("نقش کاربری در سیستم تعریف نشده است. یک‌بار برنامه را اجرا کنید تا نقش‌ها ساخته شوند.");

            var password = PersianDateHelper.GeneratePassword();
            var firstName = dto.FirstName?.Trim() ?? "";
            var lastName = dto.LastName?.Trim() ?? "";

            var user = new User
            {
                NationalCode = dto.NationalCode,
                Mobile = dto.Mobile,
                FirstName = firstName,
                LastName = lastName,
                FullName = $"{firstName} {lastName}".Trim(),
                UserName = dto.NationalCode,
                Password = password,
                RoleId = role.RoleId,
                UserStatus = roleType == UserRoleType.Donator
                    ? (byte)UserStatus.Donator
                    : (byte)UserStatus.LoanApplicant,
                UserActiveType = (byte)UserActiveType.Active,
                UserStatusLife = (byte)UserStatusLife.living,
                UserGender = (byte)UserGender.Man,
                HasBaleApp = true,
                BaleChatId = string.IsNullOrWhiteSpace(dto.BaleChatId) ? null : dto.BaleChatId.Trim(),
                MonthlyDonationAmount = dto.MonthlyDonationAmount,
                CreateDate = DateTime.Now,
                Active = true
            };

            _repositoryManager.UserRepository.Insert(user);
            var save = await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            if (save <= 0)
                return ApiResultDto.Fail("ثبت عضویت با مشکل مواجه شد");

            var botLink = _baleService.GetBotStartLink(user.UserId);
            if (!string.IsNullOrWhiteSpace(user.BaleChatId))
                await _baleService.NotifyPasswordAsync(user, password, cancellationToken);

            var needStart = string.IsNullOrWhiteSpace(user.BaleChatId);
            var message = needStart
                ? "عضویت ثبت شد. ربات بله را با لینک زیر استارت کنید تا Chat ID ذخیره و رمز عبور برایتان ارسال شود. می‌توانید به‌جای لینک، کد ملی را هم برای ربات بفرستید."
                : "عضویت با موفقیت انجام شد. رمز عبور از طریق بله ارسال شد.";

            return ApiResultDto.Ok(message, new
            {
                user.UserId,
                user.NationalCode,
                user.FullName,
                user.RoleId,
                botLink,
                needBaleStart = needStart,
                // فقط وقتی هنوز به بله وصل نیست؛ بعد از Start از طریق بله هم ارسال می‌شود
                passwordUntilBaleConnected = needStart ? password : null
            });
        }

        public async Task<ApiResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NationalCode) || string.IsNullOrWhiteSpace(dto.Password))
                return ApiResultDto.Fail("کد ملی و رمز عبور الزامی است");

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode).Trim();
            var password = dto.Password.Trim();

            var user = await _repositoryManager.UserRepository.LoginAsync(nationalCode, password, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربری با این کد ملی و رمز یافت نشد");

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Password = null;
            return ApiResultDto.Ok("ورود موفق", userDto);
        }

        public async Task<ApiResultDto> RequestLoginOtpAsync(RequestLoginOtpDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NationalCode))
                return ApiResultDto.Fail("کد ملی الزامی است");

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode).Trim();
            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return ApiResultDto.Fail("کد ملی نامعتبر است");

            var user = await _repositoryManager.UserRepository.GetByNationalCodeAsync(nationalCode, cancellationToken);
            if (user == null || user.Active != true)
                return ApiResultDto.Fail("کاربری با این کد ملی یافت نشد");

            if (string.IsNullOrWhiteSpace(user.BaleChatId))
            {
                var botLink = _baleService.GetBotStartLink(user.UserId);
                return new ApiResultDto
                {
                    Success = false,
                    Message = "حساب بله شما متصل نیست. ابتدا ربات بله را استارت کنید.",
                    Data = new { needBaleStart = true, botLink, user.UserId }
                };
            }

            var recentCount = await _repositoryManager.LoginOtpRepository.CountRecentRequestsAsync(nationalCode, OtpRequestWindowMinutes, cancellationToken);
            if (recentCount >= OtpMaxRequestsPerWindow)
                return ApiResultDto.Fail($"حداکثر {OtpMaxRequestsPerWindow} بار در {OtpRequestWindowMinutes} دقیقه می‌توانید کد درخواست کنید. کمی بعد دوباره تلاش کنید.");

            await _repositoryManager.LoginOtpRepository.InvalidateActiveForUserAsync(user.UserId, cancellationToken);

            var otpCode = PersianDateHelper.GenerateOtpCode();
            var otp = new LoginOtp
            {
                UserId = user.UserId,
                NationalCode = nationalCode,
                OtpHash = HashOtp(nationalCode, otpCode),
                ExpiresAt = DateTime.Now.AddMinutes(OtpExpiryMinutes),
                AttemptCount = 0,
                IsUsed = false,
                CreateDate = DateTime.Now
            };

            _repositoryManager.LoginOtpRepository.Insert(otp);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var sent = await _baleService.NotifyLoginOtpAsync(user, otpCode, cancellationToken);
            if (!sent)
                return ApiResultDto.Fail("ارسال کد از طریق بله ناموفق بود. بعداً دوباره تلاش کنید.");

            return ApiResultDto.Ok("کد ورود به بله شما ارسال شد.", new
            {
                expiresInSeconds = OtpExpiryMinutes * 60,
                maskedNationalCode = MaskNationalCode(nationalCode)
            });
        }

        public async Task<ApiResultDto> VerifyLoginOtpAsync(VerifyLoginOtpDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NationalCode) || string.IsNullOrWhiteSpace(dto.OtpCode))
                return ApiResultDto.Fail("کد ملی و کد ورود الزامی است");

            var nationalCode = PersianDateHelper.ToEnglishDigits(dto.NationalCode).Trim();
            var otpCode = PersianDateHelper.ToEnglishDigits(dto.OtpCode).Trim();

            if (!PersianDateHelper.IsValidNationalCode(nationalCode))
                return ApiResultDto.Fail("کد ملی نامعتبر است");

            if (otpCode.Length != 6 || !otpCode.All(char.IsDigit))
                return ApiResultDto.Fail("کد ورود باید ۶ رقم باشد");

            var record = await _repositoryManager.LoginOtpRepository.GetActiveByNationalCodeAsync(nationalCode, cancellationToken);
            if (record == null)
                return ApiResultDto.Fail("کد ورود منقضی شده یا نامعتبر است. دوباره درخواست دهید.");

            if (record.AttemptCount >= OtpMaxAttempts)
            {
                record.IsUsed = true;
                _repositoryManager.LoginOtpRepository.Update(record);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResultDto.Fail("تعداد تلاش‌های مجاز تمام شد. کد جدید درخواست کنید.");
            }

            record.AttemptCount++;
            if (record.OtpHash != HashOtp(nationalCode, otpCode))
            {
                _repositoryManager.LoginOtpRepository.Update(record);
                await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
                var left = OtpMaxAttempts - record.AttemptCount;
                return ApiResultDto.Fail(left > 0
                    ? $"کد ورود اشتباه است. {left} تلاش باقی مانده."
                    : "کد ورود اشتباه است. کد جدید درخواست کنید.");
            }

            record.IsUsed = true;
            _repositoryManager.LoginOtpRepository.Update(record);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(record.UserId, cancellationToken);
            if (user == null || user.Active != true)
                return ApiResultDto.Fail("کاربر یافت نشد");

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Password = null;
            return ApiResultDto.Ok("ورود موفق", userDto);
        }

        private static string HashOtp(string nationalCode, string otpCode)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{nationalCode}:{otpCode}:GoodLoanOtp"));
            return Convert.ToBase64String(bytes);
        }

        private static string MaskNationalCode(string nationalCode)
        {
            if (string.IsNullOrEmpty(nationalCode) || nationalCode.Length < 4)
                return nationalCode;
            return nationalCode.Substring(0, 3) + "****" + nationalCode.Substring(nationalCode.Length - 2);
        }

        public async Task<ApiResultDto> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(dto.UserId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            user.FirstName = dto.FirstName?.Trim();
            user.LastName = dto.LastName?.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}".Trim();
            user.PostalCode = dto.PostalCode;
            user.Address = dto.Address;
            user.UserGender = dto.UserGender;
            user.BirthDate_Shamsi = dto.BirthDate_Shamsi;
            user.BirthDate = PersianDateHelper.FromShamsi(dto.BirthDate_Shamsi);
            user.Email = dto.Email;
            user.CardNumber = dto.CardNumber;
            user.BankName = dto.BankName;
            if (!string.IsNullOrWhiteSpace(dto.BaleChatId))
                user.BaleChatId = dto.BaleChatId;
            if (dto.MonthlyDonationAmount.HasValue)
                user.MonthlyDonationAmount = dto.MonthlyDonationAmount;
            user.UpdateDate = DateTime.Now;

            _repositoryManager.UserRepository.Update(user);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("پروفایل به‌روزرسانی شد", _mapper.Map<UserDto>(user));
        }

        public async Task<ApiResultDto> UploadNationalIdAsync(int userId, string filePath, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResultDto.Fail("کاربر یافت نشد");

            user.NationalIdCardPath = filePath;
            var verify = await _nationalIdVerifyService.VerifyAsync(user.NationalCode, filePath, cancellationToken);
            user.NationalIdVerified = verify.IsValid;
            user.NationalIdVerifyMessage = verify.Message;
            user.UpdateDate = DateTime.Now;

            _repositoryManager.UserRepository.Update(user);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResultDto.Ok(verify.Message, new { user.NationalIdVerified, user.NationalIdCardPath });
        }

        public async Task<string> Insert(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                return "اطلاعات کاربر نامعتبر است";

            if (!string.IsNullOrEmpty(user.NationalCode))
            {
                var existing = await _repositoryManager.UserRepository.GetByNationalCodeAsync(user.NationalCode, cancellationToken);
                if (existing != null)
                    return "کاربر با این کد ملی وجود دارد";
            }

            user.CreateDate = DateTime.Now;
            user.Active = true;
            if (string.IsNullOrWhiteSpace(user.FullName))
                user.FullName = $"{user.FirstName} {user.LastName}".Trim();

            _repositoryManager.UserRepository.Insert(user);
            var result = await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0 ? "0" : "عمل ثبت با مشکل مواجه شده است";
        }

        public async Task<string> Update(User user, CancellationToken cancellationToken = default)
        {
            var existing = await _repositoryManager.UserRepository.GetByUserIdAsync(user.UserId, cancellationToken);
            if (existing == null)
                return "کاربر مورد نظر یافت نشد";

            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.FullName = string.IsNullOrWhiteSpace(user.FullName)
                ? $"{user.FirstName} {user.LastName}".Trim()
                : user.FullName;
            existing.Email = user.Email;
            existing.Mobile = user.Mobile;
            existing.CardNumber = user.CardNumber;
            existing.BankName = user.BankName;
            existing.PostalCode = user.PostalCode;
            existing.Address = user.Address;
            existing.UserGender = user.UserGender;
            existing.UserStatus = user.UserStatus;
            existing.UserActiveType = user.UserActiveType;
            existing.RoleId = user.RoleId;
            existing.MonthlyDonationAmount = user.MonthlyDonationAmount;
            existing.BaleChatId = user.BaleChatId;
            existing.UpdateDate = DateTime.Now;

            _repositoryManager.UserRepository.Update(existing);
            var result = await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0 ? "0" : "عمل ویرایش با مشکل مواجه شده است";
        }
    }
}
