using Contracts.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Persistence
{
    public static class DatabaseBootstrap
    {
        public static async Task EnsureSchemaAndSeedAsync(RepositoryDbContext db)
        {
            // ابتدا جداول پایه که ممکن است در دیتابیس ناقص وجود نداشته باشند
            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRole](
        [RoleId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleName] nvarchar(100) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[Setting]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Setting](
        [SettingId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SettingName] nvarchar(50) NULL,
        [SettinCaption] nvarchar(50) NULL,
        [SettingValue] nvarchar(50) NULL,
        [Category] nvarchar(50) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[User]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[User](
        [UserId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] int NOT NULL,
        [FirstName] nvarchar(50) NULL,
        [LastName] nvarchar(50) NULL,
        [FullName] nvarchar(100) NULL,
        [NationalCode] nvarchar(10) NULL,
        [Email] nvarchar(200) NULL,
        [Mobile] nvarchar(11) NOT NULL,
        [PostalCode] nvarchar(10) NULL,
        [Address] nvarchar(500) NULL,
        [BirthDate] datetime2 NULL,
        [BirthDate_Shamsi] nvarchar(10) NULL,
        [UserGender] tinyint NOT NULL DEFAULT(2),
        [UserStatusLife] tinyint NOT NULL DEFAULT(1),
        [UserActiveType] tinyint NOT NULL DEFAULT(1),
        [UserStatus] tinyint NOT NULL DEFAULT(3),
        [UserName] nvarchar(20) NULL,
        [Password] nvarchar(100) NULL,
        [CardNumber] nvarchar(20) NULL,
        [BankName] nvarchar(50) NULL,
        [NationalIdCardPath] nvarchar(500) NULL,
        [NationalIdVerified] bit NULL,
        [NationalIdVerifyMessage] nvarchar(500) NULL,
        [BaleChatId] nvarchar(50) NULL,
        [HasBaleApp] bit NOT NULL DEFAULT(1),
        [MonthlyDonationAmount] bigint NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[Payment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payment](
        [PaymentId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NULL,
        [InstallmentId] int NULL,
        [PaymentDate] datetime2 NOT NULL,
        [PaymentDate_Shamsi] nvarchar(11) NULL,
        [Amount] bigint NOT NULL,
        [GatewayFeeAmount] bigint NOT NULL DEFAULT(0),
        [PaymentStatus] tinyint NOT NULL,
        [PaymentType] tinyint NOT NULL DEFAULT(1),
        [PaymentMethod] tinyint NOT NULL DEFAULT(2),
        [GatewayStatus] tinyint NOT NULL DEFAULT(1),
        [CardNumber] nvarchar(20) NULL,
        [FileUload] nvarchar(500) NULL,
        [RefrenceNumber] nvarchar(50) NULL,
        [ZarinPalAuthority] nvarchar(100) NULL,
        [ZarinPalRefId] nvarchar(50) NULL,
        [Confirm] bit NOT NULL DEFAULT(0),
        [ConfirmUserId] int NULL,
        [ConfirmUserDescription] nvarchar(max) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[Loan]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Loan](
        [LoanId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PaymentId] int NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentDate_Shamsi] nvarchar(11) NULL,
        [RequestDate] datetime2 NOT NULL DEFAULT(GETDATE()),
        [RequestDate_Shamsi] nvarchar(11) NULL,
        [QueuePriority] int NOT NULL DEFAULT(0),
        [UserId] int NOT NULL,
        [UserSponsorId] int NULL,
        [LoanAmount] bigint NOT NULL,
        [RemainAmount] bigint NULL,
        [InstallmentAmount] bigint NULL,
        [InstallmentCount] int NULL,
        [LoanStatus] tinyint NOT NULL,
        [LoanRequestStatus] tinyint NOT NULL DEFAULT(1),
        [RejectReason] nvarchar(500) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[Installment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Installment](
        [InstallmentId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LoanId] int NOT NULL,
        [PaymentId] int NULL,
        [PaymentDate] datetime2 NULL,
        [InstallmentDate] datetime2 NOT NULL,
        [PaymentDate_shansi] nvarchar(max) NULL,
        [InstallmentDate_shansi] nvarchar(max) NULL,
        [InstallmentAmount] bigint NOT NULL,
        [InstallmentStatus] tinyint NOT NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[NotificationLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationLog](
        [NotificationId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NULL,
        [NotificationType] tinyint NOT NULL,
        [NotificationStatus] tinyint NOT NULL,
        [Message] nvarchar(max) NULL,
        [Channel] nvarchar(20) NULL,
        [SentDate] datetime2 NULL,
        [RelatedEntity] nvarchar(50) NULL,
        [RelatedId] int NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[LoginOtp]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LoginOtp](
        [LoginOtpId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [NationalCode] nvarchar(10) NOT NULL,
        [OtpHash] nvarchar(128) NOT NULL,
        [LoginTokenHash] nvarchar(128) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [AttemptCount] int NOT NULL DEFAULT(0),
        [IsUsed] bit NOT NULL DEFAULT(0),
        [CreateDate] datetime2 NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT [FK_LoginOtp_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([UserId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_LoginOtp_NationalCode] ON [dbo].[LoginOtp]([NationalCode], [CreateDate] DESC);
END

IF OBJECT_ID(N'[dbo].[LoginOtp]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('LoginOtp', 'LoginTokenHash') IS NULL ALTER TABLE [LoginOtp] ADD [LoginTokenHash] nvarchar(128) NULL;
END

IF OBJECT_ID(N'[dbo].[FundTransaction]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FundTransaction](
        [FundTransactionId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TransactionType] tinyint NOT NULL,
        [Amount] bigint NOT NULL,
        [BalanceAfter] bigint NOT NULL,
        [UserId] int NULL,
        [PaymentId] int NULL,
        [LoanId] int NULL,
        [TransactionDate] datetime2 NOT NULL,
        [TransactionDate_Shamsi] nvarchar(11) NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL
    );
END

IF OBJECT_ID(N'[dbo].[UserSubMenu]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserSubMenu](
        [UserSubMenuId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ControlID] nvarchar(50) NULL,
        [SelectorID] nvarchar(50) NULL,
        [MenuTitle] nvarchar(50) NULL,
        [Lang] nvarchar(max) NULL,
        [IsShowMenu] bit NULL CONSTRAINT DF_UserSubMenu_IsShowMenu DEFAULT(1),
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL CONSTRAINT DF_UserSubMenu_Active DEFAULT(1)
    );
END

IF OBJECT_ID(N'[dbo].[UserRoleMenuAccess]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoleMenuAccess](
        [UserRoleMenuAccessId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] int NOT NULL,
        [UserMenuId] int NOT NULL,
        [CreateDate] datetime2 NULL,
        [UpdateDate] datetime2 NULL,
        [ModifyUserId] int NULL,
        [CreateUserId] int NULL,
        [Description] nvarchar(max) NULL,
        [Active] bit NULL CONSTRAINT DF_UserRoleMenuAccess_Active DEFAULT(1)
    );
END
");

            // ستون‌های جدید روی جداول موجود
            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[User]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('User', 'NationalCode') IS NULL ALTER TABLE [User] ADD [NationalCode] nvarchar(10) NULL;
    IF COL_LENGTH('User', 'FirstName') IS NULL ALTER TABLE [User] ADD [FirstName] nvarchar(50) NULL;
    IF COL_LENGTH('User', 'LastName') IS NULL ALTER TABLE [User] ADD [LastName] nvarchar(50) NULL;
    IF COL_LENGTH('User', 'PostalCode') IS NULL ALTER TABLE [User] ADD [PostalCode] nvarchar(10) NULL;
    IF COL_LENGTH('User', 'Address') IS NULL ALTER TABLE [User] ADD [Address] nvarchar(500) NULL;
    IF COL_LENGTH('User', 'BirthDate') IS NULL ALTER TABLE [User] ADD [BirthDate] datetime2 NULL;
    IF COL_LENGTH('User', 'BirthDate_Shamsi') IS NULL ALTER TABLE [User] ADD [BirthDate_Shamsi] nvarchar(10) NULL;
    IF COL_LENGTH('User', 'NationalIdCardPath') IS NULL ALTER TABLE [User] ADD [NationalIdCardPath] nvarchar(500) NULL;
    IF COL_LENGTH('User', 'NationalIdVerified') IS NULL ALTER TABLE [User] ADD [NationalIdVerified] bit NULL;
    IF COL_LENGTH('User', 'NationalIdVerifyMessage') IS NULL ALTER TABLE [User] ADD [NationalIdVerifyMessage] nvarchar(500) NULL;
    IF COL_LENGTH('User', 'BaleChatId') IS NULL ALTER TABLE [User] ADD [BaleChatId] nvarchar(50) NULL;
    IF COL_LENGTH('User', 'HasBaleApp') IS NULL ALTER TABLE [User] ADD [HasBaleApp] bit NOT NULL CONSTRAINT DF_User_HasBaleApp DEFAULT(1);
    IF COL_LENGTH('User', 'MonthlyDonationAmount') IS NULL ALTER TABLE [User] ADD [MonthlyDonationAmount] bigint NULL;
    IF COL_LENGTH('User', 'Password') IS NOT NULL ALTER TABLE [User] ALTER COLUMN [Password] nvarchar(100) NULL;
    IF COL_LENGTH('User', 'FullName') IS NOT NULL ALTER TABLE [User] ALTER COLUMN [FullName] nvarchar(100) NULL;
END

IF OBJECT_ID(N'[dbo].[Loan]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Loan', 'RequestDate') IS NULL ALTER TABLE [Loan] ADD [RequestDate] datetime2 NOT NULL CONSTRAINT DF_Loan_RequestDate DEFAULT(GETDATE());
    IF COL_LENGTH('Loan', 'RequestDate_Shamsi') IS NULL ALTER TABLE [Loan] ADD [RequestDate_Shamsi] nvarchar(11) NULL;
    IF COL_LENGTH('Loan', 'QueuePriority') IS NULL ALTER TABLE [Loan] ADD [QueuePriority] int NOT NULL CONSTRAINT DF_Loan_QueuePriority DEFAULT(0);
    IF COL_LENGTH('Loan', 'LoanRequestStatus') IS NULL ALTER TABLE [Loan] ADD [LoanRequestStatus] tinyint NOT NULL CONSTRAINT DF_Loan_LoanRequestStatus DEFAULT(1);
    IF COL_LENGTH('Loan', 'RejectReason') IS NULL ALTER TABLE [Loan] ADD [RejectReason] nvarchar(500) NULL;
END

IF OBJECT_ID(N'[dbo].[Payment]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Payment', 'InstallmentId') IS NULL ALTER TABLE [Payment] ADD [InstallmentId] int NULL;
    IF COL_LENGTH('Payment', 'PaymentType') IS NULL ALTER TABLE [Payment] ADD [PaymentType] tinyint NOT NULL CONSTRAINT DF_Payment_PaymentType DEFAULT(1);
    IF COL_LENGTH('Payment', 'PaymentMethod') IS NULL ALTER TABLE [Payment] ADD [PaymentMethod] tinyint NOT NULL CONSTRAINT DF_Payment_PaymentMethod DEFAULT(2);
    IF COL_LENGTH('Payment', 'GatewayStatus') IS NULL ALTER TABLE [Payment] ADD [GatewayStatus] tinyint NOT NULL CONSTRAINT DF_Payment_GatewayStatus DEFAULT(1);
    IF COL_LENGTH('Payment', 'ZarinPalAuthority') IS NULL ALTER TABLE [Payment] ADD [ZarinPalAuthority] nvarchar(100) NULL;
    IF COL_LENGTH('Payment', 'ZarinPalRefId') IS NULL ALTER TABLE [Payment] ADD [ZarinPalRefId] nvarchar(50) NULL;
    IF COL_LENGTH('Payment', 'GatewayFeeAmount') IS NULL ALTER TABLE [Payment] ADD [GatewayFeeAmount] bigint NOT NULL CONSTRAINT DF_Payment_GatewayFeeAmount DEFAULT(0);
    IF COL_LENGTH('Payment', 'RefrenceNumber') IS NOT NULL ALTER TABLE [Payment] ALTER COLUMN [RefrenceNumber] nvarchar(50) NULL;
END
");

            if (!await db.UserRole.AnyAsync())
            {
                db.UserRole.AddRange(
                    new UserRole { RoleName = "مدیر صندوق", Active = true, CreateDate = DateTime.Now },
                    new UserRole { RoleName = "عضو / وام‌گیرنده", Active = true, CreateDate = DateTime.Now },
                    new UserRole { RoleName = "خیر", Active = true, CreateDate = DateTime.Now }
                );
                await db.SaveChangesAsync();
            }

            // مهاجرت changeUserMenu روی این پایگاه اعمال نشده و کلید خارجی هنوز به جدول قدیمی UserMenu وصل است.
            // مدل برنامه شناسهٔ UserSubMenu را در UserMenuId ذخیره می‌کند.
            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[UserSubMenu]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[UserRoleMenuAccess]', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.name = N'FK_UserRoleMenuAccess_UserMenu'
          AND fk.parent_object_id = OBJECT_ID(N'[dbo].[UserRoleMenuAccess]')
          AND fk.referenced_object_id = OBJECT_ID(N'[dbo].[UserMenu]')
   )
BEGIN
    DELETE FROM [dbo].[UserRoleMenuAccess];
    ALTER TABLE [dbo].[UserRoleMenuAccess] DROP CONSTRAINT [FK_UserRoleMenuAccess_UserMenu];
    ALTER TABLE [dbo].[UserRoleMenuAccess] WITH CHECK
        ADD CONSTRAINT [FK_UserRoleMenuAccess_UserMenu]
        FOREIGN KEY ([UserMenuId]) REFERENCES [dbo].[UserSubMenu]([UserSubMenuId]);
END
");

            await EnsureMenusAndAccessAsync(db);
            await EnsureDefaultAdminUserAsync(db);
            await EnsureMultiRoleUserAsync(db);

            await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('dbo.Setting', 'SettingValue') IS NOT NULL AND COL_LENGTH('dbo.Setting', 'SettingValue') < 500
    ALTER TABLE [dbo].[Setting] ALTER COLUMN [SettingValue] nvarchar(500) NULL;");

            await EnsureSettingAsync(db, "MaxLoanAmount", "سقف وام قابل پرداخت", "50000000", "Loan");
            await EnsureSettingAsync(db, "DefaultInstallmentCount", "تعداد اقساط پیش‌فرض", "10", "Loan");
            await EnsureSettingAsync(db, "FundInitialBalance", "موجودی اولیه صندوق", "0", "Fund");
            await EnsureSettingAsync(db, "AppBaseUrl", "آدرس سایت (لینک بله)", "http://localhost:5000", "App");
            await db.SaveChangesAsync();

            if (!await db.FundTransactions.AnyAsync())
            {
                var initial = await db.Settings.FirstOrDefaultAsync(a => a.SettingName == "FundInitialBalance");
                long.TryParse(initial?.SettingValue, out var bal);
                db.FundTransactions.Add(new FundTransaction
                {
                    TransactionType = 4,
                    Amount = bal,
                    BalanceAfter = bal,
                    TransactionDate = DateTime.Now,
                    TransactionDate_Shamsi = DateTime.Now.ToString("yyyy/MM/dd"),
                    Description = "موجودی اولیه",
                    Active = true,
                    CreateDate = DateTime.Now
                });
                await db.SaveChangesAsync();
            }
        }

        private static async Task EnsureSettingAsync(
            RepositoryDbContext db, string name, string caption, string value, string category)
        {
            if (!await db.Settings.AnyAsync(a => a.SettingName == name))
            {
                db.Settings.Add(new Setting
                {
                    SettingName = name,
                    SettinCaption = caption,
                    SettingValue = value,
                    Category = category,
                    Active = true,
                    CreateDate = DateTime.Now
                });
            }
        }

        private static async Task EnsureMenusAndAccessAsync(RepositoryDbContext db)
        {
            var menuDefs = new[]
            {
                ("home", "index.html", "خانه"),
                ("register", "register.html", "عضویت"),
                ("login", "login.html", "ورود"),
                ("panel", "panel.html", "پنل اعضا"),
                ("profile", "profile.html", "پروفایل"),
                ("admin", "admin.html", "مدیریت صندوق"),
                ("roles", "roles.html", "نقش‌ها و دسترسی"),
                ("swagger", "swagger", "مستندات API")
            };

            foreach (var (controlId, selectorId, title) in menuDefs)
            {
                if (!await db.UserSubMenu.AnyAsync(m => m.SelectorID == selectorId))
                {
                    db.UserSubMenu.Add(new UserSubMenu
                    {
                        ControlID = controlId,
                        SelectorID = selectorId,
                        MenuTitle = title,
                        Lang = "fa",
                        IsShowMenu = true,
                        Active = true,
                        CreateDate = DateTime.Now
                    });
                }
            }
            await db.SaveChangesAsync();

            var roles = await db.UserRole.ToListAsync();
            var menus = await db.UserSubMenu.ToListAsync();
            if (!roles.Any() || !menus.Any()) return;

            async Task EnsureAccess(string roleName, params string[] selectorIds)
            {
                var role = roles.FirstOrDefault(r => r.RoleName == roleName);
                if (role == null) return;
                if (await db.UserRoleMenuAccess.AnyAsync(a => a.RoleId == role.RoleId)) return;

                foreach (var selector in selectorIds)
                {
                    var menu = menus.FirstOrDefault(m => m.SelectorID == selector);
                    if (menu == null) continue;
                    db.UserRoleMenuAccess.Add(new UserRoleMenuAccess
                    {
                        RoleId = role.RoleId,
                        UserMenuId = menu.UserSubMenuId,
                        Active = true,
                        CreateDate = DateTime.Now
                    });
                }
            }

            var allSelectors = menus.Select(m => m.SelectorID).ToArray();
            var memberSelectors = new[] { "index.html", "login.html", "register.html", "panel.html", "profile.html" };

            await EnsureAccess("مدیر صندوق", allSelectors);
            await EnsureAccess("عضو / وام‌گیرنده", memberSelectors);
            await EnsureAccess("خیر", memberSelectors);
            await db.SaveChangesAsync();
        }

        private static async Task EnsureDefaultAdminUserAsync(RepositoryDbContext db)
        {
            const string adminNationalCode = "1000000001";
            const string adminPassword = "Admin1234";
            const string adminMobile = "09120000001";

            var managerRole = await db.UserRole.FirstOrDefaultAsync(r => r.RoleName == "مدیر صندوق");
            if (managerRole == null) return;

            var existing = await db.Users.FirstOrDefaultAsync(u => u.NationalCode == adminNationalCode);
            if (existing != null)
            {
                if (existing.RoleId != managerRole.RoleId || existing.Password != adminPassword)
                {
                    existing.RoleId = managerRole.RoleId;
                    existing.Password = adminPassword;
                    existing.UserStatus = (byte)UserStatus.Manager;
                    existing.Active = true;
                    existing.UpdateDate = DateTime.Now;
                    await db.SaveChangesAsync();
                }
                return;
            }

            db.Users.Add(new User
            {
                NationalCode = adminNationalCode,
                Mobile = adminMobile,
                FirstName = "مدیر",
                LastName = "سیستم",
                FullName = "مدیر سیستم",
                UserName = adminNationalCode,
                Password = adminPassword,
                RoleId = managerRole.RoleId,
                UserStatus = (byte)UserStatus.Manager,
                UserGender = (byte)UserGender.Man,
                UserStatusLife = (byte)UserStatusLife.living,
                UserActiveType = (byte)UserActiveType.Active,
                HasBaleApp = true,
                Active = true,
                CreateDate = DateTime.Now
            });
            await db.SaveChangesAsync();
        }

        private static async Task EnsureMultiRoleUserAsync(RepositoryDbContext db)
        {
            const string nationalCode = "0058715940";
            var user = await db.Users.FirstOrDefaultAsync(u => u.NationalCode == nationalCode);
            if (user == null) return;

            var managerRole = await db.UserRole.FirstOrDefaultAsync(r => r.RoleName == "Admin")
                ?? await db.UserRole.FirstOrDefaultAsync(r => r.RoleName == "مدیر صندوق");
            if (managerRole == null) return;

            var changed = false;
            if (user.RoleId != managerRole.RoleId)
            {
                user.RoleId = managerRole.RoleId;
                changed = true;
            }

            if (user.UserStatus != (byte)UserStatus.Manager)
            {
                user.UserStatus = (byte)UserStatus.Manager;
                changed = true;
            }

            if (user.Active != true)
            {
                user.Active = true;
                changed = true;
            }

            if (!changed) return;
            user.UpdateDate = DateTime.Now;
            await db.SaveChangesAsync();
        }
    }
}
