/* GoodLoan - Schema upgrade (run manually if needed) */

IF OBJECT_ID(N'[dbo].[Setting]', N'U') IS NULL
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

IF COL_LENGTH('Loan', 'RequestDate') IS NULL ALTER TABLE [Loan] ADD [RequestDate] datetime2 NOT NULL CONSTRAINT DF_Loan_RequestDate DEFAULT(GETDATE());
IF COL_LENGTH('Loan', 'RequestDate_Shamsi') IS NULL ALTER TABLE [Loan] ADD [RequestDate_Shamsi] nvarchar(11) NULL;
IF COL_LENGTH('Loan', 'QueuePriority') IS NULL ALTER TABLE [Loan] ADD [QueuePriority] int NOT NULL CONSTRAINT DF_Loan_QueuePriority DEFAULT(0);
IF COL_LENGTH('Loan', 'LoanRequestStatus') IS NULL ALTER TABLE [Loan] ADD [LoanRequestStatus] tinyint NOT NULL CONSTRAINT DF_Loan_LoanRequestStatus DEFAULT(1);
IF COL_LENGTH('Loan', 'RejectReason') IS NULL ALTER TABLE [Loan] ADD [RejectReason] nvarchar(500) NULL;

IF COL_LENGTH('Payment', 'InstallmentId') IS NULL ALTER TABLE [Payment] ADD [InstallmentId] int NULL;
IF COL_LENGTH('Payment', 'PaymentType') IS NULL ALTER TABLE [Payment] ADD [PaymentType] tinyint NOT NULL CONSTRAINT DF_Payment_PaymentType DEFAULT(1);
IF COL_LENGTH('Payment', 'PaymentMethod') IS NULL ALTER TABLE [Payment] ADD [PaymentMethod] tinyint NOT NULL CONSTRAINT DF_Payment_PaymentMethod DEFAULT(2);
IF COL_LENGTH('Payment', 'GatewayStatus') IS NULL ALTER TABLE [Payment] ADD [GatewayStatus] tinyint NOT NULL CONSTRAINT DF_Payment_GatewayStatus DEFAULT(1);
IF COL_LENGTH('Payment', 'ZarinPalAuthority') IS NULL ALTER TABLE [Payment] ADD [ZarinPalAuthority] nvarchar(100) NULL;
IF COL_LENGTH('Payment', 'ZarinPalRefId') IS NULL ALTER TABLE [Payment] ADD [ZarinPalRefId] nvarchar(50) NULL;

IF OBJECT_ID(N'[dbo].[NotificationLog]', N'U') IS NULL
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

IF OBJECT_ID(N'[dbo].[FundTransaction]', N'U') IS NULL
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_NationalCode' AND object_id = OBJECT_ID('User'))
    CREATE UNIQUE INDEX IX_User_NationalCode ON [User](NationalCode) WHERE NationalCode IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_Mobile' AND object_id = OBJECT_ID('User'))
    CREATE UNIQUE INDEX IX_User_Mobile ON [User](Mobile);

IF OBJECT_ID(N'[dbo].[LoginOtp]', N'U') IS NULL
CREATE TABLE [dbo].[LoginOtp](
    [LoginOtpId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] int NOT NULL,
    [NationalCode] nvarchar(10) NOT NULL,
    [OtpHash] nvarchar(128) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [AttemptCount] int NOT NULL DEFAULT(0),
    [IsUsed] bit NOT NULL DEFAULT(0),
    [CreateDate] datetime2 NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT [FK_LoginOtp_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([UserId]) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginOtp_NationalCode' AND object_id = OBJECT_ID('LoginOtp'))
    CREATE INDEX IX_LoginOtp_NationalCode ON [dbo].[LoginOtp]([NationalCode], [CreateDate] DESC);
